using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ClosedXML.Excel;
using KademeliEgitimPlatformu.Models;
using Npgsql;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>
    /// Soru havuzunu yerel PostgreSQL 18 (kademeli_egitim) veritabanındaki
    /// 'sorular' tablosu üzerinden yönetir.
    /// İçe ve dışa aktarım işlemlerinde ClosedXML (.xlsx) ve .csv formatlarını destekler.
    /// </summary>
    public class SoruServisi : ISoruDeposu
    {
        private const long MaksPaketBoyutu = 10 * 1024 * 1024; // 10 MB
        private const string AraSinavEtiketi = "AraSinav";

        private readonly string? _baglantiMetni;

        /// <summary>Son DisaridanPaketYukle çağrısında geçersiz olduğu için atlanan soru sayısı.</summary>
        public int SonPaketAtlananSayisi { get; private set; }

        public SoruServisi(string? ozelBaglantiDizesi = null, bool varsayilanlariTohumla = true)
        {
            _baglantiMetni = ozelBaglantiDizesi;
            VeritabaniBaglantisi.TablolariHazirla(_baglantiMetni);

            if (varsayilanlariTohumla && SoruSayisi() == 0)
            {
                VarsayilanSorulariTohumla();
            }
        }

        // ==================== OKUMA ====================
        public int SoruSayisi()
        {
            using var baglanti = VeritabaniBaglantisi.Ac(_baglantiMetni);
            using var komut = new NpgsqlCommand("SELECT COUNT(*) FROM sorular;", baglanti);
            return Convert.ToInt32(komut.ExecuteScalar());
        }

        public List<Soru> TumSorulariGetir(ZorlukSeviyesi? zorluk = null)
        {
            using var baglanti = VeritabaniBaglantisi.Ac(_baglantiMetni);
            using var komut = new NpgsqlCommand
            {
                Connection = baglanti,
                CommandText =
                    "SELECT id, disiplin, zorluk, etiket, baslik, kod_blogu, " +
                    "       secenek_a, secenek_b, secenek_c, secenek_d, " +
                    "       dogru_secenek_index, aciklama_a, aciklama_b, aciklama_c, aciklama_d " +
                    "FROM sorular" +
                    (zorluk.HasValue ? " WHERE zorluk = @zorluk" : "") +
                    " ORDER BY id;"
            };

            if (zorluk.HasValue)
                komut.Parameters.AddWithValue("@zorluk", (short)zorluk.Value);

            return Oku(komut);
        }

        public List<Soru> SorulariFiltrele(DisiplinTuru disiplin, ZorlukSeviyesi? zorluk = null)
        {
            using var baglanti = VeritabaniBaglantisi.Ac(_baglantiMetni);
            using var komut = new NpgsqlCommand
            {
                Connection = baglanti,
                CommandText =
                    "SELECT id, disiplin, zorluk, etiket, baslik, kod_blogu, " +
                    "       secenek_a, secenek_b, secenek_c, secenek_d, " +
                    "       dogru_secenek_index, aciklama_a, aciklama_b, aciklama_c, aciklama_d " +
                    "FROM sorular " +
                    "WHERE disiplin = @disiplin" +
                    (zorluk.HasValue ? " AND zorluk = @zorluk" : "") +
                    " ORDER BY id;"
            };

            komut.Parameters.AddWithValue("@disiplin", (short)disiplin);
            if (zorluk.HasValue)
                komut.Parameters.AddWithValue("@zorluk", (short)zorluk.Value);

            return Oku(komut);
        }

        /// <summary>Belirtilen modülün "AraSinav" etiketli sorularını getirir.</summary>
        public List<Soru> AraSinavSorulariniGetir(DisiplinTuru disiplin)
        {
            using var baglanti = VeritabaniBaglantisi.Ac(_baglantiMetni);
            using var komut = new NpgsqlCommand
            {
                Connection = baglanti,
                CommandText =
                    "SELECT id, disiplin, zorluk, etiket, baslik, kod_blogu, " +
                    "       secenek_a, secenek_b, secenek_c, secenek_d, " +
                    "       dogru_secenek_index, aciklama_a, aciklama_b, aciklama_c, aciklama_d " +
                    "FROM sorular " +
                    "WHERE disiplin = @disiplin AND etiket = @etiket " +
                    "ORDER BY id;"
            };

            komut.Parameters.AddWithValue("@disiplin", (short)disiplin);
            komut.Parameters.AddWithValue("@etiket", AraSinavEtiketi);

            return Oku(komut);
        }

        /// <summary>
        /// Kolay -> Orta veya Orta -> Zor "Seviye Geçiş Ara Sınavı" için uygun soruları getirir.
        /// </summary>
        public List<Soru> SeviyeGecisSorulariniGetir(ZorlukSeviyesi hedefZorluk)
        {
            using var baglanti = VeritabaniBaglantisi.Ac(_baglantiMetni);
            using var komut = new NpgsqlCommand { Connection = baglanti };

            if (hedefZorluk == ZorlukSeviyesi.Orta)
            {
                komut.CommandText =
                    "SELECT id, disiplin, zorluk, etiket, baslik, kod_blogu, " +
                    "       secenek_a, secenek_b, secenek_c, secenek_d, " +
                    "       dogru_secenek_index, aciklama_a, aciklama_b, aciklama_c, aciklama_d " +
                    "FROM sorular " +
                    "WHERE etiket IN ('AraSinav', 'KolayOrtaAraSinav') OR zorluk = @kolay " +
                    "ORDER BY id;";
                komut.Parameters.AddWithValue("@kolay", (short)ZorlukSeviyesi.Kolay);
            }
            else
            {
                komut.CommandText =
                    "SELECT id, disiplin, zorluk, etiket, baslik, kod_blogu, " +
                    "       secenek_a, secenek_b, secenek_c, secenek_d, " +
                    "       dogru_secenek_index, aciklama_a, aciklama_b, aciklama_c, aciklama_d " +
                    "FROM sorular " +
                    "WHERE etiket IN ('OrtaZorAraSinav', 'AraSinav') OR zorluk = @orta " +
                    "ORDER BY id;";
                komut.Parameters.AddWithValue("@orta", (short)ZorlukSeviyesi.Orta);
            }

            return Oku(komut);
        }

        // ==================== VERİ İÇE / DIŞA AKTARIM (.XLSX & .CSV) ====================
        /// <summary>
        /// Dışarıdan Excel (.xlsx) veya .csv soru dosyasını PostgreSQL veritabanına aktarır.
        /// Mevcut kullanıcı ilerlemesini ezmez ve tüm işlemleri tek bir Transaction içinde yürütür.
        /// </summary>
        public int DisaridanPaketYukle(string secilenDosyaYolu)
        {
            SonPaketAtlananSayisi = 0;

            if (!File.Exists(secilenDosyaYolu))
                throw new FileNotFoundException("Seçilen soru dosyası bulunamadı.", secilenDosyaYolu);

            if (new FileInfo(secilenDosyaYolu).Length > MaksPaketBoyutu)
                throw new InvalidDataException("Dosya boyutu 10 MB sınırını aşıyor.");

            string uzanti = Path.GetExtension(secilenDosyaYolu).ToLowerInvariant();
            List<Soru> okunanSorular = uzanti switch
            {
                ".xlsx" => ExceldenOku(secilenDosyaYolu),
                ".csv" => CsvdenOku(secilenDosyaYolu),
                _ => throw new InvalidDataException("Yalnızca Excel (.xlsx) ve .csv formatları desteklenmektedir.")
            };

            return SorulariVeritabaninaAktar(okunanSorular);
        }

        /// <summary>
        /// Verilen soru listesini tek bir PostgreSQL transaction içinde 'sorular' tablosuna ekler.
        /// Aynı başlıklı sorular tekrar eklenmez (ON CONFLICT DO NOTHING).
        /// </summary>
        public int SorulariVeritabaninaAktar(IEnumerable<Soru> yeniSorular, bool mevcutlariGuncelle = false)
        {
            ArgumentNullException.ThrowIfNull(yeniSorular);

            int eklenen = 0;
            using var baglanti = VeritabaniBaglantisi.Ac(_baglantiMetni);
            using var islem = baglanti.BeginTransaction();

            foreach (Soru aday in yeniSorular)
            {
                if (!SoruGecerliMi(Normallestir(aday)))
                {
                    SonPaketAtlananSayisi++;
                    continue;
                }

                bool basarili = mevcutlariGuncelle
                    ? SoruEkleVeyaGuncelle(baglanti, islem, aday)
                    : SoruEkle(baglanti, islem, aday);

                if (basarili)
                    eklenen++;
            }

            islem.Commit();
            return eklenen;
        }

        /// <summary>
        /// Mevcut soru havuzunu ClosedXML ile Excel (.xlsx) veya .csv dosyasına dışa aktarır.
        /// Salt-okunur (Program Files) veya göreli yolları Belgelerim\KademeliEgitimPlatformu klasörüne yönlendirir.
        /// </summary>
        public int DisariAktar(string ciktiDosyaYolu)
        {
            if (string.IsNullOrWhiteSpace(ciktiDosyaYolu))
                throw new ArgumentException("Çıktı dosya yolu boş olamaz.", nameof(ciktiDosyaYolu));

            string guvenliCiktiYolu = VeriYolu.GuvenliCiktiYolu(ciktiDosyaYolu);
            List<Soru> sorular = TumSorulariGetir();
            string uzanti = Path.GetExtension(guvenliCiktiYolu).ToLowerInvariant();

            string? klasor = Path.GetDirectoryName(Path.GetFullPath(guvenliCiktiYolu));
            if (!string.IsNullOrEmpty(klasor))
                Directory.CreateDirectory(klasor);

            switch (uzanti)
            {
                case ".xlsx":
                    ExceleYaz(sorular, guvenliCiktiYolu);
                    break;
                case ".csv":
                    CsvyeYaz(sorular, guvenliCiktiYolu);
                    break;
                default:
                    throw new InvalidDataException("Dışa aktarım için yalnızca .xlsx ve .csv uzantıları desteklenir.");
            }

            return sorular.Count;
        }

        /// <summary>
        /// Kaynak Excel (.xlsx) veya .csv dosyasındaki soruları SoruGecerliMi ile doğrular
        /// ve ClosedXML kullanarak hedef .xlsx veya .csv USB soru paketini üretir.
        /// </summary>
        public static int PaketOlustur(string kaynakYolu, string ciktiYolu)
        {
            if (!File.Exists(kaynakYolu))
                throw new FileNotFoundException("Kaynak soru dosyası bulunamadı.", kaynakYolu);

            string guvenliCiktiYolu = VeriYolu.GuvenliCiktiYolu(ciktiYolu);
            string kaynakUzanti = Path.GetExtension(kaynakYolu).ToLowerInvariant();
            List<Soru> sorular = kaynakUzanti switch
            {
                ".xlsx" => ExceldenOku(kaynakYolu),
                ".csv" => CsvdenOku(kaynakYolu),
                _ => throw new InvalidDataException("Kaynak dosya yalnızca .xlsx veya .csv formatında olabilir.")
            };

            if (sorular.Count == 0)
                throw new InvalidDataException("Kaynak dosya boş veya geçerli soru satırı içermiyor.");

            var gecerliSorular = new List<Soru>(sorular.Count);
            foreach (Soru soru in sorular)
            {
                if (!SoruGecerliMi(Normallestir(soru)))
                    throw new InvalidDataException($"Geçersiz soru tespit edildi: {soru.Baslik}");

                gecerliSorular.Add(soru);
            }

            string? klasor = Path.GetDirectoryName(Path.GetFullPath(guvenliCiktiYolu));
            if (!string.IsNullOrEmpty(klasor))
                Directory.CreateDirectory(klasor);

            string ciktiUzanti = Path.GetExtension(guvenliCiktiYolu).ToLowerInvariant();
            switch (ciktiUzanti)
            {
                case ".xlsx":
                    ExceleYaz(gecerliSorular, guvenliCiktiYolu);
                    break;
                case ".csv":
                    CsvyeYaz(gecerliSorular, guvenliCiktiYolu);
                    break;
                default:
                    throw new InvalidDataException("Çıktı paketi yalnızca .xlsx veya .csv formatında olabilir.");
            }

            return gecerliSorular.Count;
        }

        /// <summary>Bozuk/eksik soruların uygulamayı çökertmesini engelleyen doğrulama.</summary>
        public static bool SoruGecerliMi(Soru? s)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.Baslik))
                return false;

            if (s.Secenekler == null || s.Secenekler.Count != 4 || s.Secenekler.Any(string.IsNullOrWhiteSpace))
                return false;

            if (s.DogruSecenekIndex < 0 || s.DogruSecenekIndex > 3)
                return false;

            if (!Enum.IsDefined(typeof(DisiplinTuru), s.Disiplin) || !Enum.IsDefined(typeof(ZorlukSeviyesi), s.Zorluk))
                return false;

            if (s.YanlisCevapAciklamalari != null && s.YanlisCevapAciklamalari.Keys.Any(k => k < 0 || k > 3))
                return false;

            if (s.Etiket != null && s.Etiket.Length > 50)
                return false;

            return true;
        }

        // ==================== EXCEL (.XLSX) VE CSV YARDIMCILARI ====================
        private static List<Soru> ExceldenOku(string dosyaYolu)
        {
            try
            {
                using var kitap = new XLWorkbook(dosyaYolu);
                IXLWorksheet? sayfa = kitap.Worksheets.FirstOrDefault();
                if (sayfa == null)
                    return new List<Soru>();

                var sorular = new List<Soru>();
                var kullanilanSatirlar = sayfa.RowsUsed().Skip(1); // İlk satır başlık satırı

                foreach (IXLRow satir in kullanilanSatirlar)
                {
                    Soru? soru = SatirdanSoruOlustur(
                        satir.Cell(1).GetString(),
                        satir.Cell(2).GetString(),
                        satir.Cell(3).GetString(),
                        satir.Cell(4).GetString(),
                        satir.Cell(5).GetString(),
                        satir.Cell(6).GetString(),
                        satir.Cell(7).GetString(),
                        satir.Cell(8).GetString(),
                        satir.Cell(9).GetString(),
                        satir.Cell(10).GetString(),
                        satir.Cell(11).GetString(),
                        satir.Cell(12).GetString(),
                        satir.Cell(13).GetString(),
                        satir.Cell(14).GetString());

                    if (soru != null)
                        sorular.Add(soru);
                }

                return sorular;
            }
            catch (Exception ex) when (ex is not InvalidDataException && ex is not FileNotFoundException)
            {
                throw new InvalidDataException("Excel (.xlsx) dosyası okunamadı veya biçimi hatalı.", ex);
            }
        }

        private static void ExceleYaz(IReadOnlyList<Soru> sorular, string dosyaYolu)
        {
            using var kitap = new XLWorkbook();
            IXLWorksheet sayfa = kitap.Worksheets.Add("Sorular");

            string[] basliklar =
            {
                "Disiplin", "Zorluk", "Etiket", "Baslik", "KodBlogu",
                "SecenekA", "SecenekB", "SecenekC", "SecenekD",
                "DogruSecenekIndex", "AciklamaA", "AciklamaB", "AciklamaC", "AciklamaD"
            };

            for (int i = 0; i < basliklar.Length; i++)
            {
                sayfa.Cell(1, i + 1).Value = basliklar[i];
                sayfa.Cell(1, i + 1).Style.Font.Bold = true;
            }

            int satirNo = 2;
            foreach (Soru s in sorular)
            {
                sayfa.Cell(satirNo, 1).Value = s.Disiplin.ToString();
                sayfa.Cell(satirNo, 2).Value = s.Zorluk.ToString();
                sayfa.Cell(satirNo, 3).Value = s.Etiket ?? string.Empty;
                sayfa.Cell(satirNo, 4).Value = s.Baslik;
                sayfa.Cell(satirNo, 5).Value = s.KodBlogu ?? string.Empty;
                sayfa.Cell(satirNo, 6).Value = s.Secenekler.ElementAtOrDefault(0) ?? string.Empty;
                sayfa.Cell(satirNo, 7).Value = s.Secenekler.ElementAtOrDefault(1) ?? string.Empty;
                sayfa.Cell(satirNo, 8).Value = s.Secenekler.ElementAtOrDefault(2) ?? string.Empty;
                sayfa.Cell(satirNo, 9).Value = s.Secenekler.ElementAtOrDefault(3) ?? string.Empty;
                sayfa.Cell(satirNo, 10).Value = s.DogruSecenekIndex;
                sayfa.Cell(satirNo, 11).Value = s.YanlisCevapAciklamalari.GetValueOrDefault(0, string.Empty);
                sayfa.Cell(satirNo, 12).Value = s.YanlisCevapAciklamalari.GetValueOrDefault(1, string.Empty);
                sayfa.Cell(satirNo, 13).Value = s.YanlisCevapAciklamalari.GetValueOrDefault(2, string.Empty);
                sayfa.Cell(satirNo, 14).Value = s.YanlisCevapAciklamalari.GetValueOrDefault(3, string.Empty);
                satirNo++;
            }

            kitap.SaveAs(dosyaYolu);
        }

        private static List<Soru> CsvdenOku(string dosyaYolu)
        {
            string icerik = File.ReadAllText(dosyaYolu, Encoding.UTF8);
            List<string[]> satirlar = CsvAyristir(icerik);
            if (satirlar.Count <= 1)
                return new List<Soru>();

            var sorular = new List<Soru>();
            foreach (string[] hucreler in satirlar.Skip(1))
            {
                if (hucreler.Length < 10)
                    continue;

                Soru? soru = SatirdanSoruOlustur(
                    DegerAl(hucreler, 0),
                    DegerAl(hucreler, 1),
                    DegerAl(hucreler, 2),
                    DegerAl(hucreler, 3),
                    DegerAl(hucreler, 4),
                    DegerAl(hucreler, 5),
                    DegerAl(hucreler, 6),
                    DegerAl(hucreler, 7),
                    DegerAl(hucreler, 8),
                    DegerAl(hucreler, 9),
                    DegerAl(hucreler, 10),
                    DegerAl(hucreler, 11),
                    DegerAl(hucreler, 12),
                    DegerAl(hucreler, 13));

                if (soru != null)
                    sorular.Add(soru);
            }

            return sorular;
        }

        private static void CsvyeYaz(IReadOnlyList<Soru> sorular, string dosyaYolu)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Disiplin,Zorluk,Etiket,Baslik,KodBlogu,SecenekA,SecenekB,SecenekC,SecenekD,DogruSecenekIndex,AciklamaA,AciklamaB,AciklamaC,AciklamaD");

            foreach (Soru s in sorular)
            {
                string[] alanlar =
                {
                    s.Disiplin.ToString(),
                    s.Zorluk.ToString(),
                    s.Etiket ?? string.Empty,
                    s.Baslik,
                    s.KodBlogu ?? string.Empty,
                    s.Secenekler.ElementAtOrDefault(0) ?? string.Empty,
                    s.Secenekler.ElementAtOrDefault(1) ?? string.Empty,
                    s.Secenekler.ElementAtOrDefault(2) ?? string.Empty,
                    s.Secenekler.ElementAtOrDefault(3) ?? string.Empty,
                    s.DogruSecenekIndex.ToString(CultureInfo.InvariantCulture),
                    s.YanlisCevapAciklamalari.GetValueOrDefault(0, string.Empty),
                    s.YanlisCevapAciklamalari.GetValueOrDefault(1, string.Empty),
                    s.YanlisCevapAciklamalari.GetValueOrDefault(2, string.Empty),
                    s.YanlisCevapAciklamalari.GetValueOrDefault(3, string.Empty)
                };

                sb.AppendLine(string.Join(",", alanlar.Select(CsvKacis)));
            }

            File.WriteAllText(dosyaYolu, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }

        private static string CsvKacis(string deger)
        {
            if (string.IsNullOrEmpty(deger))
                return string.Empty;

            if (deger.Contains(',') || deger.Contains('"') || deger.Contains('\n') || deger.Contains('\r'))
                return "\"" + deger.Replace("\"", "\"\"") + "\"";

            return deger;
        }

        private static List<string[]> CsvAyristir(string icerik)
        {
            var satirlar = new List<string[]>();
            var mevcutSatir = new List<string>();
            var mevcutAlan = new StringBuilder();
            bool tirnakIcinde = false;

            for (int i = 0; i < icerik.Length; i++)
            {
                char c = icerik[i];
                if (tirnakIcinde)
                {
                    if (c == '"')
                    {
                        if (i + 1 < icerik.Length && icerik[i + 1] == '"')
                        {
                            mevcutAlan.Append('"');
                            i++;
                        }
                        else
                        {
                            tirnakIcinde = false;
                        }
                    }
                    else
                    {
                        mevcutAlan.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        tirnakIcinde = true;
                    }
                    else if (c == ',')
                    {
                        mevcutSatir.Add(mevcutAlan.ToString());
                        mevcutAlan.Clear();
                    }
                    else if (c == '\r' || c == '\n')
                    {
                        if (c == '\r' && i + 1 < icerik.Length && icerik[i + 1] == '\n')
                            i++;

                        mevcutSatir.Add(mevcutAlan.ToString());
                        mevcutAlan.Clear();
                        if (mevcutSatir.Any(h => !string.IsNullOrWhiteSpace(h)))
                            satirlar.Add(mevcutSatir.ToArray());
                        mevcutSatir.Clear();
                    }
                    else
                    {
                        mevcutAlan.Append(c);
                    }
                }
            }

            if (mevcutAlan.Length > 0 || mevcutSatir.Count > 0)
            {
                mevcutSatir.Add(mevcutAlan.ToString());
                if (mevcutSatir.Any(h => !string.IsNullOrWhiteSpace(h)))
                    satirlar.Add(mevcutSatir.ToArray());
            }

            return satirlar;
        }

        private static string DegerAl(string[] dizi, int index) =>
            index < dizi.Length ? dizi[index] : string.Empty;

        private static Soru? SatirdanSoruOlustur(
            string disiplinMetni,
            string zorlukMetni,
            string etiket,
            string baslik,
            string kodBlogu,
            string secenekA,
            string secenekB,
            string secenekC,
            string secenekD,
            string dogruIndexMetni,
            string aciklamaA,
            string aciklamaB,
            string aciklamaC,
            string aciklamaD)
        {
            if (!Enum.TryParse(disiplinMetni.Trim(), ignoreCase: true, out DisiplinTuru disiplin) ||
                !Enum.TryParse(zorlukMetni.Trim(), ignoreCase: true, out ZorlukSeviyesi zorluk) ||
                !int.TryParse(dogruIndexMetni.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int dogruIndex))
            {
                return null;
            }

            var aciklamalar = new Dictionary<int, string>();
            if (!string.IsNullOrWhiteSpace(aciklamaA)) aciklamalar[0] = aciklamaA.Trim();
            if (!string.IsNullOrWhiteSpace(aciklamaB)) aciklamalar[1] = aciklamaB.Trim();
            if (!string.IsNullOrWhiteSpace(aciklamaC)) aciklamalar[2] = aciklamaC.Trim();
            if (!string.IsNullOrWhiteSpace(aciklamaD)) aciklamalar[3] = aciklamaD.Trim();

            return new Soru
            {
                Disiplin = disiplin,
                Zorluk = zorluk,
                Etiket = string.IsNullOrWhiteSpace(etiket) ? null : etiket.Trim(),
                Baslik = baslik.Trim(),
                KodBlogu = string.IsNullOrWhiteSpace(kodBlogu) ? null : kodBlogu.Replace("\\n", "\n"),
                Secenekler = new List<string>
                {
                    secenekA.Trim(),
                    secenekB.Trim(),
                    secenekC.Trim(),
                    secenekD.Trim()
                },
                DogruSecenekIndex = dogruIndex,
                YanlisCevapAciklamalari = aciklamalar
            };
        }

        // ==================== VERİTABANI YAZMA / OKUMA ====================
        private static Soru Normallestir(Soru soru)
        {
            soru.Baslik = (soru.Baslik ?? string.Empty).Trim();
            soru.Etiket = string.IsNullOrWhiteSpace(soru.Etiket) ? null : soru.Etiket.Trim();
            soru.KodBlogu = string.IsNullOrWhiteSpace(soru.KodBlogu) ? null : soru.KodBlogu;
            soru.Secenekler ??= new List<string>();
            soru.YanlisCevapAciklamalari ??= new Dictionary<int, string>();

            var bosAciklamalar = soru.YanlisCevapAciklamalari
                .Where(kv => string.IsNullOrWhiteSpace(kv.Value))
                .Select(kv => kv.Key)
                .ToList();
            foreach (int anahtar in bosAciklamalar)
                soru.YanlisCevapAciklamalari.Remove(anahtar);

            return soru;
        }

        private static bool SoruEkle(NpgsqlConnection baglanti, NpgsqlTransaction islem, Soru soru)
        {
            const string sql = @"
                INSERT INTO sorular (
                    disiplin, zorluk, etiket, baslik, kod_blogu,
                    secenek_a, secenek_b, secenek_c, secenek_d,
                    dogru_secenek_index, aciklama_a, aciklama_b, aciklama_c, aciklama_d
                ) VALUES (
                    @disiplin, @zorluk, @etiket, @baslik, @kod_blogu,
                    @secenek_a, @secenek_b, @secenek_c, @secenek_d,
                    @dogru_secenek_index, @aciklama_a, @aciklama_b, @aciklama_c, @aciklama_d
                )
                ON CONFLICT (baslik) DO NOTHING;";

            using var komut = new NpgsqlCommand(sql, baglanti, islem);
            ParametreleriBagla(komut, soru);
            return komut.ExecuteNonQuery() > 0;
        }

        private static bool SoruEkleVeyaGuncelle(NpgsqlConnection baglanti, NpgsqlTransaction islem, Soru soru)
        {
            const string sql = @"
                INSERT INTO sorular (
                    disiplin, zorluk, etiket, baslik, kod_blogu,
                    secenek_a, secenek_b, secenek_c, secenek_d,
                    dogru_secenek_index, aciklama_a, aciklama_b, aciklama_c, aciklama_d
                ) VALUES (
                    @disiplin, @zorluk, @etiket, @baslik, @kod_blogu,
                    @secenek_a, @secenek_b, @secenek_c, @secenek_d,
                    @dogru_secenek_index, @aciklama_a, @aciklama_b, @aciklama_c, @aciklama_d
                )
                ON CONFLICT (baslik) DO UPDATE SET
                    disiplin            = EXCLUDED.disiplin,
                    zorluk              = EXCLUDED.zorluk,
                    etiket              = EXCLUDED.etiket,
                    kod_blogu           = EXCLUDED.kod_blogu,
                    secenek_a           = EXCLUDED.secenek_a,
                    secenek_b           = EXCLUDED.secenek_b,
                    secenek_c           = EXCLUDED.secenek_c,
                    secenek_d           = EXCLUDED.secenek_d,
                    dogru_secenek_index = EXCLUDED.dogru_secenek_index,
                    aciklama_a          = EXCLUDED.aciklama_a,
                    aciklama_b          = EXCLUDED.aciklama_b,
                    aciklama_c          = EXCLUDED.aciklama_c,
                    aciklama_d          = EXCLUDED.aciklama_d,
                    guncelleme_tarihi   = CURRENT_TIMESTAMP;";

            using var komut = new NpgsqlCommand(sql, baglanti, islem);
            ParametreleriBagla(komut, soru);
            return komut.ExecuteNonQuery() > 0;
        }

        private static void ParametreleriBagla(NpgsqlCommand komut, Soru soru)
        {
            komut.Parameters.AddWithValue("@disiplin", (short)soru.Disiplin);
            komut.Parameters.AddWithValue("@zorluk", (short)soru.Zorluk);
            komut.Parameters.AddWithValue("@etiket", (object?)soru.Etiket ?? DBNull.Value);
            komut.Parameters.AddWithValue("@baslik", soru.Baslik);
            komut.Parameters.AddWithValue("@kod_blogu", (object?)soru.KodBlogu ?? DBNull.Value);
            komut.Parameters.AddWithValue("@secenek_a", soru.Secenekler[0]);
            komut.Parameters.AddWithValue("@secenek_b", soru.Secenekler[1]);
            komut.Parameters.AddWithValue("@secenek_c", soru.Secenekler[2]);
            komut.Parameters.AddWithValue("@secenek_d", soru.Secenekler[3]);
            komut.Parameters.AddWithValue("@dogru_secenek_index", (short)soru.DogruSecenekIndex);
            komut.Parameters.AddWithValue("@aciklama_a",
                soru.YanlisCevapAciklamalari.TryGetValue(0, out string? a) ? a : DBNull.Value);
            komut.Parameters.AddWithValue("@aciklama_b",
                soru.YanlisCevapAciklamalari.TryGetValue(1, out string? b) ? b : DBNull.Value);
            komut.Parameters.AddWithValue("@aciklama_c",
                soru.YanlisCevapAciklamalari.TryGetValue(2, out string? c) ? c : DBNull.Value);
            komut.Parameters.AddWithValue("@aciklama_d",
                soru.YanlisCevapAciklamalari.TryGetValue(3, out string? d) ? d : DBNull.Value);
        }

        private static List<Soru> Oku(NpgsqlCommand komut)
        {
            var sonuc = new List<Soru>();
            using var okuyucu = komut.ExecuteReader();
            while (okuyucu.Read())
            {
                var aciklamalar = new Dictionary<int, string>();
                if (!okuyucu.IsDBNull(11)) aciklamalar[0] = okuyucu.GetString(11);
                if (!okuyucu.IsDBNull(12)) aciklamalar[1] = okuyucu.GetString(12);
                if (!okuyucu.IsDBNull(13)) aciklamalar[2] = okuyucu.GetString(13);
                if (!okuyucu.IsDBNull(14)) aciklamalar[3] = okuyucu.GetString(14);

                var soru = new Soru
                {
                    Id = okuyucu.GetInt32(0),
                    Disiplin = (DisiplinTuru)okuyucu.GetInt16(1),
                    Zorluk = (ZorlukSeviyesi)okuyucu.GetInt16(2),
                    Etiket = okuyucu.IsDBNull(3) ? null : okuyucu.GetString(3),
                    Baslik = okuyucu.GetString(4),
                    KodBlogu = okuyucu.IsDBNull(5) ? null : okuyucu.GetString(5),
                    Secenekler = new List<string>
                    {
                        okuyucu.GetString(6),
                        okuyucu.GetString(7),
                        okuyucu.GetString(8),
                        okuyucu.GetString(9)
                    },
                    DogruSecenekIndex = okuyucu.GetInt16(10),
                    YanlisCevapAciklamalari = aciklamalar
                };

                sonuc.Add(Normallestir(soru));
            }

            return sonuc;
        }

        // ==================== BAŞLANGIÇ SORU TOHUMLAMA ====================
        /// <summary>
        /// PostgreSQL 'sorular' tablosu boşsa varsayılan 18 soruluk eğitim havuzunu
        /// (3 genel + her modül için 5 ara sınav sorusu) tek bir transaction ile yükler.
        /// </summary>
        public int VarsayilanSorulariTohumla()
        {
            return SorulariVeritabaninaAktar(VarsayilanSoruHavuzu(), mevcutlariGuncelle: true);
        }

        public static IReadOnlyList<Soru> VarsayilanSoruHavuzu() => new List<Soru>
        {
            new()
            {
                Disiplin = DisiplinTuru.GorselProgramlama,
                Zorluk = ZorlukSeviyesi.Orta,
                Baslik = "Aşağıdaki C# event handler (olay yöneticisi) kodu tetiklendiğinde, arayüzün kilitlenmesini önlemek ve arka planda veri çekmek için hangi yöntem kullanılmalıdır?",
                KodBlogu = "private void btnVericek_Click(object sender, EventArgs e)\n{\n    lblDurum.Text = \"Veri alınıyor...\";\n    // Bu işlemin arayüzü kilitlememesi için ne yapılmalı?\n    VeriyiVeritabanindanGetir();\n    lblDurum.Text = \"Veri yüklendi.\";\n}",
                Secenekler = new List<string>
                {
                    "Kodu bir try-catch bloğu içine alarak hataları yakalamak.",
                    "Metodu Thread.Sleep(5000) ile 5 saniye boyunca bekletmek.",
                    "Metodu async tanımlayıp, await Task.Run(() => VeriyiVeritabanindanGetir()) kullanmak.",
                    "Application.DoEvents() metodunu döngü içerisinde sık sık çağırmak."
                },
                DogruSecenekIndex = 2,
                YanlisCevapAciklamalari = new Dictionary<int, string>
                {
                    [0] = "Try-catch blokları sadece çalışma zamanı hatalarını yakalar; arayüz (UI) iş parçacığının donmasını engellemez.",
                    [1] = "Thread.Sleep() doğrudan ana arayüz iş parçacığını dondurur ve uygulamanın tamamen kilitlenmesine yol açar.",
                    [3] = "Application.DoEvents() eski ve riskli bir tekniktir; modern mimarilerde beklenmedik çökmelere yol açar."
                }
            },
            new()
            {
                Disiplin = DisiplinTuru.VeriTabanlari,
                Zorluk = ZorlukSeviyesi.Kolay,
                Baslik = "Bir ilişkisel veri tabanında (PostgreSQL/SQL) iki farklı tablodaki verileri ortak bir sütun üzerinden eşleştirerek tek bir sorguda listelemek için hangi SQL komutu kullanılır?",
                KodBlogu = "SELECT Ogrenciler.Ad, Notlar.Puan\nFROM Ogrenciler\n??? Notlar ON Ogrenciler.Id = Notlar.OgrenciId;",
                Secenekler = new List<string>
                {
                    "JOIN (INNER JOIN)",
                    "GROUP BY",
                    "ORDER BY",
                    "DISTINCT"
                },
                DogruSecenekIndex = 0,
                YanlisCevapAciklamalari = new Dictionary<int, string>
                {
                    [1] = "GROUP BY verileri belirli sütunlara göre gruplamak (özetlemek) için kullanılır, tabloları birleştirmek için değil.",
                    [2] = "ORDER BY gelen sorgu sonucunu sıralamak (A-Z veya artan/azalan) için kullanılır.",
                    [3] = "DISTINCT sorgu sonucundaki mükerrer (tekrar eden) kayıtları teke indirmek için kullanılır."
                }
            },
            new()
            {
                Disiplin = DisiplinTuru.SiberGuvenlik,
                Zorluk = ZorlukSeviyesi.Kolay,
                Baslik = "Kullanıcıdan alınan şifrelerin veri tabanında güvenli bir şekilde saklanması için aşağıdaki yöntemlerden hangisi uygulanmalıdır?",
                KodBlogu = "// Kullanıcı Kaydı: Şifre kaydetme işlemi\nstring sifre = txtSifre.Text;\n// Veri tabanına nasıl kaydedilmeli?",
                Secenekler = new List<string>
                {
                    "Şifre doğrudan düz metin (plain text) olarak kaydedilmelidir.",
                    "Şifre tek başına tuzsuz hızlı özetleme ile kaydedilmelidir.",
                    "Şifre, her kullanıcı için rastgele bir tuz (salt) ile birlikte BCrypt/Argon2 gibi yavaş bir özetleme algoritmasından geçirilerek kaydedilmelidir.",
                    "Şifre Base64 ile kodlanıp düz metin gibi saklanmalıdır."
                },
                DogruSecenekIndex = 2,
                YanlisCevapAciklamalari = new Dictionary<int, string>
                {
                    [0] = "Düz metin saklanan şifreler, veri tabanı sızdığında anında ifşa olur.",
                    [1] = "Tuzsuz hızlı özetleme aynı şifreler için aynı özeti üretir; rainbow table ve kaba kuvvet saldırılarına karşı zayıftır.",
                    [3] = "Base64 bir şifreleme değil, sadece format dönüştürmedir; herkes tek tıkla orijinal şifreyi çözebilir."
                }
            },
            new()
            {
                Disiplin = DisiplinTuru.GorselProgramlama,
                Zorluk = ZorlukSeviyesi.Kolay,
                Etiket = AraSinavEtiketi,
                Baslik = "WinForms'ta bir panele çok sayıda kontrol eklerken `SuspendLayout()` ve `ResumeLayout()` kullanmanın temel amacı nedir?",
                Secenekler = new List<string>
                {
                    "Kontrollerin görünürlüğünü tamamen kapatmak.",
                    "Yerleşim hesaplamalarını geçici olarak askıya alarak ekranın titremesini ve gereksiz yeniden çizimi azaltmak.",
                    "Olay (event) işleyicilerinin çalışmasını kalıcı olarak engellemek.",
                    "Veritabanı bağlantısını kapatmak."
                },
                DogruSecenekIndex = 1,
                YanlisCevapAciklamalari = new Dictionary<int, string>
                {
                    [0] = "SuspendLayout görünürlüğü değiştirmez; yalnızca yerleşim hesaplamalarını erteler.",
                    [2] = "Olay işleyicileri engellenmez; yalnızca yerleşim (layout) işlemleri ertelenir.",
                    [3] = "Bu metotların veritabanı bağlantısıyla ilgisi yoktur."
                }
            },
            new()
            {
                Disiplin = DisiplinTuru.GorselProgramlama,
                Zorluk = ZorlukSeviyesi.Kolay,
                Etiket = AraSinavEtiketi,
                Baslik = "C# dilinde `using` deyimi hangi tür nesneler için kullanılır?",
                Secenekler = new List<string>
                {
                    "Yalnızca int ve string gibi değer türleri için.",
                    "IDisposable arayüzünü uygulayan ve serbest bırakılması gereken kaynaklar için.",
                    "Sadece Windows Forms kontrolleri için.",
                    "Yalnızca statik sınıflar için."
                },
                DogruSecenekIndex = 1,
                YanlisCevapAciklamalari = new Dictionary<int, string>
                {
                    [0] = "Değer türleri için using gerekmez; using kaynak yönetimi içindir.",
                    [2] = "WinForms kontrolleri de IDisposable olabilir; ancak using yalnızca onlara özel değildir.",
                    [3] = "Statik sınıflar IDisposable uygulamaz; using onlar için kullanılmaz."
                }
            },
            new()
            {
                Disiplin = DisiplinTuru.GorselProgramlama,
                Zorluk = ZorlukSeviyesi.Kolay,
                Etiket = AraSinavEtiketi,
                Baslik = "Bir WinForms olay işleyicisinin (event handler) standart imzası aşağıdakilerden hangisidir?",
                Secenekler = new List<string>
                {
                    "void Metot()",
                    "void Metot(object sender, EventArgs e)",
                    "int Metot(string metin)",
                    "Task Metot(int sayi)"
                },
                DogruSecenekIndex = 1,
                YanlisCevapAciklamalari = new Dictionary<int, string>
                {
                    [0] = "Parametresiz metot standart olay imzası değildir; olay kaynağı ve verisi aktarılamaz.",
                    [2] = "Olay işleyicisi string döndürmez; olaylar genellikle void döner.",
                    [3] = "Task döndüren metotlar async olabilir; ancak standart EventHandler imzası bu değildir."
                }
            },
            new()
            {
                Disiplin = DisiplinTuru.GorselProgramlama,
                Zorluk = ZorlukSeviyesi.Kolay,
                Etiket = AraSinavEtiketi,
                Baslik = "Uzun süren bir veritabanı sorgusunu arayüz iş parçacığında doğrudan çalıştırmak neden sakıncalıdır?",
                Secenekler = new List<string>
                {
                    "Sorgu sonucu yanlış döner.",
                    "Arayüz iş parçacığı meşgul olduğu için pencere donar ve kullanıcıya yanıt vermez.",
                    "PostgreSQL sorguları yalnızca arka planda çalışabilir.",
                    "Veritabanı otomatik olarak silinir."
                },
                DogruSecenekIndex = 1,
                YanlisCevapAciklamalari = new Dictionary<int, string>
                {
                    [0] = "Sorgu sonucu bu nedenle yanlış olmaz; sorun arayüzün yanıt vermemesidir.",
                    [2] = "Sorgular ana iş parçacığında da çalışabilir; ancak uzun sorgular arayüzü kilitler.",
                    [3] = "Veritabanı bu nedenle silinmez."
                }
            },
            new()
            {
                Disiplin = DisiplinTuru.GorselProgramlama,
                Zorluk = ZorlukSeviyesi.Kolay,
                Etiket = AraSinavEtiketi,
                Baslik = "Çift tamponlama (double buffering) hangi sorunu azaltmak için kullanılır?",
                Secenekler = new List<string>
                {
                    "Veritabanı kilitlenmelerini",
                    "Ekranın titremesini (flicker) ve yeniden çizim sırasındaki görsel bozulmaları",
                    "İnternet bağlantısı kopmalarını",
                    "Şifreleme anahtarının kaybolmasını"
                },
                DogruSecenekIndex = 1,
                YanlisCevapAciklamalari = new Dictionary<int, string>
                {
                    [0] = "Çift tamponlama veritabanı kilitlemesiyle ilgili değildir.",
                    [2] = "İnternet bağlantısıyla ilgisi yoktur.",
                    [3] = "Şifreleme anahtarı yönetimi tamamen farklı bir konudur."
                }
            },
            new()
            {
                Disiplin = DisiplinTuru.VeriTabanlari,
                Zorluk = ZorlukSeviyesi.Kolay,
                Etiket = AraSinavEtiketi,
                Baslik = "İlişkisel bir veri tabanında PRIMARY KEY (birincil anahtar) neyi garanti eder?",
                Secenekler = new List<string>
                {
                    "Sütundaki değerlerin benzersiz olmasını ve NULL olamamasını",
                    "Tablodaki tüm sütunların otomatik şifrelenmesini",
                    "Sorguların her zaman indekssiz çalışmasını",
                    "Tablodaki kayıt sayısının sınırsız olmasını"
                },
                DogruSecenekIndex = 0,
                YanlisCevapAciklamalari = new Dictionary<int, string>
                {
                    [1] = "PRIMARY KEY şifreleme yapmaz; yalnızca benzersizlik ve NULL olmama kuralı getirir.",
                    [2] = "PRIMARY KEY genellikle bir indeks oluşturur; sorguların indekssiz çalışmasını sağlamaz.",
                    [3] = "Kayıt sayısı sınırıyla doğrudan ilgisi yoktur."
                }
            },
            new()
            {
                Disiplin = DisiplinTuru.VeriTabanlari,
                Zorluk = ZorlukSeviyesi.Kolay,
                Etiket = AraSinavEtiketi,
                Baslik = "FOREIGN KEY (yabancı anahtar) hangi veri bütünlüğü kuralını uygular?",
                Secenekler = new List<string>
                {
                    "Bir tablodaki kaydın, başka bir tabloda var olan bir kayda bağlı olmasını",
                    "Tüm metin alanlarının büyük harfe çevrilmesini",
                    "Veritabanının her açılışta silinmesini",
                    "Sorguların yalnızca tek tabloda çalışmasını"
                },
                DogruSecenekIndex = 0,
                YanlisCevapAciklamalari = new Dictionary<int, string>
                {
                    [1] = "FOREIGN KEY harf dönüşümü yapmaz.",
                    [2] = "Veritabanının silinmesine yol açmaz; ilişkisel bütünlük sağlar.",
                    [3] = "Aksine, tablolar arasında ilişki kurulmasını sağlar."
                }
            },
            new()
            {
                Disiplin = DisiplinTuru.VeriTabanlari,
                Zorluk = ZorlukSeviyesi.Kolay,
                Etiket = AraSinavEtiketi,
                Baslik = "SQL'de `SELECT COUNT(*) FROM Ogrenciler;` sorgusu ne döndürür?",
                Secenekler = new List<string>
                {
                    "Ogrenciler tablosundaki toplam kayıt sayısını",
                    "Ogrenciler tablosunun tüm sütun adlarını",
                    "Yalnızca ilk öğrencinin adını",
                    "Tablodaki benzersiz sütun sayısını"
                },
                DogruSecenekIndex = 0,
                YanlisCevapAciklamalari = new Dictionary<int, string>
                {
                    [1] = "Sütun adları için information_schema.columns sorguları kullanılır.",
                    [2] = "İlk kaydı getirmek için SELECT ... LIMIT 1 gerekir.",
                    [3] = "COUNT(*) sütun sayısını değil, satır sayısını sayar."
                }
            },
            new()
            {
                Disiplin = DisiplinTuru.VeriTabanlari,
                Zorluk = ZorlukSeviyesi.Kolay,
                Etiket = AraSinavEtiketi,
                Baslik = "Bir PostgreSQL işleminde (transaction) `COMMIT` ve `ROLLBACK` arasındaki fark nedir?",
                Secenekler = new List<string>
                {
                    "COMMIT değişiklikleri kalıcı yazar; ROLLBACK işlem başladıktan sonraki değişiklikleri geri alır.",
                    "COMMIT yalnızca tablo siler; ROLLBACK yeni tablo oluşturur.",
                    "İkisi de aynı işi yapar; yalnızca isimleri farklıdır.",
                    "COMMIT yalnızca okuma yapar; ROLLBACK yazma yapar."
                },
                DogruSecenekIndex = 0,
                YanlisCevapAciklamalari = new Dictionary<int, string>
                {
                    [1] = "COMMIT tablo silmez; işlemi onaylar.",
                    [2] = "Aynı işi yapmazlar; biri onaylar, diğeri geri alır.",
                    [3] = "COMMIT yazma işlemini kalıcılaştırır; ROLLBACK onu iptal eder."
                }
            },
            new()
            {
                Disiplin = DisiplinTuru.VeriTabanlari,
                Zorluk = ZorlukSeviyesi.Kolay,
                Etiket = AraSinavEtiketi,
                Baslik = "Veri tabanında indeks (index) kullanmanın temel etkisi nedir?",
                Secenekler = new List<string>
                {
                    "Okuma/sorgu hızını artırır, ancak ekleme-güncelleme işlemlerinde ek maliyet getirir.",
                    "Tüm verileri otomatik olarak şifreler.",
                    "Tablodaki yinelenen kayıtları siler.",
                    "Veritabanı boyutunu küçültür."
                },
                DogruSecenekIndex = 0,
                YanlisCevapAciklamalari = new Dictionary<int, string>
                {
                    [1] = "İndeks şifreleme yapmaz.",
                    [2] = "İndeks yinelenen kayıtları silmez; UNIQUE kısıtı bunu engeller.",
                    [3] = "İndeks ek disk alanı kullanır; boyutu küçültmez."
                }
            },
            new()
            {
                Disiplin = DisiplinTuru.SiberGuvenlik,
                Zorluk = ZorlukSeviyesi.Kolay,
                Etiket = AraSinavEtiketi,
                Baslik = "BCrypt gibi tuzlu (salted) parola özetleme algoritmalarının tercih edilmesinin temel nedeni nedir?",
                Secenekler = new List<string>
                {
                    "Veritabanı bağlantısını hızlandırması.",
                    "Her parola için benzersiz tuz (salt) üretmesi ve ayarlanabilir maliyet faktörüyle kaba kuvvet/rainbow table saldırılarını zorlaştırması.",
                    "Parolanın istendiğinde düz metne geri döndürülebilmesi.",
                    "Yalnızca sayısal şifrelerde çalışması."
                },
                DogruSecenekIndex = 1,
                YanlisCevapAciklamalari = new Dictionary<int, string>
                {
                    [0] = "BCrypt güvenlik için kasıtlı olarak yavaşlatılmış bir algoritmadır; bağlantı hızıyla ilgisi yoktur.",
                    [2] = "BCrypt tek yönlü bir özetleme algoritmasıdır; parola geri çözülemez.",
                    [3] = "BCrypt her türlü metin ve karakter dizisi üzerinde çalışır."
                }
            },
            new()
            {
                Disiplin = DisiplinTuru.SiberGuvenlik,
                Zorluk = ZorlukSeviyesi.Kolay,
                Etiket = AraSinavEtiketi,
                Baslik = "Hash (özetleme) ile şifreleme (encryption) arasındaki temel fark nedir?",
                Secenekler = new List<string>
                {
                    "Hash tek yönlüdür ve geri döndürülemez; şifreleme anahtarla geri döndürülebilir.",
                    "Hash yalnızca metinlerde, şifreleme yalnızca sayılarda kullanılır.",
                    "İkisi de aynıdır; sadece farklı isimlerdir.",
                    "Şifreleme her zaman anahtarsızdır."
                },
                DogruSecenekIndex = 0,
                YanlisCevapAciklamalari = new Dictionary<int, string>
                {
                    [1] = "Her ikisi de metin ve bayt dizileri üzerinde çalışabilir.",
                    [2] = "Aynı değildir; hash geri döndürülemez, şifreleme anahtarla çözülebilir.",
                    [3] = "Şifreleme anahtar kullanır; anahtarsız şifreleme güvenli değildir."
                }
            },
            new()
            {
                Disiplin = DisiplinTuru.SiberGuvenlik,
                Zorluk = ZorlukSeviyesi.Kolay,
                Etiket = AraSinavEtiketi,
                Baslik = "Npgsql ile PostgreSQL sorgusu yazarken SQL Injection saldırılarını önlemek için ne yapılmalıdır?",
                Secenekler = new List<string>
                {
                    "Kullanıcı girdisini doğrudan string birleştirme (+) ile SQL cümlesine eklemek.",
                    "Mutlaka parametreli sorgular (@parametre) ve NpgsqlParameter kullanmak.",
                    "Tablo adlarını gizlemek.",
                    "Sorguları yalnızca büyük harfle yazmak."
                },
                DogruSecenekIndex = 1,
                YanlisCevapAciklamalari = new Dictionary<int, string>
                {
                    [0] = "String birleştirme SQL Injection zafiyetinin birincil kaynağıdır.",
                    [2] = "Tablo adlarını gizlemek SQL Injection saldırısını engellemez.",
                    [3] = "Büyük/küçük harf kullanımı güvenliği etkilemez."
                }
            },
            new()
            {
                Disiplin = DisiplinTuru.SiberGuvenlik,
                Zorluk = ZorlukSeviyesi.Kolay,
                Etiket = AraSinavEtiketi,
                Baslik = "Veritabanı bağlantı bilgilerinin (ConnectionString) kaynak kod içine gömülmemesi neden önemlidir?",
                Secenekler = new List<string>
                {
                    "Kod içindeki sabit parolalar versiyon kontrol sisteminde (Git) sızabilir ve ortam değiştiğinde yeniden derleme gerektirir.",
                    "Derleyici bağlantı dizelerini derleyemez.",
                    "PostgreSQL yalnızca şifresiz bağlantıları kabul eder.",
                    "WinForms uygulamaları string sabitleri desteklemez."
                },
                DogruSecenekIndex = 0,
                YanlisCevapAciklamalari = new Dictionary<int, string>
                {
                    [1] = "Derleyici string sabitleri derler; ancak güvenlik ve yapılandırma açısından yanlıştır.",
                    [2] = "PostgreSQL parola doğrulamasını destekler ve gerektirir.",
                    [3] = "WinForms her türlü C# string sabitini destekler."
                }
            },
            new()
            {
                Disiplin = DisiplinTuru.SiberGuvenlik,
                Zorluk = ZorlukSeviyesi.Kolay,
                Etiket = AraSinavEtiketi,
                Baslik = "Şifre saklarken kullanılan tuz (salt) değeri ne işe yarar?",
                Secenekler = new List<string>
                {
                    "Aynı şifreye sahip kullanıcıların farklı özetler üretmesini sağlar ve rainbow table saldırılarını zorlaştırır.",
                    "Şifreyi kısaltarak daha hızlı doğrulama yapar.",
                    "Şifreyi herkese açık biçimde paylaşır.",
                    "Veritabanı bağlantısını hızlandırır."
                },
                DogruSecenekIndex = 0,
                YanlisCevapAciklamalari = new Dictionary<int, string>
                {
                    [1] = "Tuz şifreyi kısaltmaz; özete eklenen rastgele bir değerdir.",
                    [2] = "Tuz gizli değildir ama şifreyi paylaşmaz; yalnızca özeti benzersizleştirir.",
                    [3] = "Tuzun veritabanı bağlantı hızıyla ilgisi yoktur."
                }
            }
        };
    }
}
