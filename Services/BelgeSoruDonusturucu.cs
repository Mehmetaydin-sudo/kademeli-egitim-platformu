using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using KademeliEgitimPlatformu.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>
    /// Belgeden okunan tek bir soru adayını, doğrulama durumunu ve uyarılarını taşır.
    /// </summary>
    public sealed class BelgeSoruKaydi
    {
        public int SoruNo { get; }
        public Soru Soru { get; }
        public string? KaynakOzeti { get; }
        public List<string> Hatalar { get; } = new();
        public List<string> BilgiUyarilari { get; } = new();

        public bool GecerliMi => Hatalar.Count == 0 && SoruDogrulayici.GecerliMi(Soru);

        public string DurumMetni => GecerliMi
            ? (BilgiUyarilari.Count > 0 ? "Geçerli (Notlu)" : "Geçerli")
            : "Hatalı / Eksik";

        public string TumUyarilarMetni
        {
            get
            {
                var tumu = new List<string>(Hatalar.Count + BilgiUyarilari.Count);
                tumu.AddRange(Hatalar);
                tumu.AddRange(BilgiUyarilari);
                return string.Join(" | ", tumu);
            }
        }

        public BelgeSoruKaydi(int soruNo, Soru soru, string? kaynakOzeti = null)
        {
            SoruNo = soruNo;
            Soru = soru;
            KaynakOzeti = kaynakOzeti;
        }

        /// <summary>
        /// Kullanıcı önizleme ekranında soruyu düzenledikten sonra SoruDogrulayici ile yeniden denetler.
        /// </summary>
        public void YenidenDogrula()
        {
            Hatalar.Clear();
            SoruDogrulamaSonucu dogrulama = SoruDogrulayici.Dogrula(Soru);
            foreach (string hata in dogrulama.Hatalar)
                Hatalar.Add(hata);
        }
    }

    /// <summary>
    /// Ayrıştırma sırasında tespit edilen eksik şık, geçersiz cevap veya okunamayan içerik uyarısı.
    /// </summary>
    public sealed class BelgeAyristirmaUyarisi
    {
        public int? SoruNo { get; }
        public string Sebep { get; }
        public string? HamOzet { get; }

        public BelgeAyristirmaUyarisi(int? soruNo, string sebep, string? hamOzet = null)
        {
            SoruNo = soruNo;
            Sebep = sebep;
            HamOzet = hamOzet;
        }

        public override string ToString() =>
            SoruNo.HasValue ? $"Soru {SoruNo.Value}: {Sebep}" : Sebep;
    }

    /// <summary>
    /// Bir .docx veya .pdf belgesinden çıkarılan tüm soru adayları ve uyarı özetini taşır.
    /// </summary>
    public sealed class BelgeDonusturmeSonucu
    {
        public string? KaynakDosyaYolu { get; set; }
        public string HamMetin { get; set; } = string.Empty;
        public List<BelgeSoruKaydi> Kayitlar { get; } = new();
        public List<BelgeAyristirmaUyarisi> Uyarilar { get; } = new();

        public List<Soru> GecerliSorular => Kayitlar
            .Where(k => k.GecerliMi)
            .Select(k => k.Soru)
            .ToList();

        public int GecerliSayisi => Kayitlar.Count(k => k.GecerliMi);
        public int HataliSayisi => Kayitlar.Count(k => !k.GecerliMi);

        /// <summary>
        /// Önizleme ekranında yapılan düzenlemelerden sonra tüm soru uyarılarını güncel duruma göre yeniler.
        /// Belge geneli (SoruNo == null) uyarıları korunur.
        /// </summary>
        public void UyarilariYenile()
        {
            var genelUyarilar = Uyarilar.Where(u => !u.SoruNo.HasValue).ToList();
            Uyarilar.Clear();
            Uyarilar.AddRange(genelUyarilar);

            foreach (BelgeSoruKaydi kayit in Kayitlar)
            {
                foreach (string hata in kayit.Hatalar)
                    Uyarilar.Add(new BelgeAyristirmaUyarisi(kayit.SoruNo, hata, kayit.KaynakOzeti));

                foreach (string bilgi in kayit.BilgiUyarilari)
                    Uyarilar.Add(new BelgeAyristirmaUyarisi(kayit.SoruNo, bilgi, kayit.KaynakOzeti));
            }
        }
    }

    /// <summary>
    /// Bir .docx belgesinden ham paragraf metinleri ve tablo verilerini taşır.
    /// </summary>
    public sealed class DocxOkumaSonucu
    {
        public string ParagrafMetni { get; }
        public List<List<string[]>> Tablolar { get; }

        public DocxOkumaSonucu(string paragrafMetni, List<List<string[]>> tablolar)
        {
            ParagrafMetni = paragrafMetni;
            Tablolar = tablolar;
        }
    }

    /// <summary>
    /// Word (.docx) ve PDF (.pdf) belgelerinden tamamen çevrimdışı olarak soru çıkarır,
    /// mevcut Soru modeline dönüştürür ve SoruDogrulayici ile denetler.
    /// </summary>
    public static class BelgeSoruDonusturucu
    {
        private const long MaksBelgeBoyutu = 10 * 1024 * 1024; // 10 MB

        private static readonly Regex TopluCevapBaslikDeseni = new(
            @"^\s*(?:CEVAP\s+ANAHTAR[Iıİi]|YAN[Iıİi]T\s+ANAHTAR[Iıİi]|DO[ĞğGg]RU\s+CEVAPLAR|CEVAPLAR)\s*[:\-–—]?\s*(.*)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex TopluCevapCiftDeseni = new(
            @"(?<![A-Za-zÇĞİÖŞÜçğıöşü0-9])(\d{1,4})\s*[\-\.\)\:\=–—]\s*([A-Da-d])(?![A-Za-zÇĞİÖŞÜçğıöşü0-9])",
            RegexOptions.Compiled);

        private static readonly Regex TekilCevapSatiriDeseni = new(
            @"^\s*(\d{1,4})\s*[\-\.\)\:\=–—]\s*([A-Da-d])\s*$",
            RegexOptions.Compiled);

        private static readonly Regex SoruBaslangicDeseni = new(
            @"^\s*(?:Soru\s*[-#:]?\s*(\d{1,4})\s*[:\.\)\-–—]?\s*(.*)|(\d{1,4})\s*[\.\)]\s*(?:Soru\s*[:\-]?\s*)?(.*))$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SikSatirBaslangicDeseni = new(
            @"^\s*(?:\(([A-Ea-e])\)|([A-Ea-e])\s*[\)\.\:])\s*(.*)$",
            RegexOptions.Compiled);

        private static readonly Regex SatirIciSikDeseni = new(
            @"(?:^|\s+)(?:\(([A-Ea-e])\)|([A-Ea-e])[\)\.])\s+",
            RegexOptions.Compiled);

        private static readonly Regex CevapSatiriDeseni = new(
            @"^\s*(?:Do[ğĞgG]ru\s+)?(?:Cevap|Yan[ıiİI]t|[ŞşSs][ıiİI]k)\s*[:\-=]\s*(.+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ZorlukSatiriDeseni = new(
            @"^\s*(?:Zorluk(?:\s+Sev[ıiİI]yes[ıiİI])?|Sev[ıiİI]ye)\s*[:\-=]\s*(.+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ModulSatiriDeseni = new(
            @"^\s*(?:Mod[üuÜU]l|D[ıiİI]s[ıiİI]pl[ıiİI]n|Ders|Kategor[ıiİI])\s*[:\-=]\s*(.+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex EtiketSatiriDeseni = new(
            @"^\s*Et[ıiİI]ket\s*[:\-=]\s*(.+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex KodSatiriDeseni = new(
            @"^\s*(?:Kod(?:\s+Blo[ğĞgG]u)?)\s*[:\-=]\s*(.*)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SikBazliAciklamaDeseni = new(
            @"^\s*(?:(?:A[çcÇC][ıiİI]klama|Neden\s+Yanl[ıiİI][şsŞS])\s*\(?([A-Da-d])\)?|([A-Da-d])\s*(?:[ŞşSs][ıiİI]kk?[ıiİI]?\s*)?(?:A[çcÇC][ıiİI]klama|Neden\s+Yanl[ıiİI][şsŞS]))\s*[:\-=]\s*(.+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex GenelAciklamaDeseni = new(
            @"^\s*(?:A[çcÇC][ıiİI]klama|Neden\s+Yanl[ıiİI][şsŞS]|[ÇC][öoÖO]z[üuÜU]m)\s*[:\-=]\s*(.+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Verilen .docx veya .pdf dosyasını okuyup soru listesi ve uyarı raporu üretir.
        /// </summary>
        public static BelgeDonusturmeSonucu DosyadanDonustur(
            string dosyaYolu,
            DisiplinTuru varsayilanDisiplin = DisiplinTuru.GorselProgramlama)
        {
            if (string.IsNullOrWhiteSpace(dosyaYolu))
                throw new ArgumentException("Dosya yolu boş olamaz.", nameof(dosyaYolu));

            if (!File.Exists(dosyaYolu))
                throw new FileNotFoundException("Seçilen belge dosyası bulunamadı.", dosyaYolu);

            if (new FileInfo(dosyaYolu).Length > MaksBelgeBoyutu)
                throw new InvalidDataException("Belge boyutu 10 MB sınırını aşıyor.");

            string uzanti = Path.GetExtension(dosyaYolu).ToLowerInvariant();
            BelgeDonusturmeSonucu sonuc = uzanti switch
            {
                ".docx" => DocxdenDonustur(dosyaYolu, varsayilanDisiplin),
                ".pdf" => PdfdenDonustur(dosyaYolu, varsayilanDisiplin),
                _ => throw new InvalidDataException("Belge dönüştürücü yalnızca .docx ve .pdf dosyalarını destekler.")
            };

            sonuc.KaynakDosyaYolu = dosyaYolu;
            return sonuc;
        }

        /// <summary>
        /// Öğretmenlerin hem klasik test formatını hem de tablo formatını görerek
        /// kolayca doldurabileceği çevrimdışı örnek Word (.docx) şablonu üretir.
        /// </summary>
        public static void OrnekWordSablonuOlustur(string ciktiDosyaYolu)
        {
            if (string.IsNullOrWhiteSpace(ciktiDosyaYolu))
                throw new ArgumentException("Çıktı dosya yolu boş olamaz.", nameof(ciktiDosyaYolu));

            string guvenliYol = VeriYolu.GuvenliCiktiYolu(ciktiDosyaYolu);
            string? klasor = Path.GetDirectoryName(Path.GetFullPath(guvenliYol));
            if (!string.IsNullOrEmpty(klasor))
                Directory.CreateDirectory(klasor);

            using WordprocessingDocument doc = WordprocessingDocument.Create(guvenliYol, WordprocessingDocumentType.Document);
            MainDocumentPart mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            Body body = mainPart.Document.Body!;

            string[] klasikSatirlar =
            {
                "KADEMELİ EĞİTİM PLATFORMU — ÖRNEK SORU ŞABLONU",
                "Bu belgeyi doğrudan düzenleyip 'Soru İçe Aktar' butonuyla platforma yükleyebilirsiniz.",
                "",
                "1. Kademeli öğrenme yaklaşımında zorluk seviyelerinin temel amacı nedir?",
                "A) Tüm konuları tek seferde ezberletmek",
                "B) Kolaydan zora adım adım kavrama ve pekiştirme sağlamak",
                "C) Yalnızca hız ölçmek",
                "D) Yanlış cevapları gizlemek",
                "Doğru Cevap: B",
                "Zorluk: Kolay",
                "Açıklama: Kademeli sistemde öğrenci temel kavramları oturttuktan sonra bir üst zorluk seviyesine geçer.",
                "",
                "2. Yanlış cevap verildiğinde 'Neden Yanlış?' açıklaması göstermek öğrenmeyi nasıl etkiler?",
                "A) Kavram yanılgısının anında fark edilip düzeltilmesini sağlar",
                "B) Yalnızca sınav süresini uzatır",
                "C) Öğrenme kalıcılığına etkisi yoktur",
                "D) Doğru şıkkı gizler",
                "Cevap: A",
                "Zorluk: Orta",
                "Neden Yanlış: Hatanın gerekçesini görmek kalıcı öğrenmeyi güçlendirir.",
                "",
                "Alternatif Tablo Formatı (Aşağıdaki tabloya da yeni satırlar ekleyebilirsiniz):"
            };

            foreach (string satir in klasikSatirlar)
                body.AppendChild(new Paragraph(new Run(new Text(satir))));

            var tablo = new Table();
            tablo.AppendChild(SablonTabloSatiri("Soru", "A", "B", "C", "D", "Cevap", "Zorluk", "Açıklama"));
            tablo.AppendChild(SablonTabloSatiri(
                "Tablo formatında hazırlanan örnek soru metni buraya yazılır.",
                "Birinci seçenek",
                "İkinci seçenek",
                "Üçüncü seçenek (doğru cevap)",
                "Dördüncü seçenek",
                "C",
                "Zor",
                "Yanlış şık seçildiğinde öğrenciye gösterilecek kısa açıklama."));

            body.AppendChild(tablo);
            mainPart.Document.Save();
        }

        private static TableRow SablonTabloSatiri(params string[] hucreler)
        {
            var satir = new TableRow();
            foreach (string metin in hucreler)
                satir.AppendChild(new TableCell(new Paragraph(new Run(new Text(metin)))));
            return satir;
        }

        // ==================== DOCX (.DOCX) OKUMA ====================
        /// <summary>
        /// Word (.docx) dosyasından hem paragraf metinlerini hem de varsa tabloları okur.
        /// Dosya başka bir programda (Word vb.) açık olsa bile salt-okunur olarak okuyabilir.
        /// </summary>
        public static DocxOkumaSonucu DocxOku(string dosyaYolu)
        {
            if (!File.Exists(dosyaYolu))
                throw new FileNotFoundException("Word (.docx) dosyası bulunamadı.", dosyaYolu);

            try
            {
                using var akis = new FileStream(dosyaYolu, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var belge = WordprocessingDocument.Open(akis, isEditable: false);

                Body? govde = belge.MainDocumentPart?.Document?.Body;
                if (govde == null)
                    return new DocxOkumaSonucu(string.Empty, new List<List<string[]>>());

                var paragrafMetni = new StringBuilder();
                var tablolar = new List<List<string[]>>();

                foreach (OpenXmlElement oge in govde.ChildElements)
                {
                    if (oge is Paragraph paragraf)
                    {
                        string satir = ParagrafMetniniAl(paragraf);
                        paragrafMetni.AppendLine(satir);
                    }
                    else if (oge is Table tablo)
                    {
                        var tabloSatirlari = TabloSatirlariniAl(tablo);
                        if (tabloSatirlari.Count > 0)
                            tablolar.Add(tabloSatirlari);
                    }
                }

                return new DocxOkumaSonucu(MetniNormallestir(paragrafMetni.ToString()), tablolar);
            }
            catch (Exception ex) when (ex is not FileNotFoundException && ex is not InvalidDataException)
            {
                throw new InvalidDataException("Word (.docx) dosyası okunamadı veya bozuk.", ex);
            }
        }

        /// <summary>
        /// Word (.docx) belgesindeki tüm paragraf ve tablo metinlerini tek bir birleşik metin olarak döndürür.
        /// </summary>
        public static string DocxdenMetinCikar(string dosyaYolu)
        {
            DocxOkumaSonucu okuma = DocxOku(dosyaYolu);
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(okuma.ParagrafMetni))
                sb.AppendLine(okuma.ParagrafMetni.TrimEnd());

            foreach (List<string[]> tablo in okuma.Tablolar)
            {
                if (sb.Length > 0)
                    sb.AppendLine();

                foreach (string[] satir in tablo)
                    sb.AppendLine(string.Join(" | ", satir));
            }

            return MetniNormallestir(sb.ToString());
        }

        private static BelgeDonusturmeSonucu DocxdenDonustur(string dosyaYolu, DisiplinTuru varsayilanDisiplin)
        {
            DocxOkumaSonucu okuma = DocxOku(dosyaYolu);
            var sonuc = new BelgeDonusturmeSonucu();
            var metinTamponu = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(okuma.ParagrafMetni))
                metinTamponu.AppendLine(okuma.ParagrafMetni);

            int sonrakiSoruNo = 1;
            bool tabloSorusuBulundu = false;

            foreach (List<string[]> tablo in okuma.Tablolar)
            {
                if (SoruTablosuMu(tablo, out TabloSutunHaritasi? harita, out bool baslikSatiriVar) && harita != null)
                {
                    int oncekiKayitSayisi = sonuc.Kayitlar.Count;
                    TablodanSorulariEkle(tablo, harita, baslikSatiriVar, varsayilanDisiplin, sonuc, ref sonrakiSoruNo);

                    if (sonuc.Kayitlar.Count > oncekiKayitSayisi)
                    {
                        tabloSorusuBulundu = true;
                    }
                    else
                    {
                        sonuc.Uyarilar.Add(new BelgeAyristirmaUyarisi(
                            null,
                            "Soru tablosunda başlık satırı dışında veri satırı bulunamadı."));
                    }
                }
                else
                {
                    // Soru tablosu değilse (ör. düzen/çerçeve tablosuysa) hücre metinlerini serbest metin ayrıştırıcısına aktar.
                    foreach (string[] satir in tablo)
                    {
                        foreach (string hucre in satir)
                        {
                            if (!string.IsNullOrWhiteSpace(hucre))
                                metinTamponu.AppendLine(hucre);
                        }
                    }
                }
            }

            string serbestMetin = MetniNormallestir(metinTamponu.ToString());
            sonuc.HamMetin = serbestMetin;

            if (!string.IsNullOrWhiteSpace(serbestMetin))
            {
                BelgeDonusturmeSonucu metinSonucu = MetindenSorulariAyristir(serbestMetin, varsayilanDisiplin);

                // Tablo soruları zaten varsa ve serbest metinde hiç soru bulunmadıysa (ör. belge başlığı),
                // "soru bulunamadı" genel uyarısını gereksiz yere ekleme.
                if (metinSonucu.Kayitlar.Count > 0)
                {
                    sonuc.Kayitlar.AddRange(metinSonucu.Kayitlar);
                    sonuc.Uyarilar.AddRange(metinSonucu.Uyarilar);
                }
                else if (!tabloSorusuBulundu)
                {
                    sonuc.Uyarilar.AddRange(metinSonucu.Uyarilar);
                }
            }
            else if (!tabloSorusuBulundu)
            {
                sonuc.Uyarilar.Add(new BelgeAyristirmaUyarisi(
                    null,
                    "Word (.docx) belgesinde okunabilir metin veya soru tablosu bulunamadı."));
            }

            return sonuc;
        }

        private static string ParagrafMetniniAl(Paragraph paragraf)
        {
            var sb = new StringBuilder();
            foreach (OpenXmlElement oge in paragraf.Descendants())
            {
                switch (oge)
                {
                    case Text metin:
                        sb.Append(metin.Text);
                        break;
                    case TabChar:
                        sb.Append(' ');
                        break;
                    case Break:
                    case CarriageReturn:
                        sb.Append('\n');
                        break;
                }
            }

            return sb.ToString();
        }

        private static List<string[]> TabloSatirlariniAl(Table tablo)
        {
            var satirlar = new List<string[]>();

            foreach (TableRow satir in tablo.Elements<TableRow>())
            {
                var hucreMetinleri = new List<string>();
                foreach (TableCell hucre in satir.Elements<TableCell>())
                {
                    var paragraflar = hucre.Elements<Paragraph>()
                        .Select(ParagrafMetniniAl)
                        .Select(p => MetniNormallestir(p).Trim())
                        .Where(p => !string.IsNullOrWhiteSpace(p));

                    hucreMetinleri.Add(string.Join("\n", paragraflar));
                }

                if (hucreMetinleri.Any(h => !string.IsNullOrWhiteSpace(h)))
                    satirlar.Add(hucreMetinleri.ToArray());
            }

            return satirlar;
        }

        private sealed class TabloSutunHaritasi
        {
            public int Soru { get; set; } = -1;
            public int SecenekA { get; set; } = -1;
            public int SecenekB { get; set; } = -1;
            public int SecenekC { get; set; } = -1;
            public int SecenekD { get; set; } = -1;
            public int Cevap { get; set; } = -1;
            public int Zorluk { get; set; } = -1;
            public int Modul { get; set; } = -1;
            public int Aciklama { get; set; } = -1;
            public int AciklamaA { get; set; } = -1;
            public int AciklamaB { get; set; } = -1;
            public int AciklamaC { get; set; } = -1;
            public int AciklamaD { get; set; } = -1;
            public int KodBlogu { get; set; } = -1;
            public int Etiket { get; set; } = -1;
        }

        private static bool SoruTablosuMu(
            List<string[]> satirlar,
            out TabloSutunHaritasi? harita,
            out bool baslikSatiriVar)
        {
            harita = null;
            baslikSatiriVar = false;

            if (satirlar.Count == 0)
                return false;

            string[] ilkSatir = satirlar[0];
            var baslikHaritasi = new TabloSutunHaritasi();

            for (int i = 0; i < ilkSatir.Length; i++)
            {
                string anahtar = AnahtarNormallestir(ilkSatir[i]);
                switch (anahtar)
                {
                    case "soru":
                    case "sorumetni":
                    case "baslik":
                    case "sorubasligi":
                    case "question":
                        if (baslikHaritasi.Soru < 0) baslikHaritasi.Soru = i;
                        break;
                    case "a":
                    case "sika":
                    case "asikki":
                    case "seceneka":
                    case "optiona":
                        if (baslikHaritasi.SecenekA < 0) baslikHaritasi.SecenekA = i;
                        break;
                    case "b":
                    case "sikb":
                    case "bsikki":
                    case "secenekb":
                    case "optionb":
                        if (baslikHaritasi.SecenekB < 0) baslikHaritasi.SecenekB = i;
                        break;
                    case "c":
                    case "sikc":
                    case "csikki":
                    case "secenekc":
                    case "optionc":
                        if (baslikHaritasi.SecenekC < 0) baslikHaritasi.SecenekC = i;
                        break;
                    case "d":
                    case "sikd":
                    case "dsikki":
                    case "secenekd":
                    case "optiond":
                        if (baslikHaritasi.SecenekD < 0) baslikHaritasi.SecenekD = i;
                        break;
                    case "cevap":
                    case "dogrucevap":
                    case "dogrusik":
                    case "yanit":
                    case "dogruyanit":
                    case "dogrusecenek":
                    case "dogrusecenekindex":
                    case "answer":
                        if (baslikHaritasi.Cevap < 0) baslikHaritasi.Cevap = i;
                        break;
                    case "zorluk":
                    case "seviye":
                    case "zorlukseviyesi":
                    case "difficulty":
                        if (baslikHaritasi.Zorluk < 0) baslikHaritasi.Zorluk = i;
                        break;
                    case "modul":
                    case "disiplin":
                    case "ders":
                    case "kategori":
                    case "konu":
                        if (baslikHaritasi.Modul < 0) baslikHaritasi.Modul = i;
                        break;
                    case "aciklama":
                    case "nedenyanlis":
                    case "cozum":
                    case "genelaciklama":
                        if (baslikHaritasi.Aciklama < 0) baslikHaritasi.Aciklama = i;
                        break;
                    case "aciklamaa":
                        if (baslikHaritasi.AciklamaA < 0) baslikHaritasi.AciklamaA = i;
                        break;
                    case "aciklamab":
                        if (baslikHaritasi.AciklamaB < 0) baslikHaritasi.AciklamaB = i;
                        break;
                    case "aciklamac":
                        if (baslikHaritasi.AciklamaC < 0) baslikHaritasi.AciklamaC = i;
                        break;
                    case "aciklamad":
                        if (baslikHaritasi.AciklamaD < 0) baslikHaritasi.AciklamaD = i;
                        break;
                    case "kod":
                    case "kodblogu":
                    case "code":
                        if (baslikHaritasi.KodBlogu < 0) baslikHaritasi.KodBlogu = i;
                        break;
                    case "etiket":
                    case "tag":
                        if (baslikHaritasi.Etiket < 0) baslikHaritasi.Etiket = i;
                        break;
                }
            }

            int bulunanSikSayisi =
                (baslikHaritasi.SecenekA >= 0 ? 1 : 0) +
                (baslikHaritasi.SecenekB >= 0 ? 1 : 0) +
                (baslikHaritasi.SecenekC >= 0 ? 1 : 0) +
                (baslikHaritasi.SecenekD >= 0 ? 1 : 0);

            if (baslikHaritasi.Soru >= 0 && bulunanSikSayisi >= 2)
            {
                harita = baslikHaritasi;
                baslikSatiriVar = true;
                return true;
            }

            // Başlık satırı yoksa sabit 6-8 sütunlu (Soru | A | B | C | D | Cevap [| Zorluk] [| Açıklama]) düzenini algıla.
            if (ilkSatir.Length >= 6 && ilkSatir.Length <= 8)
            {
                int gecerliCevapSatiri = satirlar.Count(s => s.Length >= 6 && CevapHarfiniCoz(s[5], out _));
                if (gecerliCevapSatiri > 0 && gecerliCevapSatiri * 2 >= satirlar.Count)
                {
                    harita = new TabloSutunHaritasi
                    {
                        Soru = 0,
                        SecenekA = 1,
                        SecenekB = 2,
                        SecenekC = 3,
                        SecenekD = 4,
                        Cevap = 5,
                        Zorluk = ilkSatir.Length >= 7 ? 6 : -1,
                        Aciklama = ilkSatir.Length >= 8 ? 7 : -1
                    };
                    baslikSatiriVar = false;
                    return true;
                }
            }

            return false;
        }

        private static void TablodanSorulariEkle(
            List<string[]> satirlar,
            TabloSutunHaritasi harita,
            bool baslikSatiriVar,
            DisiplinTuru varsayilanDisiplin,
            BelgeDonusturmeSonucu sonuc,
            ref int sonrakiSoruNo)
        {
            int baslangic = baslikSatiriVar ? 1 : 0;

            for (int r = baslangic; r < satirlar.Count; r++)
            {
                string[] satir = satirlar[r];
                int soruNo = sonrakiSoruNo++;

                string baslik = HucreAl(satir, harita.Soru);
                string a = HucreAl(satir, harita.SecenekA);
                string b = HucreAl(satir, harita.SecenekB);
                string c = HucreAl(satir, harita.SecenekC);
                string d = HucreAl(satir, harita.SecenekD);
                string cevapMetni = HucreAl(satir, harita.Cevap);
                string zorlukMetni = HucreAl(satir, harita.Zorluk);
                string modulMetni = HucreAl(satir, harita.Modul);
                string genelAciklama = HucreAl(satir, harita.Aciklama);
                string kodBlogu = HucreAl(satir, harita.KodBlogu);
                string etiket = HucreAl(satir, harita.Etiket);

                var hatalar = new List<string>();
                var bilgiUyarilari = new List<string>();

                int dogruIndex = -1;
                if (!string.IsNullOrWhiteSpace(cevapMetni))
                {
                    if (!CevapHarfiniCoz(cevapMetni, out dogruIndex))
                        hatalar.Add($"Geçersiz doğru cevap değeri: '{cevapMetni}'. Yalnızca A, B, C veya D kullanılmalıdır.");
                }

                ZorlukSeviyesi zorluk = ZorlukSeviyesi.Orta;
                if (!string.IsNullOrWhiteSpace(zorlukMetni))
                {
                    if (!ZorlukCoz(zorlukMetni, out zorluk))
                    {
                        zorluk = ZorlukSeviyesi.Orta;
                        bilgiUyarilari.Add($"Tanınmayan zorluk değeri ('{zorlukMetni}'); varsayılan olarak Orta atandı.");
                    }
                }

                DisiplinTuru disiplin = varsayilanDisiplin;
                if (!string.IsNullOrWhiteSpace(modulMetni))
                {
                    if (!DisiplinCoz(modulMetni, out disiplin))
                    {
                        disiplin = varsayilanDisiplin;
                        bilgiUyarilari.Add($"Tanınmayan modül değeri ('{modulMetni}'); seçili varsayılan modül atandı.");
                    }
                }

                var aciklamalar = new Dictionary<int, string>();
                AciklamaEkle(aciklamalar, 0, HucreAl(satir, harita.AciklamaA));
                AciklamaEkle(aciklamalar, 1, HucreAl(satir, harita.AciklamaB));
                AciklamaEkle(aciklamalar, 2, HucreAl(satir, harita.AciklamaC));
                AciklamaEkle(aciklamalar, 3, HucreAl(satir, harita.AciklamaD));
                GenelAciklamayiUygula(aciklamalar, dogruIndex, genelAciklama);

                var soru = new Soru
                {
                    Disiplin = disiplin,
                    Zorluk = zorluk,
                    Etiket = string.IsNullOrWhiteSpace(etiket) ? null : etiket,
                    Baslik = baslik,
                    KodBlogu = string.IsNullOrWhiteSpace(kodBlogu) ? null : kodBlogu,
                    Secenekler = new List<string> { a, b, c, d },
                    DogruSecenekIndex = dogruIndex,
                    YanlisCevapAciklamalari = aciklamalar
                };

                KayitOlusturVeEkle(soruNo, soru, hatalar, bilgiUyarilari, sonuc, $"Tablo satırı {r + 1}");
            }
        }

        // ==================== PDF (.PDF) OKUMA ====================
        /// <summary>
        /// PDF (.pdf) dosyasından sayfa sayfa Türkçe (UTF-8) uyumlu metin çıkarır.
        /// Kelimeleri görsel satır hizasına göre gruplayarak şık ve soru satır düzenini korur.
        /// </summary>
        public static string PdfdenMetinCikar(string dosyaYolu)
        {
            if (!File.Exists(dosyaYolu))
                throw new FileNotFoundException("PDF (.pdf) dosyası bulunamadı.", dosyaYolu);

            try
            {
                using var akis = new FileStream(dosyaYolu, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var belge = PdfDocument.Open(akis);

                var sayfaMetinleri = new List<string>();
                foreach (Page sayfa in belge.GetPages())
                {
                    string sayfaMetni = SayfadanSatirliMetinCikar(sayfa);
                    if (!string.IsNullOrWhiteSpace(sayfaMetni))
                        sayfaMetinleri.Add(sayfaMetni);
                }

                return MetniNormallestir(string.Join("\n\n", sayfaMetinleri));
            }
            catch (Exception ex) when (ex is not FileNotFoundException && ex is not InvalidDataException)
            {
                throw new InvalidDataException("PDF (.pdf) dosyası okunamadı, şifreli veya bozuk olabilir.", ex);
            }
        }

        private static BelgeDonusturmeSonucu PdfdenDonustur(string dosyaYolu, DisiplinTuru varsayilanDisiplin)
        {
            string hamMetin = PdfdenMetinCikar(dosyaYolu);
            if (string.IsNullOrWhiteSpace(hamMetin))
            {
                var bosSonuc = new BelgeDonusturmeSonucu { HamMetin = string.Empty };
                bosSonuc.Uyarilar.Add(new BelgeAyristirmaUyarisi(
                    null,
                    "PDF belgesinden metin çıkarılamadı (dosya boş veya yalnızca taranmış görsel içeriyor olabilir)."));
                return bosSonuc;
            }

            BelgeDonusturmeSonucu sonuc = MetindenSorulariAyristir(hamMetin, varsayilanDisiplin);
            sonuc.HamMetin = hamMetin;
            return sonuc;
        }

        private static string SayfadanSatirliMetinCikar(Page sayfa)
        {
            List<Word> kelimeler = sayfa.GetWords().ToList();
            if (kelimeler.Count == 0)
                return MetniNormallestir(sayfa.Text ?? string.Empty);

            // Yukarıdan aşağıya (Bottom büyükten küçüğe) sırala, ardından aynı satırdaki kelimeleri grupla.
            kelimeler.Sort((a, b) =>
            {
                int yKarsilastirma = b.BoundingBox.Bottom.CompareTo(a.BoundingBox.Bottom);
                return yKarsilastirma != 0
                    ? yKarsilastirma
                    : a.BoundingBox.Left.CompareTo(b.BoundingBox.Left);
            });

            var satirlar = new List<(double TabanY, double OrtalamaYukseklik, List<Word> Kelimeler)>();

            foreach (Word kelime in kelimeler)
            {
                double tabanY = kelime.BoundingBox.Bottom;
                double yukseklik = Math.Max(4.0, kelime.BoundingBox.Height);

                int uygunSatirIndex = -1;
                double enKucukFark = double.MaxValue;

                for (int i = 0; i < satirlar.Count; i++)
                {
                    double tolerans = Math.Max(3.0, Math.Max(satirlar[i].OrtalamaYukseklik, yukseklik) * 0.55);
                    double fark = Math.Abs(satirlar[i].TabanY - tabanY);
                    if (fark <= tolerans && fark < enKucukFark)
                    {
                        enKucukFark = fark;
                        uygunSatirIndex = i;
                    }
                }

                if (uygunSatirIndex >= 0)
                {
                    var mevcut = satirlar[uygunSatirIndex];
                    mevcut.Kelimeler.Add(kelime);
                    int adet = mevcut.Kelimeler.Count;
                    double yeniTabanY = ((mevcut.TabanY * (adet - 1)) + tabanY) / adet;
                    double yeniYukseklik = ((mevcut.OrtalamaYukseklik * (adet - 1)) + yukseklik) / adet;
                    satirlar[uygunSatirIndex] = (yeniTabanY, yeniYukseklik, mevcut.Kelimeler);
                }
                else
                {
                    satirlar.Add((tabanY, yukseklik, new List<Word> { kelime }));
                }
            }

            satirlar.Sort((a, b) => b.TabanY.CompareTo(a.TabanY));

            var sb = new StringBuilder();
            foreach (var satir in satirlar)
            {
                satir.Kelimeler.Sort((a, b) => a.BoundingBox.Left.CompareTo(b.BoundingBox.Left));
                string satirMetni = string.Join(" ", satir.Kelimeler.Select(k => k.Text.Trim()).Where(t => t.Length > 0));
                if (!string.IsNullOrWhiteSpace(satirMetni))
                    sb.AppendLine(satirMetni);
            }

            return MetniNormallestir(sb.ToString());
        }

        // ==================== ESNEK METİN AYRIŞTIRICI (PARSER) ====================
        /// <summary>
        /// Çıkarılan serbest metni (Word paragrafları veya PDF metni) mevcut Soru modeline dönüştürür.
        /// Klasik test formatlarını, satır içi/toplu cevap anahtarlarını ve opsiyonel alanları destekler.
        /// Eksik veya bozuk soruları sessizce atlamaz; uyarı listesiyle birlikte raporlar.
        /// </summary>
        public static BelgeDonusturmeSonucu MetindenSorulariAyristir(
            string hamMetin,
            DisiplinTuru varsayilanDisiplin = DisiplinTuru.GorselProgramlama)
        {
            var sonuc = new BelgeDonusturmeSonucu
            {
                HamMetin = MetniNormallestir(hamMetin ?? string.Empty)
            };

            if (string.IsNullOrWhiteSpace(sonuc.HamMetin))
            {
                sonuc.Uyarilar.Add(new BelgeAyristirmaUyarisi(null, "Belge içeriği boş."));
                return sonuc;
            }

            List<string> satirlar = sonuc.HamMetin.Split('\n').ToList();

            // 1) Belge sonundaki (veya 'Cevap Anahtarı' başlıklı) toplu cevap anahtarını ayıkla.
            Dictionary<int, int> topluCevaplar = TopluCevapAnahtariniAyikla(satirlar);

            // 2) Satırları soru bloklarına böl.
            var soruBloklari = new List<(int SoruNo, List<string> Satirlar)>();
            int? aktifSoruNo = null;
            List<string>? aktifSatirlar = null;
            bool kodBloguIcinde = false;

            foreach (string hamSatir in satirlar)
            {
                string kirpilmis = hamSatir.Trim();

                if (kirpilmis.StartsWith("```", StringComparison.Ordinal))
                {
                    kodBloguIcinde = !kodBloguIcinde;
                    aktifSatirlar?.Add(hamSatir);
                    continue;
                }

                if (!kodBloguIcinde)
                {
                    Match soruEslesme = SoruBaslangicDeseni.Match(hamSatir);
                    if (soruEslesme.Success)
                    {
                        string noStr = soruEslesme.Groups[1].Success
                            ? soruEslesme.Groups[1].Value
                            : soruEslesme.Groups[3].Value;
                        string kalan = soruEslesme.Groups[1].Success
                            ? soruEslesme.Groups[2].Value
                            : soruEslesme.Groups[4].Value;

                        if (int.TryParse(noStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int soruNo))
                        {
                            if (aktifSoruNo.HasValue && aktifSatirlar != null)
                                soruBloklari.Add((aktifSoruNo.Value, aktifSatirlar));

                            aktifSoruNo = soruNo;
                            aktifSatirlar = new List<string>();
                            if (!string.IsNullOrWhiteSpace(kalan))
                                aktifSatirlar.Add(kalan.Trim());
                            continue;
                        }
                    }
                }

                if (aktifSatirlar != null)
                {
                    aktifSatirlar.Add(hamSatir);
                }
            }

            if (aktifSoruNo.HasValue && aktifSatirlar != null)
                soruBloklari.Add((aktifSoruNo.Value, aktifSatirlar));

            if (soruBloklari.Count == 0)
            {
                sonuc.Uyarilar.Add(new BelgeAyristirmaUyarisi(
                    null,
                    "Belgede tanınabilir soru başlangıcı bulunamadı (ör. '1.', '1)', 'Soru 1:')."));
                return sonuc;
            }

            // 3) Her soru bloğunu ayrıştır ve doğrula.
            foreach (var (soruNo, blokSatirlari) in soruBloklari)
            {
                SoruBlogunuAyristir(soruNo, blokSatirlari, topluCevaplar, varsayilanDisiplin, sonuc);
            }

            return sonuc;
        }

        private static Dictionary<int, int> TopluCevapAnahtariniAyikla(List<string> satirlar)
        {
            var cevaplar = new Dictionary<int, int>();

            // Önce açık "Cevap Anahtarı" / "Yanıt Anahtarı" başlığını ara (sondan başa).
            for (int i = satirlar.Count - 1; i >= 0; i--)
            {
                Match baslikEslesme = TopluCevapBaslikDeseni.Match(satirlar[i]);
                if (!baslikEslesme.Success)
                    continue;

                var geciciCevaplar = new Dictionary<int, int>();
                string ilkSatirKalan = baslikEslesme.Groups[1].Value;
                CevapCiftleriniEkle(ilkSatirKalan, geciciCevaplar);

                int sonIndex = i;
                for (int j = i + 1; j < satirlar.Count; j++)
                {
                    string s = satirlar[j].Trim();
                    if (string.IsNullOrWhiteSpace(s))
                    {
                        sonIndex = j;
                        continue;
                    }

                    int eklenen = CevapCiftleriniEkle(s, geciciCevaplar);
                    if (eklenen > 0)
                    {
                        sonIndex = j;
                    }
                    else
                    {
                        break;
                    }
                }

                if (geciciCevaplar.Count > 0)
                {
                    foreach (var kv in geciciCevaplar)
                        cevaplar[kv.Key] = kv.Value;

                    satirlar.RemoveRange(i, sonIndex - i + 1);
                    return cevaplar;
                }
            }

            // Başlıksız olarak doküman sonunda yer alan toplu cevap anahtarı satırlarını kontrol et
            // Örnekler: "1-B, 2-A, 3-C" veya art arda "1. B" / "2. A" satırları.
            int sondanBaslangic = satirlar.Count;
            int tekilSatirSayisi = 0;
            var sondanBulunanlar = new Dictionary<int, int>();

            for (int i = satirlar.Count - 1; i >= 0; i--)
            {
                string s = satirlar[i].Trim();
                if (string.IsNullOrWhiteSpace(s))
                    continue;

                if (SadeceTopluCevapSatiriMi(s, out var satirCevaplari))
                {
                    if (satirCevaplari.Count == 1 && TekilCevapSatiriDeseni.IsMatch(s))
                        tekilSatirSayisi++;

                    foreach (var kv in satirCevaplari)
                        sondanBulunanlar[kv.Key] = kv.Value;

                    sondanBaslangic = i;
                }
                else
                {
                    break;
                }
            }

            // En az 2 cevap eşleşmesi varsa (veya tek satırda >=2 çift varsa) toplu cevap anahtarı kabul et.
            if (sondanBulunanlar.Count >= 2 || (sondanBulunanlar.Count > 0 && tekilSatirSayisi >= 2))
            {
                foreach (var kv in sondanBulunanlar)
                    cevaplar[kv.Key] = kv.Value;

                satirlar.RemoveRange(sondanBaslangic, satirlar.Count - sondanBaslangic);
            }

            return cevaplar;
        }

        private static bool SadeceTopluCevapSatiriMi(string satir, out Dictionary<int, int> bulunanlar)
        {
            bulunanlar = new Dictionary<int, int>();
            MatchCollection eslesmeler = TopluCevapCiftDeseni.Matches(satir);
            if (eslesmeler.Count == 0)
                return false;

            string kalan = TopluCevapCiftDeseni.Replace(satir, string.Empty);
            // Çiftler çıkarıldıktan sonra yalnızca ayırıcı karakterler (, ; | / boşluk) kalmalı.
            if (Regex.IsMatch(kalan, @"[^\s,;|/\\\-–—]"))
                return false;

            CevapCiftleriniEkle(satir, bulunanlar);
            return bulunanlar.Count > 0;
        }

        private static int CevapCiftleriniEkle(string metin, Dictionary<int, int> hedef)
        {
            int eklenen = 0;
            foreach (Match m in TopluCevapCiftDeseni.Matches(metin))
            {
                if (int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int soruNo) &&
                    CevapHarfiniCoz(m.Groups[2].Value, out int secenekIndex))
                {
                    hedef[soruNo] = secenekIndex;
                    eklenen++;
                }
            }
            return eklenen;
        }

        private static void SoruBlogunuAyristir(
            int soruNo,
            List<string> blokSatirlari,
            Dictionary<int, int> topluCevaplar,
            DisiplinTuru varsayilanDisiplin,
            BelgeDonusturmeSonucu sonuc)
        {
            var baslikSatirlari = new List<string>();
            var kodSatirlari = new List<string>();
            var secenekler = new Dictionary<int, StringBuilder>();
            var sikAciklamalari = new Dictionary<int, StringBuilder>();
            var genelAciklama = new StringBuilder();

            int dogruIndex = -1;
            ZorlukSeviyesi zorluk = ZorlukSeviyesi.Orta;
            DisiplinTuru disiplin = varsayilanDisiplin;
            string? etiket = null;

            var hatalar = new List<string>();
            var bilgiUyarilari = new List<string>();

            bool siklarBasladi = false;
            bool metaBasladi = false;
            bool kodBloguIcinde = false;
            bool aktifKodAlani = false;
            int? aktifSikIndex = null;
            bool aktifGenelAciklama = false;
            int? aktifAciklamaSikIndex = null;

            foreach (string hamSatir in blokSatirlari)
            {
                string kirpilmis = hamSatir.Trim();

                if (kirpilmis.StartsWith("```", StringComparison.Ordinal))
                {
                    kodBloguIcinde = !kodBloguIcinde;
                    continue;
                }

                if (kodBloguIcinde)
                {
                    kodSatirlari.Add(hamSatir.TrimEnd());
                    continue;
                }

                if (string.IsNullOrWhiteSpace(kirpilmis))
                    continue;

                // 1) Şık bazlı açıklama ("Açıklama A:", "Neden Yanlış (B):" vb.)
                Match sikAciklamaEslesme = SikBazliAciklamaDeseni.Match(kirpilmis);
                if (sikAciklamaEslesme.Success)
                {
                    metaBasladi = true;
                    aktifKodAlani = false;
                    aktifSikIndex = null;
                    aktifGenelAciklama = false;
                    string harf = sikAciklamaEslesme.Groups[1].Success
                        ? sikAciklamaEslesme.Groups[1].Value
                        : sikAciklamaEslesme.Groups[2].Value;
                    string icerik = sikAciklamaEslesme.Groups[3].Value.Trim();

                    if (CevapHarfiniCoz(harf, out int idx))
                    {
                        aktifAciklamaSikIndex = idx;
                        MetinTamponunaEkle(sikAciklamalari, idx, icerik);
                    }
                    continue;
                }

                // 2) Genel açıklama ("Açıklama:", "Neden Yanlış:", "Çözüm:")
                Match genelAciklamaEslesme = GenelAciklamaDeseni.Match(kirpilmis);
                if (genelAciklamaEslesme.Success)
                {
                    metaBasladi = true;
                    aktifKodAlani = false;
                    aktifSikIndex = null;
                    aktifAciklamaSikIndex = null;
                    aktifGenelAciklama = true;
                    if (genelAciklama.Length > 0) genelAciklama.Append(' ');
                    genelAciklama.Append(genelAciklamaEslesme.Groups[1].Value.Trim());
                    continue;
                }

                // 3) Doğru cevap satırı ("Cevap: B", "Doğru Cevap: B", "Yanıt: B")
                Match cevapEslesme = CevapSatiriDeseni.Match(kirpilmis);
                if (cevapEslesme.Success)
                {
                    metaBasladi = true;
                    aktifKodAlani = false;
                    aktifSikIndex = null;
                    aktifGenelAciklama = false;
                    aktifAciklamaSikIndex = null;

                    string deger = cevapEslesme.Groups[1].Value.Trim();
                    if (!CevapHarfiniCoz(deger, out dogruIndex))
                    {
                        dogruIndex = -1;
                        hatalar.Add($"Geçersiz doğru cevap değeri: '{deger}'. Yalnızca A, B, C veya D kullanılmalıdır.");
                    }
                    continue;
                }

                // 4) Zorluk satırı ("Zorluk: Kolay")
                Match zorlukEslesme = ZorlukSatiriDeseni.Match(kirpilmis);
                if (zorlukEslesme.Success)
                {
                    metaBasladi = true;
                    aktifKodAlani = false;
                    aktifSikIndex = null;
                    aktifGenelAciklama = false;
                    aktifAciklamaSikIndex = null;

                    string deger = zorlukEslesme.Groups[1].Value.Trim();
                    if (!ZorlukCoz(deger, out zorluk))
                    {
                        zorluk = ZorlukSeviyesi.Orta;
                        bilgiUyarilari.Add($"Tanınmayan zorluk değeri ('{deger}'); varsayılan olarak Orta atandı.");
                    }
                    continue;
                }

                // 5) Modül / Disiplin satırı ("Modül: Siber Güvenlik")
                Match modulEslesme = ModulSatiriDeseni.Match(kirpilmis);
                if (modulEslesme.Success)
                {
                    metaBasladi = true;
                    aktifKodAlani = false;
                    aktifSikIndex = null;
                    aktifGenelAciklama = false;
                    aktifAciklamaSikIndex = null;

                    string deger = modulEslesme.Groups[1].Value.Trim();
                    if (!DisiplinCoz(deger, out disiplin))
                    {
                        disiplin = varsayilanDisiplin;
                        bilgiUyarilari.Add($"Tanınmayan modül değeri ('{deger}'); seçili varsayılan modül atandı.");
                    }
                    continue;
                }

                // 6) Etiket satırı
                Match etiketEslesme = EtiketSatiriDeseni.Match(kirpilmis);
                if (etiketEslesme.Success)
                {
                    metaBasladi = true;
                    aktifKodAlani = false;
                    aktifSikIndex = null;
                    aktifGenelAciklama = false;
                    aktifAciklamaSikIndex = null;
                    etiket = etiketEslesme.Groups[1].Value.Trim();
                    continue;
                }

                // 7) Kod bloğu satırı
                Match kodEslesme = KodSatiriDeseni.Match(kirpilmis);
                if (kodEslesme.Success)
                {
                    aktifKodAlani = true;
                    aktifSikIndex = null;
                    aktifGenelAciklama = false;
                    aktifAciklamaSikIndex = null;
                    string ilkKod = kodEslesme.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(ilkKod))
                        kodSatirlari.Add(ilkKod);
                    continue;
                }

                // 8) Şık satırı (tek şık veya aynı satırda birden fazla şık: "A) ... B) ...")
                if (SatirdanSiklariAyristir(kirpilmis, secenekler, out List<(int Index, string Metin)> bulunanSiklar, out bool besinciSikBulundu))
                {
                    siklarBasladi = true;
                    aktifKodAlani = false;
                    aktifGenelAciklama = false;
                    aktifAciklamaSikIndex = null;

                    if (besinciSikBulundu)
                        bilgiUyarilari.Add("Platform 4 şık (A–D) destekler; E şıkkı yoksayıldı.");

                    foreach (var (idx, metin) in bulunanSiklar)
                    {
                        if (secenekler.ContainsKey(idx))
                            bilgiUyarilari.Add($"{(char)('A' + idx)} şıkkı birden fazla kez tanımlanmış; son değer korundu.");

                        secenekler[idx] = new StringBuilder(metin);
                        aktifSikIndex = idx;
                    }
                    continue;
                }

                // 9) Devam satırları (kod alanı, açıklama, şık metni veya soru metni)
                if (aktifKodAlani)
                {
                    kodSatirlari.Add(hamSatir.TrimEnd());
                }
                else if (aktifAciklamaSikIndex.HasValue)
                {
                    MetinTamponunaEkle(sikAciklamalari, aktifAciklamaSikIndex.Value, kirpilmis);
                }
                else if (aktifGenelAciklama)
                {
                    if (genelAciklama.Length > 0) genelAciklama.Append(' ');
                    genelAciklama.Append(kirpilmis);
                }
                else if (aktifSikIndex.HasValue && !metaBasladi)
                {
                    if (secenekler[aktifSikIndex.Value].Length > 0)
                        secenekler[aktifSikIndex.Value].Append(' ');
                    secenekler[aktifSikIndex.Value].Append(kirpilmis);
                }
                else if (!siklarBasladi && !metaBasladi)
                {
                    baslikSatirlari.Add(kirpilmis);
                }
            }

            // Satır içi doğru cevap yoksa doküman sonundaki toplu cevap anahtarından eşleştir.
            if (dogruIndex < 0 && topluCevaplar.TryGetValue(soruNo, out int topluIndex))
            {
                dogruIndex = topluIndex;
            }

            var secenekListesi = new List<string>(4);
            for (int i = 0; i < 4; i++)
            {
                secenekListesi.Add(secenekler.TryGetValue(i, out StringBuilder? sb) ? sb.ToString().Trim() : string.Empty);
            }

            var sonAciklamalar = new Dictionary<int, string>();
            foreach (var kv in sikAciklamalari)
                AciklamaEkle(sonAciklamalar, kv.Key, kv.Value.ToString());
            GenelAciklamayiUygula(sonAciklamalar, dogruIndex, genelAciklama.ToString());

            var soru = new Soru
            {
                Disiplin = disiplin,
                Zorluk = zorluk,
                Etiket = string.IsNullOrWhiteSpace(etiket) ? null : etiket,
                Baslik = string.Join(" ", baslikSatirlari).Trim(),
                KodBlogu = kodSatirlari.Count > 0 ? string.Join("\n", kodSatirlari) : null,
                Secenekler = secenekListesi,
                DogruSecenekIndex = dogruIndex,
                YanlisCevapAciklamalari = sonAciklamalar
            };

            string ozet = baslikSatirlari.FirstOrDefault() ?? string.Empty;
            KayitOlusturVeEkle(soruNo, soru, hatalar, bilgiUyarilari, sonuc, ozet);
        }

        private static bool SatirdanSiklariAyristir(
            string satir,
            Dictionary<int, StringBuilder> mevcutSecenekler,
            out List<(int Index, string Metin)> bulunanlar,
            out bool besinciSikBulundu)
        {
            bulunanlar = new List<(int, string)>();
            besinciSikBulundu = false;

            Match baslangic = SikSatirBaslangicDeseni.Match(satir);
            if (!baslangic.Success)
                return false;

            MatchCollection tumEslesmeler = SatirIciSikDeseni.Matches(satir);
            if (tumEslesmeler.Count == 0)
            {
                string ilkHarf = baslangic.Groups[1].Success ? baslangic.Groups[1].Value : baslangic.Groups[2].Value;
                char c = char.ToUpperInvariant(ilkHarf[0]);
                if (c == 'E')
                {
                    besinciSikBulundu = true;
                    return true;
                }
                bulunanlar.Add((c - 'A', baslangic.Groups[3].Value.Trim()));
                return true;
            }

            // Yalnızca geçerli artan şık işaretlerini kabul et (örn. "A) ... B) ... C) ... D) ...")
            // böylece metin içindeki "C. sınıfı" gibi ifadeler yanlış bölünmez.
            var gecerliIsaretler = new List<(int Index, int MetinBaslangic, int EslesmeBaslangic)>();
            int sonIndex = -1;

            foreach (Match m in tumEslesmeler)
            {
                string harf = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
                char buyukHarf = char.ToUpperInvariant(harf[0]);
                int idx = buyukHarf - 'A';

                if (gecerliIsaretler.Count == 0)
                {
                    // İlk eşleşme mutlaka satır başında olmalı.
                    if (m.Index != 0)
                        break;

                    gecerliIsaretler.Add((idx, m.Index + m.Length, m.Index));
                    sonIndex = idx;
                }
                else
                {
                    // Satır içi sonraki şık, bir öncekinden büyük harf olmalı ve daha önce doldurulmamış olmalı.
                    if (idx > sonIndex && !mevcutSecenekler.ContainsKey(idx))
                    {
                        gecerliIsaretler.Add((idx, m.Index + m.Length, m.Index));
                        sonIndex = idx;
                    }
                }
            }

            if (gecerliIsaretler.Count == 0)
            {
                string ilkHarf = baslangic.Groups[1].Success ? baslangic.Groups[1].Value : baslangic.Groups[2].Value;
                char c = char.ToUpperInvariant(ilkHarf[0]);
                if (c == 'E')
                {
                    besinciSikBulundu = true;
                    return true;
                }
                bulunanlar.Add((c - 'A', baslangic.Groups[3].Value.Trim()));
                return true;
            }

            for (int i = 0; i < gecerliIsaretler.Count; i++)
            {
                var (idx, bas, _) = gecerliIsaretler[i];
                int bitis = i + 1 < gecerliIsaretler.Count ? gecerliIsaretler[i + 1].EslesmeBaslangic : satir.Length;
                string metin = satir.Substring(bas, bitis - bas).Trim();

                if (idx == 4)
                {
                    besinciSikBulundu = true;
                    continue;
                }

                if (idx >= 0 && idx <= 3)
                    bulunanlar.Add((idx, metin));
            }

            return bulunanlar.Count > 0 || besinciSikBulundu;
        }

        private static void KayitOlusturVeEkle(
            int soruNo,
            Soru soru,
            List<string> ayristirmaHatalari,
            List<string> bilgiUyarilari,
            BelgeDonusturmeSonucu sonuc,
            string? kaynakOzeti)
        {
            SoruDogrulamaSonucu dogrulama = SoruDogrulayici.Dogrula(soru);
            var kayit = new BelgeSoruKaydi(soruNo, soru, kaynakOzeti);

            foreach (string hata in ayristirmaHatalari)
            {
                if (!kayit.Hatalar.Contains(hata))
                    kayit.Hatalar.Add(hata);
            }

            foreach (string hata in dogrulama.Hatalar)
            {
                // Aynı doğru cevap hatasını çift yazmayı engelle.
                if (hata.StartsWith("Doğru cevap", StringComparison.Ordinal) &&
                    kayit.Hatalar.Any(h => h.Contains("doğru cevap", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                if (!kayit.Hatalar.Contains(hata))
                    kayit.Hatalar.Add(hata);
            }

            kayit.BilgiUyarilari.AddRange(bilgiUyarilari);
            sonuc.Kayitlar.Add(kayit);

            foreach (string hata in kayit.Hatalar)
                sonuc.Uyarilar.Add(new BelgeAyristirmaUyarisi(soruNo, hata, kaynakOzeti));

            foreach (string bilgi in kayit.BilgiUyarilari)
                sonuc.Uyarilar.Add(new BelgeAyristirmaUyarisi(soruNo, bilgi, kaynakOzeti));
        }

        // ==================== YARDIMCI ÇÖZÜMLEYİCİLER ====================
        private static void GenelAciklamayiUygula(Dictionary<int, string> aciklamalar, int dogruIndex, string genelAciklama)
        {
            if (string.IsNullOrWhiteSpace(genelAciklama))
                return;

            string temiz = genelAciklama.Trim();
            for (int i = 0; i < 4; i++)
            {
                if (i == dogruIndex)
                    continue;

                if (!aciklamalar.ContainsKey(i))
                    aciklamalar[i] = temiz;
            }
        }

        private static void AciklamaEkle(Dictionary<int, string> aciklamalar, int index, string metin)
        {
            if (index >= 0 && index <= 3 && !string.IsNullOrWhiteSpace(metin))
                aciklamalar[index] = metin.Trim();
        }

        private static void MetinTamponunaEkle(Dictionary<int, StringBuilder> sozluk, int anahtar, string metin)
        {
            if (!sozluk.TryGetValue(anahtar, out StringBuilder? sb))
            {
                sb = new StringBuilder();
                sozluk[anahtar] = sb;
            }
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(metin);
        }

        private static string HucreAl(string[] satir, int index) =>
            index >= 0 && index < satir.Length ? satir[index].Trim() : string.Empty;

        private static bool CevapHarfiniCoz(string deger, out int secenekIndex)
        {
            secenekIndex = -1;
            if (string.IsNullOrWhiteSpace(deger))
                return false;

            string temiz = deger.Trim();

            // "Cevap: B" veya "(B)" veya "B)" veya "B." veya "B - Açıklama" gibi değerlerden ilk harfi al.
            Match m = Regex.Match(temiz, @"^\(?([A-Za-z0-9])\)?[\.\)]?(?:\s+.*)?$");
            if (!m.Success)
                return false;

            char c = char.ToUpperInvariant(m.Groups[1].Value[0]);
            switch (c)
            {
                case 'A':
                case '0':
                    secenekIndex = 0;
                    return true;
                case 'B':
                case '1':
                    secenekIndex = 1;
                    return true;
                case 'C':
                case '2':
                    secenekIndex = 2;
                    return true;
                case 'D':
                case '3':
                    secenekIndex = 3;
                    return true;
                default:
                    return false;
            }
        }

        private static bool ZorlukCoz(string deger, out ZorlukSeviyesi zorluk)
        {
            string anahtar = AnahtarNormallestir(deger);
            switch (anahtar)
            {
                case "kolay":
                case "0":
                case "baslangic":
                case "temel":
                case "easy":
                    zorluk = ZorlukSeviyesi.Kolay;
                    return true;
                case "orta":
                case "1":
                case "medium":
                    zorluk = ZorlukSeviyesi.Orta;
                    return true;
                case "zor":
                case "2":
                case "ileri":
                case "hard":
                    zorluk = ZorlukSeviyesi.Zor;
                    return true;
                default:
                    zorluk = ZorlukSeviyesi.Orta;
                    return false;
            }
        }

        private static bool DisiplinCoz(string deger, out DisiplinTuru disiplin)
        {
            string anahtar = AnahtarNormallestir(deger);
            if (anahtar is "0" or "gorselprogramlama" or "gorsel" or "winforms" or "csharp" or "c")
            {
                disiplin = DisiplinTuru.GorselProgramlama;
                return true;
            }

            if (anahtar is "1" or "veritabanlari" or "veritabani" or "sql" or "postgresql" or "postgres")
            {
                disiplin = DisiplinTuru.VeriTabanlari;
                return true;
            }

            if (anahtar is "2" or "siberguvenlik" or "siber" or "guvenlik" or "cybersecurity")
            {
                disiplin = DisiplinTuru.SiberGuvenlik;
                return true;
            }

            if (anahtar.Contains("gorsel"))
            {
                disiplin = DisiplinTuru.GorselProgramlama;
                return true;
            }
            if (anahtar.Contains("veri") || anahtar.Contains("sql"))
            {
                disiplin = DisiplinTuru.VeriTabanlari;
                return true;
            }
            if (anahtar.Contains("siber") || anahtar.Contains("guvenlik"))
            {
                disiplin = DisiplinTuru.SiberGuvenlik;
                return true;
            }

            disiplin = DisiplinTuru.GorselProgramlama;
            return false;
        }

        private static string MetniNormallestir(string metin)
        {
            if (string.IsNullOrEmpty(metin))
                return string.Empty;

            string sonuc = metin
                .Normalize(NormalizationForm.FormC)
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Replace('\u00A0', ' ')
                .Replace("\uFEFF", string.Empty)
                .Replace("\u200B", string.Empty)
                .Replace("\u00AD", string.Empty)
                .Replace("\uFB01", "fi")
                .Replace("\uFB02", "fl");

            return sonuc;
        }

        private static string AnahtarNormallestir(string metin)
        {
            if (string.IsNullOrWhiteSpace(metin))
                return string.Empty;

            var sb = new StringBuilder(metin.Length);
            foreach (char ch in metin.Normalize(NormalizationForm.FormC))
            {
                char donusmus = ch switch
                {
                    'Ç' or 'ç' => 'c',
                    'Ğ' or 'ğ' => 'g',
                    'İ' or 'I' or 'ı' or 'i' => 'i',
                    'Ö' or 'ö' => 'o',
                    'Ş' or 'ş' => 's',
                    'Ü' or 'ü' => 'u',
                    _ => char.ToLowerInvariant(ch)
                };

                if (char.IsLetterOrDigit(donusmus))
                    sb.Append(donusmus);
            }

            return sb.ToString();
        }
    }
}
