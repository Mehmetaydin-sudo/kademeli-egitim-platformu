using System.Collections.Generic;
using KademeliEgitimPlatformu.Models;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>
    /// Soru havuzunun veri erişim sözleşmesi (PostgreSQL 18 / Npgsql).
    /// </summary>
    public interface ISoruDeposu
    {
        int SonPaketAtlananSayisi { get; }

        int SoruSayisi();

        List<Soru> TumSorulariGetir(ZorlukSeviyesi? zorluk = null);

        List<Soru> SorulariFiltrele(DisiplinTuru disiplin, ZorlukSeviyesi? zorluk = null);

        /// <summary>Belirtilen modülün ara sınav sorularını getirir.</summary>
        List<Soru> AraSinavSorulariniGetir(DisiplinTuru disiplin);

        /// <summary>Kolay -> Orta veya Orta -> Zor seviye geçiş ara sınavı sorularını getirir.</summary>
        List<Soru> SeviyeGecisSorulariniGetir(ZorlukSeviyesi hedefZorluk);

        int DisaridanPaketYukle(string secilenDosyaYolu);
    }
}
