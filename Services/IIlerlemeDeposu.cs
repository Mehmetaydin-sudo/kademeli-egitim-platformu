using System;
using System.Collections.Generic;
using KademeliEgitimPlatformu.Models;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>
    /// PostgreSQL 'seviye_gecis_sinavlari' tablosuna kaydedilen ara sınav / geçiş sınavı sonucu.
    /// </summary>
    public sealed record SeviyeGecisSinavKaydi(
        string GecisTuru,
        DisiplinTuru? Disiplin,
        ZorlukSeviyesi HedefZorluk,
        int ToplamSoru,
        int DogruSayisi,
        int BasariYuzdesi,
        int BarajYuzdesi,
        bool BasariliMi,
        int GecenSureSaniye,
        DateTime? SinavTarihi = null);

    /// <summary>
    /// Kullanıcı ilerlemesinin ve seviye geçiş ara sınavlarının veri erişim sözleşmesi (PostgreSQL 18 / Npgsql).
    /// </summary>
    public interface IIlerlemeDeposu
    {
        IlerlemeDurumu Yukle();

        void Kaydet(IlerlemeDurumu durum);

        void SeviyeGecisSinaviKaydet(SeviyeGecisSinavKaydi kayit);

        List<SeviyeGecisSinavKaydi> SonSeviyeGecisSinavlari(int adet = 20);
    }
}
