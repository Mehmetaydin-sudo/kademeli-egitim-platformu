using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using KademeliEgitimPlatformu.Models;
using Npgsql;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>
    /// Öğrenci profili, XP/seviye, rozet vitrini, günlük görev ve tema tercihlerini yönetir.
    /// Tercihler birincil olarak PostgreSQL 'meta' tablosunda saklanır; veritabanı kapalıysa
    /// yalnızca kullanıcıya özel yazılabilir VeriYolu.VeriKlasoru (Belgelerim) yedeği kullanılır.
    /// </summary>
    public class KisisellestirmeServisi
    {
        private const string VarsayilanMetaAnahtari = "kisisellestirme_tercihi";

        // Zayıf referans tablosu: Dispose edilen WinForms kontrolleri bellekte kilitlenmez.
        private static readonly ConditionalWeakTable<Control, StrongBox<float>> OrijinalFontBoyutlari = new();

        private readonly string? _baglantiMetni;
        private readonly string _metaAnahtari;
        private readonly string _yedekDosyaYolu;

        public KisisellestirmeServisi(
            string? ozelBaglantiDizesi = null,
            string? metaAnahtari = null,
            string? ozelYedekDosyaYolu = null)
        {
            _baglantiMetni = ozelBaglantiDizesi;
            _metaAnahtari = string.IsNullOrWhiteSpace(metaAnahtari) ? VarsayilanMetaAnahtari : metaAnahtari.Trim();
            _yedekDosyaYolu = string.IsNullOrWhiteSpace(ozelYedekDosyaYolu)
                ? Path.Combine(VeriYolu.VeriKlasoru, "kullanici_tercihi.cfg")
                : ozelYedekDosyaYolu;
        }

        public List<string> AvatarSecenekleri { get; } = new()
        {
            "🎓", "👨‍💻", "👩‍💻", "📚", "🧠", "🚀", "⚡", "🎯",
            "🛡️", "🗄️", "🔬", "📐", "🔥", "💎", "🌟", "🏆"
        };

        public List<TemaPaleti> HazirTemalar { get; } = new()
        {
            new("Koyu Klasik",      Color.FromArgb(15, 23, 42),    Color.FromArgb(9, 13, 24),     Color.FromArgb(22, 32, 50),    Color.FromArgb(56, 189, 248),  Color.FromArgb(241, 245, 249)),
            new("Gece Mavisi",      Color.FromArgb(18, 25, 38),    Color.FromArgb(13, 19, 30),    Color.FromArgb(28, 38, 56),    Color.FromArgb(56, 189, 248),  Color.FromArgb(241, 245, 249)),
            new("Antrasit Gri",     Color.FromArgb(34, 36, 40),    Color.FromArgb(26, 28, 31),    Color.FromArgb(48, 51, 56),    Color.FromArgb(148, 163, 184), Color.FromArgb(244, 244, 246)),
            new("Zümrüt Koyu",      Color.FromArgb(17, 30, 27),    Color.FromArgb(12, 22, 20),    Color.FromArgb(26, 46, 42),    Color.FromArgb(16, 185, 129),  Color.FromArgb(236, 253, 245)),
            new("Ametist Gece",     Color.FromArgb(28, 22, 42),    Color.FromArgb(20, 16, 32),    Color.FromArgb(42, 34, 62),    Color.FromArgb(168, 85, 247),  Color.FromArgb(245, 240, 255)),
            new("Sıcak Kahve",      Color.FromArgb(32, 26, 23),    Color.FromArgb(24, 19, 17),    Color.FromArgb(48, 39, 35),    Color.FromArgb(245, 158, 11),  Color.FromArgb(250, 245, 238)),
            new("Kömür Siyahı",     Color.FromArgb(16, 16, 18),    Color.FromArgb(11, 11, 13),    Color.FromArgb(28, 28, 32),    Color.FromArgb(226, 232, 240), Color.FromArgb(248, 250, 252)),
            new("Okyanus Derini",   Color.FromArgb(14, 28, 36),    Color.FromArgb(10, 20, 26),    Color.FromArgb(22, 44, 56),    Color.FromArgb(14, 165, 233),  Color.FromArgb(240, 249, 255)),
            new("Kızıl Gece",       Color.FromArgb(32, 17, 21),    Color.FromArgb(23, 12, 15),    Color.FromArgb(50, 26, 32),    Color.FromArgb(244, 63, 94),   Color.FromArgb(255, 241, 242)),
            new("Altın Karbon",     Color.FromArgb(24, 23, 20),    Color.FromArgb(17, 16, 14),    Color.FromArgb(38, 36, 31),    Color.FromArgb(234, 179, 8),   Color.FromArgb(254, 252, 232)),
            new("Dracula",          Color.FromArgb(40, 42, 54),    Color.FromArgb(33, 34, 44),    Color.FromArgb(52, 55, 70),    Color.FromArgb(189, 147, 249), Color.FromArgb(248, 248, 242)),
            new("Tokyo Gece",       Color.FromArgb(26, 27, 38),    Color.FromArgb(21, 22, 30),    Color.FromArgb(38, 41, 58),    Color.FromArgb(122, 162, 247), Color.FromArgb(213, 220, 248)),
            new("Nord Buzul",       Color.FromArgb(46, 52, 64),    Color.FromArgb(36, 41, 51),    Color.FromArgb(59, 66, 82),    Color.FromArgb(136, 192, 208), Color.FromArgb(236, 239, 244)),
            new("Monokai Koyu",     Color.FromArgb(45, 42, 46),    Color.FromArgb(34, 31, 34),    Color.FromArgb(64, 60, 65),    Color.FromArgb(255, 216, 102), Color.FromArgb(252, 252, 250)),
            new("Gruvbox Koyu",     Color.FromArgb(40, 40, 40),    Color.FromArgb(29, 32, 33),    Color.FromArgb(60, 56, 54),    Color.FromArgb(254, 128, 25),  Color.FromArgb(235, 219, 178)),
            new("Solarized Koyu",   Color.FromArgb(0, 43, 54),     Color.FromArgb(0, 33, 43),     Color.FromArgb(7, 54, 66),     Color.FromArgb(42, 161, 152),  Color.FromArgb(238, 232, 213)),
            new("Mocha Pastel",     Color.FromArgb(30, 30, 46),    Color.FromArgb(24, 24, 37),    Color.FromArgb(49, 50, 68),    Color.FromArgb(245, 194, 231), Color.FromArgb(205, 214, 244)),
            new("Gül Kurusu Koyu",  Color.FromArgb(31, 26, 36),    Color.FromArgb(24, 20, 28),    Color.FromArgb(46, 39, 54),    Color.FromArgb(235, 111, 146), Color.FromArgb(235, 230, 245)),
            new("Neon Mavi",        Color.FromArgb(13, 20, 36),    Color.FromArgb(9, 14, 26),     Color.FromArgb(20, 32, 56),    Color.FromArgb(0, 212, 255),   Color.FromArgb(240, 248, 255)),
            new("Matrix Yeşili",    Color.FromArgb(12, 20, 14),    Color.FromArgb(8, 14, 10),     Color.FromArgb(20, 34, 24),    Color.FromArgb(34, 197, 94),   Color.FromArgb(220, 252, 231)),
            new("Turkuaz Koyu",     Color.FromArgb(15, 30, 32),    Color.FromArgb(10, 21, 23),    Color.FromArgb(24, 48, 51),    Color.FromArgb(20, 184, 166),  Color.FromArgb(240, 253, 250)),
            new("Siber Pembe",      Color.FromArgb(30, 16, 34),    Color.FromArgb(21, 11, 24),    Color.FromArgb(48, 25, 54),    Color.FromArgb(236, 72, 153),  Color.FromArgb(253, 242, 248)),
            new("İndigo Gece",      Color.FromArgb(21, 22, 44),    Color.FromArgb(15, 16, 33),    Color.FromArgb(34, 36, 70),    Color.FromArgb(99, 102, 241),  Color.FromArgb(238, 242, 255)),
            new("Safir Mavisi",     Color.FromArgb(16, 26, 48),    Color.FromArgb(11, 19, 36),    Color.FromArgb(26, 42, 76),    Color.FromArgb(59, 130, 246),  Color.FromArgb(239, 246, 255)),
            new("Mandalin Koyu",    Color.FromArgb(33, 21, 16),    Color.FromArgb(24, 15, 11),    Color.FromArgb(52, 33, 25),    Color.FromArgb(249, 115, 22),  Color.FromArgb(255, 247, 237)),
            new("Orman Yeşili",     Color.FromArgb(18, 29, 21),    Color.FromArgb(13, 21, 15),    Color.FromArgb(28, 46, 33),    Color.FromArgb(74, 222, 128),  Color.FromArgb(240, 253, 244)),
            new("Bordo Kadife",     Color.FromArgb(34, 16, 22),    Color.FromArgb(25, 11, 16),    Color.FromArgb(54, 25, 34),    Color.FromArgb(251, 113, 133), Color.FromArgb(255, 241, 242)),
            new("Bakır Koyu",       Color.FromArgb(32, 22, 18),    Color.FromArgb(23, 15, 12),    Color.FromArgb(50, 34, 28),    Color.FromArgb(217, 119, 6),   Color.FromArgb(254, 243, 199)),
            new("Fırtına Mavisi",   Color.FromArgb(24, 30, 38),    Color.FromArgb(17, 22, 28),    Color.FromArgb(38, 47, 59),    Color.FromArgb(96, 165, 250),  Color.FromArgb(241, 245, 249)),
            new("Zeytin Koyu",      Color.FromArgb(26, 29, 20),    Color.FromArgb(19, 21, 14),    Color.FromArgb(41, 46, 31),    Color.FromArgb(163, 230, 53),  Color.FromArgb(247, 254, 231)),
            new("Kraliyet Moru",    Color.FromArgb(26, 17, 42),    Color.FromArgb(18, 12, 30),    Color.FromArgb(41, 27, 66),    Color.FromArgb(192, 132, 252), Color.FromArgb(250, 245, 255)),
            new("Grafit Gri",       Color.FromArgb(38, 38, 42),    Color.FromArgb(28, 28, 31),    Color.FromArgb(56, 56, 62),    Color.FromArgb(161, 161, 170), Color.FromArgb(244, 244, 245)),
            new("Çelik Mavisi",     Color.FromArgb(22, 29, 36),    Color.FromArgb(16, 21, 27),    Color.FromArgb(35, 46, 57),    Color.FromArgb(125, 211, 252), Color.FromArgb(240, 249, 255)),
            new("Mercan Koyu",      Color.FromArgb(34, 20, 20),    Color.FromArgb(24, 14, 14),    Color.FromArgb(54, 32, 32),    Color.FromArgb(248, 113, 113), Color.FromArgb(254, 242, 242)),
            new("Gece Yarısı",      Color.FromArgb(15, 18, 30),    Color.FromArgb(10, 12, 21),    Color.FromArgb(24, 29, 48),    Color.FromArgb(129, 140, 248), Color.FromArgb(238, 242, 255)),
            new("Açık Beyaz",       Color.FromArgb(240, 243, 248), Color.FromArgb(226, 232, 240), Color.FromArgb(248, 250, 252), Color.FromArgb(0, 110, 210),   Color.FromArgb(28, 32, 38)),
            new("Buz Mavisi",       Color.FromArgb(222, 239, 255), Color.FromArgb(202, 227, 252), Color.FromArgb(234, 246, 255), Color.FromArgb(2, 132, 199),   Color.FromArgb(20, 40, 60)),
            new("Nane Ferahlığı",   Color.FromArgb(220, 246, 236), Color.FromArgb(198, 236, 222), Color.FromArgb(233, 251, 244), Color.FromArgb(13, 148, 136),  Color.FromArgb(20, 45, 42)),
            new("Krem Kağıt",       Color.FromArgb(246, 239, 224), Color.FromArgb(235, 224, 204), Color.FromArgb(252, 247, 236), Color.FromArgb(180, 110, 30),  Color.FromArgb(45, 38, 30)),
            new("Lavanta Esintisi", Color.FromArgb(236, 228, 255), Color.FromArgb(220, 210, 250), Color.FromArgb(244, 238, 255), Color.FromArgb(124, 58, 237),  Color.FromArgb(38, 28, 60)),
            new("Gül Bahçesi",      Color.FromArgb(255, 226, 235), Color.FromArgb(248, 208, 220), Color.FromArgb(255, 237, 243), Color.FromArgb(225, 29, 72),   Color.FromArgb(55, 24, 34)),
            new("Şeftali Işığı",    Color.FromArgb(255, 235, 218), Color.FromArgb(248, 218, 196), Color.FromArgb(255, 243, 232), Color.FromArgb(234, 88, 12),   Color.FromArgb(56, 32, 18)),
            new("Gümüş Bulut",      Color.FromArgb(226, 232, 240), Color.FromArgb(210, 218, 230), Color.FromArgb(236, 241, 247), Color.FromArgb(71, 85, 105),   Color.FromArgb(15, 23, 42)),
            new("Adaçayı Açık",     Color.FromArgb(224, 241, 226), Color.FromArgb(206, 229, 210), Color.FromArgb(236, 248, 238), Color.FromArgb(22, 163, 74),   Color.FromArgb(22, 44, 26)),
            new("Gökyüzü Açık",     Color.FromArgb(218, 238, 255), Color.FromArgb(196, 226, 252), Color.FromArgb(231, 245, 255), Color.FromArgb(37, 99, 235),   Color.FromArgb(18, 36, 66)),
            new("Vanilya Sıcak",    Color.FromArgb(254, 244, 218), Color.FromArgb(245, 230, 196), Color.FromArgb(255, 249, 232), Color.FromArgb(202, 138, 4),   Color.FromArgb(50, 42, 18)),
            new("Kum Beji",         Color.FromArgb(238, 232, 222), Color.FromArgb(224, 216, 204), Color.FromArgb(246, 242, 234), Color.FromArgb(120, 113, 108), Color.FromArgb(41, 37, 36)),
            new("İnci Beyazı",      Color.FromArgb(234, 236, 252), Color.FromArgb(218, 222, 246), Color.FromArgb(243, 244, 255), Color.FromArgb(79, 70, 229),   Color.FromArgb(30, 27, 55)),
            new("Bahar Yeşili",     Color.FromArgb(230, 247, 218), Color.FromArgb(212, 236, 196), Color.FromArgb(240, 252, 231), Color.FromArgb(101, 163, 13),  Color.FromArgb(32, 48, 16)),
            new("Solarized Açık",   Color.FromArgb(246, 238, 214), Color.FromArgb(234, 224, 196), Color.FromArgb(252, 246, 228), Color.FromArgb(38, 139, 210),  Color.FromArgb(40, 55, 62))
        };

        public List<string> YaziTipleri { get; } = new()
        {
            "Segoe UI",
            "Calibri",
            "Tahoma",
            "Verdana",
            "Consolas",
            "Arial"
        };

        public List<TemaPaleti> TumTemalariGetir(KullaniciTemaTercihi? tercih)
        {
            var liste = new List<TemaPaleti>(HazirTemalar);
            if (tercih?.OzelPaletler != null)
            {
                foreach (OzelRenkPaleti ozel in tercih.OzelPaletler)
                {
                    if (string.IsNullOrWhiteSpace(ozel.Ad)) continue;
                    liste.Add(new TemaPaleti(
                        ozel.Ad,
                        HexToColor(ozel.ArkaPlanHex, Color.FromArgb(15, 23, 42)),
                        HexToColor(ozel.SidebarHex, Color.FromArgb(9, 13, 24)),
                        HexToColor(ozel.KartHex, Color.FromArgb(22, 32, 50)),
                        HexToColor(ozel.VurguHex, Color.FromArgb(56, 189, 248)),
                        HexToColor(ozel.YaziHex, Color.FromArgb(241, 245, 249)),
                        ozelMi: true
                    ));
                }
            }
            return liste;
        }

        public TemaPaleti TemaBul(string temaAdi, KullaniciTemaTercihi? tercih = null)
        {
            var tumu = TumTemalariGetir(tercih);
            var bulunan = tumu.FirstOrDefault(t => t.Ad.Equals(temaAdi, StringComparison.OrdinalIgnoreCase)) ?? tumu[0];

            if (tercih != null && !string.IsNullOrWhiteSpace(tercih.VurguHex))
            {
                Color ozelVurgu = HexToColor(tercih.VurguHex, bulunan.Vurgu);
                return new TemaPaleti(bulunan.Ad, bulunan.ArkaPlan, bulunan.Sidebar, bulunan.Kart, ozelVurgu, bulunan.Yazi, bulunan.OzelMi);
            }

            return bulunan;
        }

        public static OzelRenkPaleti TekRenktenPaletUret(string ad, Color secilenRenk, bool koyuMod)
        {
            if (koyuMod)
            {
                Color arkaPlan = Karistir(Color.FromArgb(22, 24, 28), secilenRenk, 0.12);
                Color sidebar = Karistir(Color.FromArgb(15, 17, 20), secilenRenk, 0.10);
                Color kart = Karistir(Color.FromArgb(34, 37, 44), secilenRenk, 0.16);
                return new OzelRenkPaleti
                {
                    Ad = ad,
                    ArkaPlanHex = ColorToHex(arkaPlan),
                    SidebarHex = ColorToHex(sidebar),
                    KartHex = ColorToHex(kart),
                    VurguHex = ColorToHex(secilenRenk),
                    YaziHex = "#F3F4F6"
                };
            }

            Color acikArka = Karistir(Color.FromArgb(242, 245, 248), secilenRenk, 0.12);
            Color acikSidebar = Karistir(Color.FromArgb(228, 233, 240), secilenRenk, 0.16);
            Color acikKart = Karistir(Color.FromArgb(250, 252, 255), secilenRenk, 0.08);
            return new OzelRenkPaleti
            {
                Ad = ad,
                ArkaPlanHex = ColorToHex(acikArka),
                SidebarHex = ColorToHex(acikSidebar),
                KartHex = ColorToHex(acikKart),
                VurguHex = ColorToHex(secilenRenk),
                YaziHex = "#1E242B"
            };
        }

        private static Color Karistir(Color taban, Color hedef, double oran)
        {
            int r = (int)(taban.R * (1 - oran) + hedef.R * oran);
            int g = (int)(taban.G * (1 - oran) + hedef.G * oran);
            int b = (int)(taban.B * (1 - oran) + hedef.B * oran);
            return Color.FromArgb(Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255));
        }

        public static string ColorToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        public static Color HexToColor(string? hex, Color varsayilan)
        {
            if (string.IsNullOrWhiteSpace(hex)) return varsayilan;
            try { return ColorTranslator.FromHtml(hex.Trim()); }
            catch { return varsayilan; }
        }

        public static bool ParlakMi(Color c) => (c.R * 0.299 + c.G * 0.587 + c.B * 0.114) > 155;

        // ==================== KALICI VERİTABANI + GÜVENLİ YEDEK YÜKLEME / KAYDETME ====================
        /// <summary>
        /// Tercihleri PostgreSQL 'meta' tablosundan yükler; veritabanı kapalıysa
        /// kullanıcının yazılabilir Belgelerim klasöründeki yedekten okur.
        /// </summary>
        public KullaniciTemaTercihi TercihiYukle()
        {
            KullaniciTemaTercihi? tercih = VeritabanindanOku();

            if (tercih == null)
                tercih = YedektenOku();

            tercih ??= new KullaniciTemaTercihi();

            tercih.OzelPaletler ??= new List<OzelRenkPaleti>();
            tercih.GunlukGorevler ??= new List<GunlukGorev>();
            tercih.SonAktiviteler ??= new List<AktiviteKaydi>();
            tercih.HaftalikGecmis ??= new List<GunlukPerformans>();
            tercih.OdullendirilenOlaylar ??= new HashSet<string>();
            tercih.BildirilenRozetler ??= new HashSet<string>();

            GunlukDurumuSenkronizeEt(tercih);
            return tercih;
        }

        /// <summary>
        /// Tercihleri PostgreSQL 'meta' tablosuna parametreli sorguyla kaydeder;
        /// veritabanına erişilemezse kullanıcının Belgelerim\KademeliEgitimPlatformu klasörüne yazar.
        /// Program Files (BaseDirectory) altına asla yazmaz.
        /// </summary>
        public void TercihiKaydet(KullaniciTemaTercihi tercih)
        {
            ArgumentNullException.ThrowIfNull(tercih);

            string icerik = JsonSerializer.Serialize(tercih);
            bool veritabaninaYazildi = VeritabaninaYaz(icerik);

            if (!veritabaninaYazildi)
                YedegeYaz(icerik);
        }

        /// <summary>Testler için ilgili meta anahtarını veritabanından ve yedek dosyadan temizler.</summary>
        public void Temizle()
        {
            try
            {
                using var baglanti = VeritabaniBaglantisi.Ac(_baglantiMetni);
                using var komut = new NpgsqlCommand("DELETE FROM meta WHERE anahtar = @anahtar;", baglanti);
                komut.Parameters.AddWithValue("@anahtar", _metaAnahtari);
                komut.ExecuteNonQuery();
            }
            catch
            {
                // Veritabanı kapalıysa yalnızca yerel yedeği temizle.
            }

            try
            {
                if (File.Exists(_yedekDosyaYolu))
                    File.Delete(_yedekDosyaYolu);
            }
            catch
            {
            }
        }

        private KullaniciTemaTercihi? VeritabanindanOku()
        {
            try
            {
                VeritabaniBaglantisi.TablolariHazirla(_baglantiMetni);
                using var baglanti = VeritabaniBaglantisi.Ac(_baglantiMetni);
                using var komut = new NpgsqlCommand("SELECT deger FROM meta WHERE anahtar = @anahtar;", baglanti);
                komut.Parameters.AddWithValue("@anahtar", _metaAnahtari);

                object? sonuc = komut.ExecuteScalar();
                if (sonuc is string metin && !string.IsNullOrWhiteSpace(metin))
                    return JsonSerializer.Deserialize<KullaniciTemaTercihi>(metin);
            }
            catch
            {
                // PostgreSQL kapalıysa sessizce yedek dosyaya düşülür.
            }

            return null;
        }

        private bool VeritabaninaYaz(string icerik)
        {
            try
            {
                VeritabaniBaglantisi.TablolariHazirla(_baglantiMetni);
                using var baglanti = VeritabaniBaglantisi.Ac(_baglantiMetni);
                using var islem = baglanti.BeginTransaction();

                const string sql = @"
                    INSERT INTO meta (anahtar, deger)
                    VALUES (@anahtar, @deger)
                    ON CONFLICT (anahtar) DO UPDATE SET deger = EXCLUDED.deger;";

                using var komut = new NpgsqlCommand(sql, baglanti, islem);
                komut.Parameters.AddWithValue("@anahtar", _metaAnahtari);
                komut.Parameters.AddWithValue("@deger", icerik);
                komut.ExecuteNonQuery();

                islem.Commit();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private KullaniciTemaTercihi? YedektenOku()
        {
            try
            {
                if (File.Exists(_yedekDosyaYolu))
                {
                    string metin = File.ReadAllText(_yedekDosyaYolu, Encoding.UTF8);
                    return JsonSerializer.Deserialize<KullaniciTemaTercihi>(metin);
                }
            }
            catch
            {
            }

            return null;
        }

        private void YedegeYaz(string icerik)
        {
            try
            {
                string guvenliYol = VeriYolu.GuvenliCiktiYolu(_yedekDosyaYolu);
                string? klasor = Path.GetDirectoryName(guvenliYol);
                if (!string.IsNullOrEmpty(klasor))
                    Directory.CreateDirectory(klasor);

                File.WriteAllText(guvenliYol, icerik, Encoding.UTF8);
            }
            catch
            {
            }
        }

        // ==================== GÜNLÜK SERİ, XP VE ROZET HESAPLAMA ====================
        public void GunlukDurumuSenkronizeEt(KullaniciTemaTercihi tercih, DateTime? bugunTarih = null)
        {
            DateTime referansGun = (bugunTarih ?? DateTime.Today).Date;
            string bugun = referansGun.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string dun = referansGun.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            if (tercih.SonCalismaGunu != bugun)
            {
                if (tercih.SonCalismaGunu == dun)
                    tercih.GunlukSeri = Math.Max(1, tercih.GunlukSeri + 1);
                else
                    tercih.GunlukSeri = 1;

                tercih.SonCalismaGunu = bugun;
            }

            if (!tercih.HaftalikGecmis.Any(g => g.TarihAnahtari == bugun))
            {
                tercih.HaftalikGecmis.Add(new GunlukPerformans { TarihAnahtari = bugun, CalismaDakikasi = 1, CozulenSoru = 0 });
                tercih.ToplamCalismaDakikasi += 1;
            }

            tercih.HaftalikGecmis = tercih.HaftalikGecmis
                .OrderByDescending(g => g.TarihAnahtari)
                .Take(14)
                .ToList();

            if (tercih.GorevGunu != bugun || tercih.GunlukGorevler.Count == 0)
            {
                tercih.GorevGunu = bugun;
                tercih.GunlukGorevler = new List<GunlukGorev>
                {
                    new() { Kod = "SORU_5", Baslik = "Herhangi bir sınavda 5 soru çöz", Mevcut = 0, Hedef = 5, XpOdulu = 25 },
                    new() { Kod = "SINAV_1", Baslik = "Bir değerlendirme sınavını tamamla", Mevcut = 0, Hedef = 1, XpOdulu = 30 },
                    new() { Kod = "SIMULATOR_1", Baslik = "Uygulamalı simülatörde bir görev bitir", Mevcut = 0, Hedef = 1, XpOdulu = 20 }
                };
            }

            if (tercih.SonAktiviteler.Count == 0)
            {
                tercih.SonAktiviteler.Add(new AktiviteKaydi
                {
                    Tarih = DateTime.Now,
                    Aciklama = "Platform oturumu başlatıldı",
                    KazanilanXp = 0
                });
            }
        }

        public (int ToplamXp, int Seviye, string SeviyeUnvani, int MevcutSeviyeXp, int SonrakiSeviyeGereken, string SonrakiSeviyeUnvani)
            XpVeSeviyeHesapla(KullaniciTemaTercihi tercih, int gorselBasari, int veriTabaniBasari, int siberBasari, bool ortaModGecti, bool simulatorOduluAlindi)
        {
            int ilerlemeXp = (gorselBasari + veriTabaniBasari + siberBasari) * 4
                           + (ortaModGecti ? 150 : 0)
                           + (simulatorOduluAlindi ? 200 : 0);

            int toplamXp = Math.Max(0, ilerlemeXp + tercih.KazanilanEkstraXp);
            const int seviyeAdimi = 250;
            int seviye = (toplamXp / seviyeAdimi) + 1;
            int mevcutSeviyeXp = toplamXp % seviyeAdimi;

            return (
                toplamXp,
                seviye,
                SeviyeUnvaniGetir(seviye),
                mevcutSeviyeXp,
                seviyeAdimi,
                SeviyeUnvaniGetir(seviye + 1)
            );
        }

        private static string SeviyeUnvaniGetir(int seviye) => seviye switch
        {
            1 => "Başlangıç Öğrencisi",
            2 => "Meraklı Kaşif",
            3 => "Azimli Araştırmacı",
            4 => "İleri Uygulayıcı",
            5 => "Alan Uzmanı",
            6 => "Usta Mentor",
            _ => "Efsane Öğrenen"
        };

        public List<RozetBilgisi> RozetleriHesapla(
            KullaniciTemaTercihi tercih,
            int gorselBasari,
            int veriTabaniBasari,
            int siberBasari,
            bool ortaModGecti,
            bool simulatorOduluAlindi)
        {
            int ortalama = (gorselBasari + veriTabaniBasari + siberBasari) / 3;
            int cozulenSoru = Math.Max(tercih.ToplamCozulenSoru, (gorselBasari + veriTabaniBasari + siberBasari) / 5);

            return new List<RozetBilgisi>
            {
                DersRozetiOlustur("GORSEL", "Modül 1 Ustası", "💻", gorselBasari,
                    "Temel Aday", "Gelişen Öğrenci", "İleri Uygulayıcı", "Görsel Programlama Ustası",
                    "Görsel Programlama modülünde %25, %50, %75 ve %90 başarıya ulaş."),

                DersRozetiOlustur("SQL", "Modül 2 Ustası", "🗄️", veriTabaniBasari,
                    "Veri Kaşifi", "Sorgu Uzmanı", "Veri Mimarı", "Veri Tabanı Ustası",
                    "Veri Tabanları modülünde %25, %50, %75 ve %90 başarıya ulaş."),

                DersRozetiOlustur("SIBER", "Modül 3 Ustası", "🛡️", siberBasari,
                    "Güvenlik Gözcüsü", "Risk Analisti", "Savunma Uzmanı", "Siber Güvenlik Lideri",
                    "Siber Güvenlik modülünde %25, %50, %75 ve %90 başarıya ulaş."),

                SimulasyonRozetiOlustur(ortaModGecti, simulatorOduluAlindi, ortalama),

                SayiRozetiOlustur("SORU", "Bilgi Avcısı", "🎯", cozulenSoru, 10, 30, 75, 150,
                    "Soru Çırağı", "Test Çözücü", "Bilgi Avcısı", "Soru Ustası",
                    "Sınavlarda toplam 10, 30, 75 ve 150 soru çözerek kademe atla."),

                SayiRozetiOlustur("SERI", "Seri Ustası", "🔥", tercih.GunlukSeri, 2, 5, 14, 30,
                    "İstikrarlı Öğrenci", "Haftalık Seri", "Seri Ustası", "Durdurulamaz",
                    "Üst üste 2, 5, 14 ve 30 farklı gün uygulamada çalış."),

                SayiRozetiOlustur("SURE", "Zaman Yolcusu", "⏱️", tercih.ToplamCalismaDakikasi, 15, 60, 180, 600,
                    "Odaklanmış", "Çalışkan Öğrenci", "Zaman Ustası", "Maraton Şampiyonu",
                    "Toplam 15 dk, 1 saat, 3 saat ve 10 saat çalışma süresine ulaş."),

                EfsaneRozetiOlustur(gorselBasari, veriTabaniBasari, siberBasari, simulatorOduluAlindi)
            };
        }

        private static RozetBilgisi DersRozetiOlustur(
            string kod, string seriAdi, string ikon, int basari,
            string bronz, string gumus, string altin, string elmas, string nasil)
        {
            if (basari >= 90) return new RozetBilgisi { Kod = kod, SeriAdi = seriAdi, KategoriIkon = ikon, IlerlemeYuzdesi = basari, Kademe = RozetKademesi.Elmas, Unvan = elmas, Aciklama = $"%{basari} başarı", NasilKazanilir = nasil };
            if (basari >= 75) return new RozetBilgisi { Kod = kod, SeriAdi = seriAdi, KategoriIkon = ikon, IlerlemeYuzdesi = basari, Kademe = RozetKademesi.Altin, Unvan = altin, Aciklama = $"%{basari} (Elmas: %90)", NasilKazanilir = nasil };
            if (basari >= 50) return new RozetBilgisi { Kod = kod, SeriAdi = seriAdi, KategoriIkon = ikon, IlerlemeYuzdesi = basari, Kademe = RozetKademesi.Gumus, Unvan = gumus, Aciklama = $"%{basari} (Altın: %75)", NasilKazanilir = nasil };
            if (basari >= 25) return new RozetBilgisi { Kod = kod, SeriAdi = seriAdi, KategoriIkon = ikon, IlerlemeYuzdesi = basari, Kademe = RozetKademesi.Bronz, Unvan = bronz, Aciklama = $"%{basari} (Gümüş: %50)", NasilKazanilir = nasil };
            return new RozetBilgisi { Kod = kod, SeriAdi = seriAdi, KategoriIkon = ikon, IlerlemeYuzdesi = basari, Kademe = RozetKademesi.Kilitli, Unvan = "Kilitli", Aciklama = $"%{basari} (Bronz: %25)", NasilKazanilir = nasil };
        }

        private static RozetBilgisi SimulasyonRozetiOlustur(bool ortaModGecti, bool simulatorOduluAlindi, int ortalama)
        {
            const string nasil = "Orta -> Zor geçiş sınavını geç ve Uygulamalı Simülatör görevini tamamla.";
            if (simulatorOduluAlindi && ortalama >= 85)
                return new RozetBilgisi { Kod = "SIM", SeriAdi = "Hata Avcısı", KategoriIkon = "🐞", IlerlemeYuzdesi = 100, Kademe = RozetKademesi.Elmas, Unvan = "Simülasyon Ustası", Aciklama = "Simülatör + %85 ort.", NasilKazanilir = nasil };
            if (simulatorOduluAlindi)
                return new RozetBilgisi { Kod = "SIM", SeriAdi = "Hata Avcısı", KategoriIkon = "🐞", IlerlemeYuzdesi = 75, Kademe = RozetKademesi.Altin, Unvan = "Hata Avcısı", Aciklama = "Simülatör tamamlandı", NasilKazanilir = nasil };
            if (ortaModGecti)
                return new RozetBilgisi { Kod = "SIM", SeriAdi = "Hata Avcısı", KategoriIkon = "🐞", IlerlemeYuzdesi = 50, Kademe = RozetKademesi.Gumus, Unvan = "Simülatör Adayı", Aciklama = "Zor mod kilidi açık", NasilKazanilir = nasil };
            if (ortalama >= 20)
                return new RozetBilgisi { Kod = "SIM", SeriAdi = "Hata Avcısı", KategoriIkon = "🐞", IlerlemeYuzdesi = 25, Kademe = RozetKademesi.Bronz, Unvan = "Uygulama Çırağı", Aciklama = "Geçiş sınavını çöz", NasilKazanilir = nasil };
            return new RozetBilgisi { Kod = "SIM", SeriAdi = "Hata Avcısı", KategoriIkon = "🐞", IlerlemeYuzdesi = 0, Kademe = RozetKademesi.Kilitli, Unvan = "Kilitli", Aciklama = "Zor mod kilidini aç", NasilKazanilir = nasil };
        }

        private static RozetBilgisi SayiRozetiOlustur(
            string kod, string seriAdi, string ikon, int deger,
            int b, int g, int a, int e,
            string bronz, string gumus, string altin, string elmas, string nasil)
        {
            if (deger >= e) return new RozetBilgisi { Kod = kod, SeriAdi = seriAdi, KategoriIkon = ikon, IlerlemeYuzdesi = 100, Kademe = RozetKademesi.Elmas, Unvan = elmas, Aciklama = $"{deger}/{e} tamamlandı", NasilKazanilir = nasil };
            if (deger >= a) return new RozetBilgisi { Kod = kod, SeriAdi = seriAdi, KategoriIkon = ikon, IlerlemeYuzdesi = deger * 100 / e, Kademe = RozetKademesi.Altin, Unvan = altin, Aciklama = $"{deger}/{e} (Elmas)", NasilKazanilir = nasil };
            if (deger >= g) return new RozetBilgisi { Kod = kod, SeriAdi = seriAdi, KategoriIkon = ikon, IlerlemeYuzdesi = deger * 100 / a, Kademe = RozetKademesi.Gumus, Unvan = gumus, Aciklama = $"{deger}/{a} (Altın)", NasilKazanilir = nasil };
            if (deger >= b) return new RozetBilgisi { Kod = kod, SeriAdi = seriAdi, KategoriIkon = ikon, IlerlemeYuzdesi = deger * 100 / g, Kademe = RozetKademesi.Bronz, Unvan = bronz, Aciklama = $"{deger}/{g} (Gümüş)", NasilKazanilir = nasil };
            return new RozetBilgisi { Kod = kod, SeriAdi = seriAdi, KategoriIkon = ikon, IlerlemeYuzdesi = Math.Min(99, deger * 100 / Math.Max(1, b)), Kademe = RozetKademesi.Kilitli, Unvan = "Kilitli", Aciklama = $"{deger}/{b} (Bronz)", NasilKazanilir = nasil };
        }

        private static RozetBilgisi EfsaneRozetiOlustur(int gorsel, int vt, int siber, bool sim)
        {
            const string nasil = "Tüm modüllerde en az %75 başarıya ulaş ve Simülatör görevini tamamla.";
            int ort = (gorsel + vt + siber) / 3;
            if (gorsel >= 90 && vt >= 90 && siber >= 90 && sim)
                return new RozetBilgisi { Kod = "EFSANE", SeriAdi = "Efsane", KategoriIkon = "🏆", IlerlemeYuzdesi = 100, Kademe = RozetKademesi.Elmas, Unvan = "Platform Efsanesi", Aciklama = "Tüm platform fethedildi!", NasilKazanilir = nasil };
            if (gorsel >= 75 && vt >= 75 && siber >= 75 && sim)
                return new RozetBilgisi { Kod = "EFSANE", SeriAdi = "Efsane", KategoriIkon = "🏆", IlerlemeYuzdesi = 85, Kademe = RozetKademesi.Altin, Unvan = "Tam Donanımlı", Aciklama = "Tüm modüller %75+ ve Simülatör", NasilKazanilir = nasil };
            if (ort >= 50)
                return new RozetBilgisi { Kod = "EFSANE", SeriAdi = "Efsane", KategoriIkon = "🏆", IlerlemeYuzdesi = ort, Kademe = RozetKademesi.Bronz, Unvan = "Yükselen Yıldız", Aciklama = $"Genel ortalama: %{ort}", NasilKazanilir = nasil };
            return new RozetBilgisi { Kod = "EFSANE", SeriAdi = "Efsane", KategoriIkon = "🏆", IlerlemeYuzdesi = ort, Kademe = RozetKademesi.Kilitli, Unvan = "Kilitli", Aciklama = $"Genel ortalama: %{ort}/%50", NasilKazanilir = nasil };
        }

        // ==================== TEMA VE FONT UYGULAMA (GDI+ SIZINTISIZ) ====================
        /// <summary>
        /// Seçili temayı form, sidebar ve kişiselleştirme paneline uygular;
        /// FontOnbellegi kullanarak GDI+ handle sızıntısını önler ve diğer ekranların
        /// durum renklerini (doğru/yanlış, aktif/pasif rozetler) bozmaz.
        /// </summary>
        public void TemayiFormUzerineUygula(
            Form form,
            Control sidebar,
            Control icerikAlani,
            Control kisisellestirmePaneli,
            KullaniciTemaTercihi tercih)
        {
            TemaPaleti tema = TemaBul(tercih.TemaAdi, tercih);
            float fontFarki = tercih.YaziBoyutuModu switch
            {
                "Küçük" => -1.0f,
                "Büyük" => 1.2f,
                _ => 0f
            };

            form.BackColor = tema.ArkaPlan;
            sidebar.BackColor = tema.Sidebar;
            icerikAlani.BackColor = tema.ArkaPlan;
            kisisellestirmePaneli.BackColor = tema.ArkaPlan;

            // Sidebar ve Profil/Tema ekranına tam tema renkleri + önbellekli font uygulanır.
            RenkVeFontUygula(sidebar, tema, tercih.YaziTipi, fontFarki, sidebarMi: true, renkleriUygula: true);
            RenkVeFontUygula(kisisellestirmePaneli, tema, tercih.YaziTipi, fontFarki, sidebarMi: false, renkleriUygula: true);

            // Diğer ana panellerde (Ana Menü, Sınav, Simülatör) yalnızca zemin rengi ve font güncellenir;
            // yeşil/kırmızı/mavi durum rozetleri ve aksiyon buton renkleri korunur.
            foreach (Control ekranPaneli in icerikAlani.Controls)
            {
                if (ekranPaneli == kisisellestirmePaneli)
                    continue;

                if (ekranPaneli is Panel && ekranPaneli.Tag?.ToString() != "SabitRenk")
                    ekranPaneli.BackColor = tema.ArkaPlan;

                RenkVeFontUygula(ekranPaneli, tema, tercih.YaziTipi, fontFarki, sidebarMi: false, renkleriUygula: false);
            }
        }

        private static void RenkVeFontUygula(
            Control kok,
            TemaPaleti tema,
            string fontAdi,
            float fontFarki,
            bool sidebarMi,
            bool renkleriUygula)
        {
            foreach (Control ctrl in kok.Controls)
            {
                string tag = ctrl.Tag?.ToString() ?? string.Empty;

                // Kod bloklarında (Consolas) yazı tipini değiştirme, yalnızca boyut farkı uygula.
                try
                {
                    if (ctrl.Font != null && !ctrl.Font.Name.Equals("Segoe UI Emoji", StringComparison.OrdinalIgnoreCase))
                    {
                        StrongBox<float> tabanKutu = OrijinalFontBoyutlari.GetValue(ctrl, c => new StrongBox<float>(c.Font.Size));
                        float yeniBoyut = Math.Max(7.5f, tabanKutu.Value + fontFarki);
                        string hedefFont = ctrl is RichTextBox && ctrl.Font.Name.Equals("Consolas", StringComparison.OrdinalIgnoreCase)
                            ? "Consolas"
                            : fontAdi;

                        if (!ctrl.Font.Name.Equals(hedefFont, StringComparison.OrdinalIgnoreCase) || Math.Abs(ctrl.Font.Size - yeniBoyut) > 0.05f)
                            ctrl.Font = FontOnbellegi.Al(hedefFont, yeniBoyut, ctrl.Font.Style);
                    }
                }
                catch
                {
                }

                if (renkleriUygula && tag != "SabitRenk")
                {
                    if (ctrl is Button btn)
                    {
                        if (tag == "VurguButon")
                        {
                            btn.BackColor = tema.Vurgu;
                            btn.ForeColor = ParlakMi(tema.Vurgu) ? Color.FromArgb(20, 20, 26) : Color.White;
                        }
                        else if (sidebarMi)
                        {
                            btn.BackColor = tema.Sidebar;
                            btn.ForeColor = tema.Yazi;
                            btn.FlatAppearance.MouseOverBackColor = tema.Kart;
                        }
                        else
                        {
                            btn.BackColor = tema.Kart;
                            btn.ForeColor = tema.Yazi;
                            btn.FlatAppearance.BorderColor = Color.FromArgb(85, tema.Yazi);
                        }
                    }
                    else if (ctrl is ComboBox || ctrl is TextBox)
                    {
                        ctrl.BackColor = tema.Kart;
                        ctrl.ForeColor = tema.Yazi;
                    }
                    else if (ctrl is Label || ctrl is CheckBox)
                    {
                        ctrl.ForeColor = tema.Yazi;
                    }
                    else if (ctrl is Panel pnl && tag != "AnaBolum" && tag != "OnizlemeKarti")
                    {
                        pnl.BackColor = sidebarMi ? tema.Sidebar : tema.Kart;
                    }
                }

                if (ctrl.HasChildren)
                    RenkVeFontUygula(ctrl, tema, fontAdi, fontFarki, sidebarMi, renkleriUygula);
            }
        }

        public static void DuzKartCiz(Graphics g, Rectangle alan, Color kenarRengi, float kalinlik = 1.5f)
        {
            if (alan.Width < 4 || alan.Height < 4)
                return;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var kalem = new Pen(kenarRengi, kalinlik);
            g.DrawRectangle(kalem, 1, 1, alan.Width - 3, alan.Height - 3);
        }

        public static string GoreceliTarihFormatla(DateTime tarih)
        {
            if (tarih.Date == DateTime.Today) return $"Bugün {tarih:HH:mm}";
            if (tarih.Date == DateTime.Today.AddDays(-1)) return $"Dün {tarih:HH:mm}";
            return tarih.ToString("dd MMM HH:mm", new CultureInfo("tr-TR"));
        }
    }
}
