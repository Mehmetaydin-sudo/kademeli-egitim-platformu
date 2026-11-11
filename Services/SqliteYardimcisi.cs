using Microsoft.Data.Sqlite;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>
    /// SQLite bağlantılarını ortak ayarlarla açar.
    /// Düşük donanımlı makinelerde kilitlenmeleri ve gereksiz disk yükünü azaltmak için
    /// busy_timeout ve WAL günlükleme kullanılır.
    /// </summary>
    internal static class SqliteYardimcisi
    {
        /// <summary>Bağlantıyı açar ve kısa süreli kilitlenmelerde beklemeyi etkinleştirir.</summary>
        public static SqliteConnection Ac(string baglantiMetni)
        {
            var baglanti = new SqliteConnection(baglantiMetni);
            baglanti.Open();

            using var komut = baglanti.CreateCommand();
            komut.CommandText = "PRAGMA busy_timeout = 3000; PRAGMA synchronous = NORMAL;";
            komut.ExecuteNonQuery();

            return baglanti;
        }

        /// <summary>
        /// Write-Ahead Logging modunu etkinleştirir. Ayar veritabanı dosyasında kalıcıdır;
        /// her açılışta çağrılması zararsızdır.
        /// </summary>
        public static void WalEtkinlestir(SqliteConnection baglanti)
        {
            using var komut = baglanti.CreateCommand();
            komut.CommandText = "PRAGMA journal_mode = WAL;";
            komut.ExecuteNonQuery();
        }
    }
}
