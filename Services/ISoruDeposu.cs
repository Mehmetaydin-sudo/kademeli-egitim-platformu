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

        /// <summary>Doğrulanmış soru listesini tek bir transaction içinde veritabanına aktarır.</summary>
        int SorulariVeritabaninaAktar(IEnumerable<Soru> yeniSorular, bool mevcutlariGuncelle = false);

        /// <summary>Mevcut bir soruyu kimliğine (Id) göre günceller.</summary>
        bool SoruGuncelle(Soru soru);

        /// <summary>Belirtilen kimlikteki soruyu veritabanından siler.</summary>
        bool SoruSil(int soruId);

        /// <summary>Tüm soru havuzunu .xlsx veya .csv olarak dışa aktarır.</summary>
        int DisariAktar(string ciktiDosyaYolu);
    }
}
