using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>Kullanıcının kalıcı olarak saklanacak ilerleme bilgileri.</summary>
    /// <remarks>
    /// Varsayılan değerler sıfırdır: yeni kullanıcı, hiç soru çözmeden
    /// kazanmadığı bir başarı yüzdesiyle başlamaz.
    /// </remarks>
    public record IlerlemeDurumu(
        int GorselBasari = 0,
        int VeriTabaniBasari = 0,
        int SiberBasari = 0,
        int IpucuJetonu = 3,
        bool HataAvcisiKazanildi = false,
        bool GorselAraSinavGecti = false,
        bool VeriTabaniAraSinavGecti = false,
        bool SiberAraSinavGecti = false,
        bool OrtaModGecti = false);

    /// <summary>
    /// İlerleme verisini yerel SQLite dosyasında (Data/ilerleme.db) saklar.
    /// Tablo: Ilerleme(Anahtar TEXT PRIMARY KEY, Deger INTEGER)
    /// </summary>
    public class IlerlemeServisi : IIlerlemeDeposu
    {
        private readonly string _baglantiMetni;

        public IlerlemeServisi(string? dosyaYolu = null)
        {
            dosyaYolu ??= VeriYolu.IlerlemeVeritabaniYolu;
            Directory.CreateDirectory(Path.GetDirectoryName(dosyaYolu)!);

            _baglantiMetni = new SqliteConnectionStringBuilder { DataSource = dosyaYolu }.ToString();
            TabloyuHazirla();
        }

        private void TabloyuHazirla()
        {
            using var baglanti = SqliteYardimcisi.Ac(_baglantiMetni);
            SqliteYardimcisi.WalEtkinlestir(baglanti);

            using var komut = baglanti.CreateCommand();
            komut.CommandText =
                "CREATE TABLE IF NOT EXISTS Ilerleme (" +
                "  Anahtar TEXT PRIMARY KEY," +
                "  Deger   INTEGER NOT NULL);";
            komut.ExecuteNonQuery();
        }

        /// <summary>Kayıtlı ilerlemeyi okur. Kayıt yoksa varsayılan değerleri döndürür.</summary>
        public IlerlemeDurumu Yukle()
        {
            var degerler = new Dictionary<string, int>();

            using var baglanti = SqliteYardimcisi.Ac(_baglantiMetni);

            using var komut = baglanti.CreateCommand();
            komut.CommandText = "SELECT Anahtar, Deger FROM Ilerleme;";
            using var okuyucu = komut.ExecuteReader();
            while (okuyucu.Read())
                degerler[okuyucu.GetString(0)] = okuyucu.GetInt32(1);

            var v = new IlerlemeDurumu();
            return new IlerlemeDurumu(
                Math.Clamp(degerler.GetValueOrDefault("gorsel", v.GorselBasari), 0, 100),
                Math.Clamp(degerler.GetValueOrDefault("veritabani", v.VeriTabaniBasari), 0, 100),
                Math.Clamp(degerler.GetValueOrDefault("siber", v.SiberBasari), 0, 100),
                Math.Max(0, degerler.GetValueOrDefault("jeton", v.IpucuJetonu)),
                degerler.GetValueOrDefault("hataAvcisi", 0) == 1,
                degerler.GetValueOrDefault("araSinavGorsel", 0) == 1,
                degerler.GetValueOrDefault("araSinavVeri", 0) == 1,
                degerler.GetValueOrDefault("araSinavSiber", 0) == 1,
                degerler.GetValueOrDefault("ortaMod", 0) == 1);
        }

        /// <summary>İlerlemeyi tek bir transaction içinde kaydeder (ya hepsi yazılır ya hiçbiri).</summary>
        public void Kaydet(IlerlemeDurumu d)
        {
            using var baglanti = SqliteYardimcisi.Ac(_baglantiMetni);
            using var islem = baglanti.BeginTransaction();

            Yaz(baglanti, islem, "gorsel", d.GorselBasari);
            Yaz(baglanti, islem, "veritabani", d.VeriTabaniBasari);
            Yaz(baglanti, islem, "siber", d.SiberBasari);
            Yaz(baglanti, islem, "jeton", d.IpucuJetonu);
            Yaz(baglanti, islem, "hataAvcisi", d.HataAvcisiKazanildi ? 1 : 0);
            Yaz(baglanti, islem, "araSinavGorsel", d.GorselAraSinavGecti ? 1 : 0);
            Yaz(baglanti, islem, "araSinavVeri", d.VeriTabaniAraSinavGecti ? 1 : 0);
            Yaz(baglanti, islem, "araSinavSiber", d.SiberAraSinavGecti ? 1 : 0);
            Yaz(baglanti, islem, "ortaMod", d.OrtaModGecti ? 1 : 0);

            islem.Commit();
        }

        // Parametreli sorgu: değerler SQL metnine eklenmez, SQL injection'a karşı güvenlidir.
        private static void Yaz(SqliteConnection baglanti, SqliteTransaction islem, string anahtar, int deger)
        {
            using var komut = baglanti.CreateCommand();
            komut.Transaction = islem;
            komut.CommandText =
                "INSERT INTO Ilerleme (Anahtar, Deger) VALUES ($anahtar, $deger) " +
                "ON CONFLICT(Anahtar) DO UPDATE SET Deger = excluded.Deger;";
            komut.Parameters.AddWithValue("$anahtar", anahtar);
            komut.Parameters.AddWithValue("$deger", deger);
            komut.ExecuteNonQuery();
        }
    }
}
