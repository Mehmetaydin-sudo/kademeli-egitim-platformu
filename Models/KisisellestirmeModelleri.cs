using System;
using System.Collections.Generic;
using System.Drawing;

namespace KademeliEgitimPlatformu.Models
{
    public enum RozetKademesi
    {
        Kilitli = 0,
        Bronz = 1,
        Gumus = 2,
        Altin = 3,
        Elmas = 4
    }

    public class RozetBilgisi
    {
        public string Kod { get; set; } = "";
        public string SeriAdi { get; set; } = "";
        public string Unvan { get; set; } = "";
        public string Aciklama { get; set; } = "";
        public string NasilKazanilir { get; set; } = "";
        public string KategoriIkon { get; set; } = "🏆";
        public int IlerlemeYuzdesi { get; set; }
        public RozetKademesi Kademe { get; set; } = RozetKademesi.Kilitli;

        public bool KazanildiMi => Kademe != RozetKademesi.Kilitli;

        public string KademeAdi => Kademe switch
        {
            RozetKademesi.Bronz => "BRONZ",
            RozetKademesi.Gumus => "GÜMÜŞ",
            RozetKademesi.Altin => "ALTIN",
            RozetKademesi.Elmas => "ELMAS",
            _ => "KİLİTLİ"
        };

        public string Ikon => Kademe switch
        {
            RozetKademesi.Bronz => "🥉",
            RozetKademesi.Gumus => "🥈",
            RozetKademesi.Altin => "🥇",
            RozetKademesi.Elmas => "💎",
            _ => "🔒"
        };

        public Color KademeRengi => Kademe switch
        {
            RozetKademesi.Bronz => Color.FromArgb(205, 127, 50),
            RozetKademesi.Gumus => Color.FromArgb(176, 184, 196),
            RozetKademesi.Altin => Color.FromArgb(245, 185, 20),
            RozetKademesi.Elmas => Color.FromArgb(0, 195, 245),
            _ => Color.FromArgb(130, 135, 145)
        };
    }

    public class GunlukGorev
    {
        public string Kod { get; set; } = "";
        public string Baslik { get; set; } = "";
        public int Mevcut { get; set; }
        public int Hedef { get; set; }
        public int XpOdulu { get; set; }
        public bool OdulVerildi { get; set; }

        public bool TamamlandiMi => Mevcut >= Hedef;
    }

    public class AktiviteKaydi
    {
        public DateTime Tarih { get; set; } = DateTime.Now;
        public string Aciklama { get; set; } = "";
        public int KazanilanXp { get; set; }
    }

    public class GunlukPerformans
    {
        public string TarihAnahtari { get; set; } = ""; // yyyy-MM-dd
        public int CalismaDakikasi { get; set; }
        public int CozulenSoru { get; set; }
    }

    public class TemaPaleti
    {
        public string Ad { get; set; } = "";
        public Color ArkaPlan { get; set; }
        public Color Sidebar { get; set; }
        public Color Kart { get; set; }
        public Color Vurgu { get; set; }
        public Color Yazi { get; set; }
        public bool OzelMi { get; set; }

        public TemaPaleti() { }

        public TemaPaleti(string ad, Color arkaPlan, Color sidebar, Color kart, Color vurgu, Color yazi, bool ozelMi = false)
        {
            Ad = ad;
            ArkaPlan = arkaPlan;
            Sidebar = sidebar;
            Kart = kart;
            Vurgu = vurgu;
            Yazi = yazi;
            OzelMi = ozelMi;
        }
    }

    public class OzelRenkPaleti
    {
        public string Ad { get; set; } = "";
        public string ArkaPlanHex { get; set; } = "#1E1E1E";
        public string SidebarHex { get; set; } = "#252526";
        public string KartHex { get; set; } = "#2D2D30";
        public string VurguHex { get; set; } = "#007ACC";
        public string YaziHex { get; set; } = "#F1F1F1";
    }

    public class KullaniciTemaTercihi
    {
        // Görünüm Tercihleri
        public string TemaAdi { get; set; } = "Koyu Klasik";
        public string? VurguHex { get; set; } = null;
        public string YaziTipi { get; set; } = "Segoe UI";
        public string YaziBoyutuModu { get; set; } = "Normal"; // Küçük, Normal, Büyük
        public bool CerceveGoster { get; set; } = true;
        public List<OzelRenkPaleti> OzelPaletler { get; set; } = new();

        // Öğrenci Kimliği & Hedef
        public string OgrenciAdi { get; set; } = "Öğrenci";
        public string Avatar { get; set; } = "🎓";
        public string KisiselHedef { get; set; } = "Kademeli Eğitim ile Uzmanlığa Yolculuk";
        public int HedefSoruSayisi { get; set; } = 100;
        public string? SeciliUnvan { get; set; } = null;

        // Gerçek Oyunlaştırma & İlerleme Verileri
        public int KazanilanEkstraXp { get; set; } = 0;
        public int ToplamCozulenSoru { get; set; } = 0;
        public int ToplamCalismaDakikasi { get; set; } = 0;
        public int GunlukSeri { get; set; } = 0;
        public string SonCalismaGunu { get; set; } = ""; // yyyy-MM-dd
        public string GorevGunu { get; set; } = ""; // yyyy-MM-dd

        public List<GunlukGorev> GunlukGorevler { get; set; } = new();
        public List<AktiviteKaydi> SonAktiviteler { get; set; } = new();
        public List<GunlukPerformans> HaftalikGecmis { get; set; } = new();
        public HashSet<string> OdullendirilenOlaylar { get; set; } = new();
        public HashSet<string> BildirilenRozetler { get; set; } = new();
    }
}