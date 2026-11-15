using System.Collections.Generic;
using System.Linq;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>
    /// Kademeli geçiş kurallarını arayüzden bağımsız, test edilebilir biçimde uygular.
    ///
    /// Kurallar:
    ///   • Her modülün bir ara sınavı vardır; ara sınav barajı %70'tir.
    ///   • Orta Mod, üç modülün ara sınavı da geçildiğinde açılır.
    ///   • Zor Mod, Orta Mod karma sınavı %75 ile geçildiğinde açılır.
    /// </summary>
    public static class KademeServisi
    {
        public const int AraSinavBaraji = 70;
        public const int OrtaModBaraji = 75;

        /// <summary>Ara sınav sonucunun geçme barajını karşılayıp karşılamadığını söyler.</summary>
        public static bool AraSinavGectiMi(int dogru, int toplam) =>
            PuanlamaServisi.Yuzde(dogru, toplam) >= AraSinavBaraji;

        /// <summary>Orta Mod karma sınavının geçme barajını karşılayıp karşılamadığını söyler.</summary>
        public static bool OrtaModGectiMi(int dogru, int toplam) =>
            PuanlamaServisi.Yuzde(dogru, toplam) >= OrtaModBaraji;

        public static int GecilenAraSinavSayisi(IlerlemeDurumu durum)
        {
            int sayi = 0;
            if (durum.GorselAraSinavGecti) sayi++;
            if (durum.VeriTabaniAraSinavGecti) sayi++;
            if (durum.SiberAraSinavGecti) sayi++;
            return sayi;
        }

        /// <summary>Orta Mod, üç modülün ara sınavının tamamı geçilince açılır.</summary>
        public static bool OrtaModAcikMi(IlerlemeDurumu durum) =>
            GecilenAraSinavSayisi(durum) == 3;

        /// <summary>Zor Mod, Orta Mod sınavı geçildikten sonra açılır.</summary>
        public static bool ZorModAcikMi(IlerlemeDurumu durum) =>
            durum.OrtaModGecti && OrtaModAcikMi(durum);

        /// <summary>Eksik ara sınavların kullanıcıya gösterilecek adlarını döndürür.</summary>
        public static IReadOnlyList<string> EksikAraSinavlar(IlerlemeDurumu durum)
        {
            var eksikler = new List<string>();
            if (!durum.GorselAraSinavGecti) eksikler.Add("Görsel Programlama");
            if (!durum.VeriTabaniAraSinavGecti) eksikler.Add("Veri Tabanları");
            if (!durum.SiberAraSinavGecti) eksikler.Add("Siber Güvenlik");
            return eksikler;
        }

        public static string EksikAraSinavMetni(IlerlemeDurumu durum)
        {
            IReadOnlyList<string> eksikler = EksikAraSinavlar(durum);
            return eksikler.Count == 0
                ? "Tüm ara sınavlar tamamlandı."
                : "Eksik ara sınavlar: " + string.Join(", ", eksikler);
        }
    }
}
