using System;

namespace KademeliEgitimPlatformu.Models
{
    /// <summary>Sınavda yanlış cevaplanan bir sorunun kalıcı kaydı.</summary>
    public sealed record YanlisKaydi(
        DateTime Tarih,
        DisiplinTuru Disiplin,
        ZorlukSeviyesi Zorluk,
        string Baslik,
        string SecilenCevap,
        string DogruCevap,
        string Aciklama);
}
