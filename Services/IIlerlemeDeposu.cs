namespace KademeliEgitimPlatformu.Services
{
    /// <summary>
    /// Kullanıcı ilerlemesinin veri erişim sözleşmesi. SQLite'a bağımlılığı
    /// azaltır; merkezi sunucu hedeflenirse yeni bir uygulama yeterli olur.
    /// </summary>
    public interface IIlerlemeDeposu
    {
        IlerlemeDurumu Yukle();

        void Kaydet(IlerlemeDurumu durum);
    }
}
