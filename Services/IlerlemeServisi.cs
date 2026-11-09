using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>Kullanıcının kalıcı olarak saklanacak ilerleme bilgileri.</summary>
    public record IlerlemeDurumu(
        int GorselBasari = 65,
        int VeriTabaniBasari = 40,
        int SiberBasari = 10,
        int IpucuJetonu = 3,
        bool HataAvcisiKazanildi = false);

    /// <summary>
    /// İlerleme verisini yerel SQLite dosyasında (Data/ilerleme.db) saklar.
    /// Tablo: Ilerleme(Anahtar TEXT PRIMARY KEY, Deger INTEGER)
    /// </summary>
    public class IlerlemeServisi
    {
        private readonly string _baglantiMetni;

        public IlerlemeServisi(string? dosyaYolu = null)
        {
            dosyaYolu ??= Path.Combine(AppContext.BaseDirectory, "Data", "ilerleme.db");
            Directory.CreateDirectory(Path.GetDirectoryName(dosyaYolu)!);

            _baglantiMetni = new SqliteConnectionStringBuilder { DataSource = dosyaYolu }.ToString();
            TabloyuHazirla();
        }

        private void TabloyuHazirla()
        {
            using var baglanti = new SqliteConnection(_baglantiMetni);
            baglanti.Open();

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

            using var baglanti = new SqliteConnection(_baglantiMetni);
            baglanti.Open();

            using var komut = baglanti.CreateCommand();
            komut.CommandText = "SELECT Anahtar, Deger FROM Ilerleme;";
            using var okuyucu = komut.ExecuteReader();
            while (okuyucu.Read())
                degerler[okuyucu.GetString(0)] = okuyucu.GetInt32(1);

            var v = new IlerlemeDurumu();
            return new IlerlemeDurumu(
                degerler.GetValueOrDefault("gorsel", v.GorselBasari),
                degerler.GetValueOrDefault("veritabani", v.VeriTabaniBasari),
                degerler.GetValueOrDefault("siber", v.SiberBasari),
                degerler.GetValueOrDefault("jeton", v.IpucuJetonu),
                degerler.GetValueOrDefault("hataAvcisi", 0) == 1);
        }

        /// <summary>İlerlemeyi tek bir transaction içinde kaydeder (ya hepsi yazılır ya hiçbiri).</summary>
        public void Kaydet(IlerlemeDurumu d)
        {
            using var baglanti = new SqliteConnection(_baglantiMetni);
            baglanti.Open();
            using var islem = baglanti.BeginTransaction();

            Yaz(baglanti, islem, "gorsel", d.GorselBasari);
            Yaz(baglanti, islem, "veritabani", d.VeriTabaniBasari);
            Yaz(baglanti, islem, "siber", d.SiberBasari);
            Yaz(baglanti, islem, "jeton", d.IpucuJetonu);
            Yaz(baglanti, islem, "hataAvcisi", d.HataAvcisiKazanildi ? 1 : 0);

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