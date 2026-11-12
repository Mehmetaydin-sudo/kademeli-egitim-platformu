using System.Collections.Generic;
using KademeliEgitimPlatformu.Models;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>
    /// Soru havuzunun veri erişim sözleşmesi. Bugün SQLite kullanılıyor;
    /// ileride merkezi bir sunucuya (ör. PostgreSQL) geçilirse yalnızca bu
    /// arayüzün yeni uygulaması yazılır, arayüz kodu değişmez.
    /// </summary>
    public interface ISoruDeposu
    {
        int SonPaketAtlananSayisi { get; }

        int SoruSayisi();

        List<Soru> TumSorulariGetir(ZorlukSeviyesi? zorluk = null);

        List<Soru> SorulariFiltrele(DisiplinTuru disiplin, ZorlukSeviyesi? zorluk = null);

        /// <summary>Belirtilen modülün ara sınav sorularını getirir.</summary>
        List<Soru> AraSinavSorulariniGetir(DisiplinTuru disiplin);

        int DisaridanPaketYukle(string secilenDosyaYolu);
    }
}
