namespace KademeliEgitimPlatformu.Models
{
    /// <summary>
    /// Sınavın türü: normal modül/karma sınavı, modül ara sınavı veya
    /// kademeler arası (Kolay -> Orta, Orta -> Zor) Seviye Geçiş Ara Sınavı.
    /// </summary>
    public enum SinavTuru
    {
        Normal = 0,
        AraSinav = 1,
        KolayOrtaGecisSinavi = 2,
        OrtaZorGecisSinavi = 3
    }
}
