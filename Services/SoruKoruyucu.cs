using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>
    /// Soru havuzunu düz metin olarak okunamayacak biçimde korur.
    ///
    /// ÖNEMLİ: SHA-256 bir özet (hash) algoritmasıdır; geri döndürülemez ama
    /// içeriği gizlemez. "Sorular metin olarak okunamasın" isteği için doğru araç
    /// AES-256-GCM şifrelemedir. Başlık tekrarını engellemek için ise gizli anahtarlı
    /// HMAC-SHA256 kullanılır (düz SHA-256, bilinen başlıklarla karşılaştırılabilir).
    ///
    /// Sınır: Uygulama çevrimdışı dağıtıldığı için anahtar programın içindedir.
    /// Kararlı bir öğrenci anahtarı çıkarıp paketi açabilir. Bu katman, düz metin
    /// erişimini ve kopyala-yapıştır yoluyla soru sızmasını engelleyen caydırıcı
    /// bir korumadır; gerçek sınav güvenliği için soruların sunucudan gelmesi gerekir.
    /// </summary>
    public static class SoruKoruyucu
    {
        private const string PaketOneki = "KEP1";
        private const string MetinOneki = "KEP1:";
        private const int NonceBoyutu = 12;
        private const int EtiketBoyutu = 16;

        // Anahtar parçaları bilinçli olarak birkaç sabite bölünmüştür; amaç
        // kaynak koddan tek parça anahtar kopyalanmasını zorlaştırmaktır.
        private static readonly byte[] PaketAnahtari = AnahtarTuret(
            "KademeliEgitimPlatformu", "soru-paketi", "2026", "AES-256-GCM");

        private static readonly byte[] BaslikAnahtari = AnahtarTuret(
            "KademeliEgitimPlatformu", "baslik-ozeti", "2026", "HMAC-SHA256");

        /// <summary>Paket dosyasının şifreli olup olmadığını (KEP1 imzası) söyler.</summary>
        public static bool PaketMi(ReadOnlySpan<byte> veri) =>
            veri.Length >= PaketOneki.Length + NonceBoyutu + EtiketBoyutu &&
            veri[..PaketOneki.Length].SequenceEqual(Encoding.ASCII.GetBytes(PaketOneki));

        /// <summary>Veriyi AES-256-GCM ile şifreler: KEP1 + nonce + etiket + şifreli gövde.</summary>
        public static byte[] PaketSifrele(byte[] duzVeri)
        {
            ArgumentNullException.ThrowIfNull(duzVeri);

            byte[] nonce = RandomNumberGenerator.GetBytes(NonceBoyutu);
            byte[] sifreli = new byte[duzVeri.Length];
            byte[] etiket = new byte[EtiketBoyutu];

            using (var aes = new AesGcm(PaketAnahtari, EtiketBoyutu))
            {
                aes.Encrypt(nonce, duzVeri, sifreli, etiket);
            }

            byte[] sonuc = new byte[PaketOneki.Length + NonceBoyutu + EtiketBoyutu + sifreli.Length];
            int konum = 0;

            Encoding.ASCII.GetBytes(PaketOneki).CopyTo(sonuc, konum);
            konum += PaketOneki.Length;
            nonce.CopyTo(sonuc, konum);
            konum += NonceBoyutu;
            etiket.CopyTo(sonuc, konum);
            konum += EtiketBoyutu;
            sifreli.CopyTo(sonuc, konum);

            return sonuc;
        }

        /// <summary>KEP1 paketini çözer. Anahtar yanlışsa CryptographicException fırlatır.</summary>
        public static byte[] PaketCoz(ReadOnlySpan<byte> paket)
        {
            if (!PaketMi(paket))
                throw new InvalidDataException("Dosya geçerli bir şifreli soru paketi değil.");

            int konum = PaketOneki.Length;
            ReadOnlySpan<byte> nonce = paket.Slice(konum, NonceBoyutu);
            konum += NonceBoyutu;
            ReadOnlySpan<byte> etiket = paket.Slice(konum, EtiketBoyutu);
            konum += EtiketBoyutu;
            ReadOnlySpan<byte> sifreli = paket[konum..];

            byte[] duzVeri = new byte[sifreli.Length];
            using (var aes = new AesGcm(PaketAnahtari, EtiketBoyutu))
            {
                aes.Decrypt(nonce, sifreli, etiket, duzVeri);
            }

            return duzVeri;
        }

        /// <summary>
        /// JSON veya şifreli paket dosyasını UTF-8 metne çevirir.
        /// Düz JSON dosyaları (geliştirici/USB paketi) geriye dönük uyumluluk için kabul edilir.
        /// </summary>
        public static string PaketOku(string dosyaYolu)
        {
            byte[] veri = File.ReadAllBytes(dosyaYolu);
            if (PaketMi(veri))
                veri = PaketCoz(veri);

            return Encoding.UTF8.GetString(veri).TrimStart('\uFEFF');
        }

        /// <summary>Metni şifreleyip SQLite'ta saklanabilir base64 metne çevirir.</summary>
        public static string MetinSifrele(string duzMetin)
        {
            ArgumentNullException.ThrowIfNull(duzMetin);
            byte[] paket = PaketSifrele(Encoding.UTF8.GetBytes(duzMetin));
            return MetinOneki + Convert.ToBase64String(paket);
        }

        /// <summary>
        /// Şifreli metni çözer. Eski (v0.6 ve öncesi) düz metin kayıtları
        /// olduğu gibi döndürür; böylece veri kaybı olmadan geçiş yapılır.
        /// </summary>
        public static string MetinCoz(string metin)
        {
            ArgumentNullException.ThrowIfNull(metin);

            if (!SifreliMi(metin))
                return metin;

            byte[] paket = Convert.FromBase64String(metin[MetinOneki.Length..]);
            return Encoding.UTF8.GetString(PaketCoz(paket));
        }

        public static bool SifreliMi(string? metin) =>
            metin != null && metin.StartsWith(MetinOneki, StringComparison.Ordinal);

        /// <summary>
        /// Başlığı gizli anahtarlı HMAC-SHA256 ile özetler. Aynı başlık her zaman
        /// aynı özeti verir (UNIQUE kısıtı için), ancak anahtar olmadan başlık
        /// tahmin edilemez.
        /// </summary>
        public static string BaslikOzeti(string baslik)
        {
            ArgumentNullException.ThrowIfNull(baslik);
            byte[] ozet = HMACSHA256.HashData(BaslikAnahtari, Encoding.UTF8.GetBytes(baslik.Trim()));
            return Convert.ToHexString(ozet);
        }

        /// <summary>Paket dosyasının değişip değişmediğini anlamak için HMAC özeti.</summary>
        public static string PaketOzeti(byte[] veri)
        {
            ArgumentNullException.ThrowIfNull(veri);
            return Convert.ToHexString(HMACSHA256.HashData(BaslikAnahtari, veri));
        }

        /// <summary>Eski kayıtlardaki düz başlıkların zaten özet olup olmadığını sezer.</summary>
        public static bool OzetMi(string? metin) =>
            metin is { Length: 64 } && IsHex(metin);

        private static bool IsHex(string metin)
        {
            foreach (char c in metin)
            {
                if (!Uri.IsHexDigit(c))
                    return false;
            }
            return true;
        }

        private static byte[] AnahtarTuret(params string[] parcalar) =>
            SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", parcalar)));
    }
}
