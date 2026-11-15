using System;
using System.Collections.Generic;
using KademeliEgitimPlatformu.Models;
using Npgsql;

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
    /// İlerleme verisini ve Seviye Geçiş Ara Sınavı geçmişini yerel PostgreSQL 18
    /// (kademeli_egitim) veritabanındaki 'ilerleme', 'kullanici_ilerleme' ve
    /// 'seviye_gecis_sinavlari' tablolarında saklar.
    /// </summary>
    public class IlerlemeServisi : IIlerlemeDeposu
    {
        private readonly string? _baglantiMetni;
        private readonly string _anahtarOneki;
        private readonly int? _kullaniciId;

        public IlerlemeServisi(string? ozelBaglantiDizesi = null, string anahtarOneki = "", int? kullaniciId = null)
        {
            _baglantiMetni = ozelBaglantiDizesi;
            _anahtarOneki = string.IsNullOrWhiteSpace(anahtarOneki) ? string.Empty : anahtarOneki.Trim() + ":";
            _kullaniciId = kullaniciId;
            VeritabaniBaglantisi.TablolariHazirla(_baglantiMetni);
        }

        /// <summary>Kayıtlı ilerlemeyi PostgreSQL'den okur. Kayıt yoksa varsayılan değerleri döndürür.</summary>
        public IlerlemeDurumu Yukle()
        {
            var degerler = new Dictionary<string, int>(StringComparer.Ordinal);

            using var baglanti = VeritabaniBaglantisi.Ac(_baglantiMetni);
            using var komut = new NpgsqlCommand(
                "SELECT anahtar, deger FROM ilerleme WHERE anahtar LIKE @onek;",
                baglanti);
            komut.Parameters.AddWithValue("@onek", _anahtarOneki + "%");

            using var okuyucu = komut.ExecuteReader();
            while (okuyucu.Read())
            {
                string tamAnahtar = okuyucu.GetString(0);
                if (!string.IsNullOrEmpty(_anahtarOneki))
                {
                    if (!tamAnahtar.StartsWith(_anahtarOneki, StringComparison.Ordinal))
                        continue;
                    tamAnahtar = tamAnahtar[_anahtarOneki.Length..];
                }
                else if (tamAnahtar.Contains(':', StringComparison.Ordinal))
                {
                    // Test önekli kayıtları varsayılan kullanıcı profiline karıştırma
                    continue;
                }

                degerler[tamAnahtar] = okuyucu.GetInt32(1);
            }

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

        /// <summary>İlerlemeyi tek bir PostgreSQL transaction içinde kaydeder (ya hepsi yazılır ya hiçbiri).</summary>
        public void Kaydet(IlerlemeDurumu d)
        {
            ArgumentNullException.ThrowIfNull(d);

            using var baglanti = VeritabaniBaglantisi.Ac(_baglantiMetni);
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

        /// <summary>
        /// Kolay -> Orta ve Orta -> Zor Seviye Geçiş Ara Sınavı denemelerini
        /// PostgreSQL 'seviye_gecis_sinavlari' tablosuna kaydeder.
        /// </summary>
        public void SeviyeGecisSinaviKaydet(SeviyeGecisSinavKaydi kayit)
        {
            ArgumentNullException.ThrowIfNull(kayit);

            using var baglanti = VeritabaniBaglantisi.Ac(_baglantiMetni);
            using var islem = baglanti.BeginTransaction();

            const string sql = @"
                INSERT INTO seviye_gecis_sinavlari (
                    kullanici_id, gecis_turu, disiplin, hedef_zorluk,
                    toplam_soru, dogru_sayisi, basari_yuzdesi, baraj_yuzdesi,
                    basarili_mi, gecen_sure_saniye, sinav_tarihi
                ) VALUES (
                    @kullanici_id, @gecis_turu, @disiplin, @hedef_zorluk,
                    @toplam_soru, @dogru_sayisi, @basari_yuzdesi, @baraj_yuzdesi,
                    @basarili_mi, @gecen_sure_saniye, @sinav_tarihi
                );";

            using var komut = new NpgsqlCommand(sql, baglanti, islem);
            komut.Parameters.Add(new NpgsqlParameter("@kullanici_id", NpgsqlTypes.NpgsqlDbType.Integer)
            {
                Value = (object?)_kullaniciId ?? DBNull.Value
            });
            komut.Parameters.AddWithValue("@gecis_turu", kayit.GecisTuru);
            komut.Parameters.Add(new NpgsqlParameter("@disiplin", NpgsqlTypes.NpgsqlDbType.Smallint)
            {
                Value = kayit.Disiplin.HasValue ? (short)kayit.Disiplin.Value : DBNull.Value
            });
            komut.Parameters.AddWithValue("@hedef_zorluk", (short)kayit.HedefZorluk);
            komut.Parameters.AddWithValue("@toplam_soru", Math.Max(1, kayit.ToplamSoru));
            komut.Parameters.AddWithValue("@dogru_sayisi", Math.Clamp(kayit.DogruSayisi, 0, Math.Max(1, kayit.ToplamSoru)));
            komut.Parameters.AddWithValue("@basari_yuzdesi", Math.Clamp(kayit.BasariYuzdesi, 0, 100));
            komut.Parameters.AddWithValue("@baraj_yuzdesi", Math.Clamp(kayit.BarajYuzdesi, 0, 100));
            komut.Parameters.AddWithValue("@basarili_mi", kayit.BasariliMi);
            komut.Parameters.AddWithValue("@gecen_sure_saniye", Math.Max(0, kayit.GecenSureSaniye));
            komut.Parameters.AddWithValue("@sinav_tarihi", (kayit.SinavTarihi ?? DateTime.UtcNow).ToUniversalTime());

            komut.ExecuteNonQuery();
            islem.Commit();
        }

        /// <summary>
        /// PostgreSQL 'seviye_gecis_sinavlari' tablosundaki son ara sınav denemelerini döndürür.
        /// </summary>
        public List<SeviyeGecisSinavKaydi> SonSeviyeGecisSinavlari(int adet = 20)
        {
            int sinir = Math.Clamp(adet, 1, 100);
            var liste = new List<SeviyeGecisSinavKaydi>();

            using var baglanti = VeritabaniBaglantisi.Ac(_baglantiMetni);
            const string sql = @"
                SELECT gecis_turu, disiplin, hedef_zorluk, toplam_soru, dogru_sayisi,
                       basari_yuzdesi, baraj_yuzdesi, basarili_mi, gecen_sure_saniye, sinav_tarihi
                FROM seviye_gecis_sinavlari
                WHERE (@kullanici_id IS NULL AND kullanici_id IS NULL OR kullanici_id = @kullanici_id)
                ORDER BY id DESC
                LIMIT @adet;";

            using var komut = new NpgsqlCommand(sql, baglanti);
            komut.Parameters.Add(new NpgsqlParameter("@kullanici_id", NpgsqlTypes.NpgsqlDbType.Integer)
            {
                Value = (object?)_kullaniciId ?? DBNull.Value
            });
            komut.Parameters.AddWithValue("@adet", sinir);

            using var okuyucu = komut.ExecuteReader();
            while (okuyucu.Read())
            {
                liste.Add(new SeviyeGecisSinavKaydi(
                    okuyucu.GetString(0),
                    okuyucu.IsDBNull(1) ? null : (DisiplinTuru)okuyucu.GetInt16(1),
                    (ZorlukSeviyesi)okuyucu.GetInt16(2),
                    okuyucu.GetInt32(3),
                    okuyucu.GetInt32(4),
                    okuyucu.GetInt32(5),
                    okuyucu.GetInt32(6),
                    okuyucu.GetBoolean(7),
                    okuyucu.GetInt32(8),
                    okuyucu.GetDateTime(9).ToLocalTime()));
            }

            return liste;
        }

        /// <summary>İlgili önek altındaki ilerleme kayıtlarını temizler.</summary>
        public void Temizle()
        {
            using var baglanti = VeritabaniBaglantisi.Ac(_baglantiMetni);
            using var komut = new NpgsqlCommand(
                "DELETE FROM ilerleme WHERE anahtar LIKE @onek;",
                baglanti);
            komut.Parameters.AddWithValue("@onek", _anahtarOneki + "%");
            komut.ExecuteNonQuery();
        }

        private void Yaz(NpgsqlConnection baglanti, NpgsqlTransaction islem, string anahtar, int deger)
        {
            const string sql = @"
                INSERT INTO ilerleme (anahtar, deger)
                VALUES (@anahtar, @deger)
                ON CONFLICT (anahtar) DO UPDATE SET deger = EXCLUDED.deger;";

            using var komut = new NpgsqlCommand(sql, baglanti, islem);
            komut.Parameters.AddWithValue("@anahtar", _anahtarOneki + anahtar);
            komut.Parameters.AddWithValue("@deger", deger);
            komut.ExecuteNonQuery();
        }
    }
}
