using System;
using KademeliEgitimPlatformu.Models;
using Npgsql;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>
    /// Kullanıcı kayıt, giriş ve şifre güncelleme işlemlerini PostgreSQL 18 (Npgsql)
    /// ve BCrypt.Net-Next tuzlu (salted) parola özetleme ile yönetir.
    /// SHA256 tamamen kaldırılmıştır; yalnızca BCrypt.HashPassword ve BCrypt.Verify kullanılır.
    /// </summary>
    public class KullaniciServisi
    {
        private const int BcryptMaliyetFaktoru = 11;
        private readonly string? _baglantiMetni;

        public KullaniciServisi(string? ozelBaglantiDizesi = null)
        {
            _baglantiMetni = ozelBaglantiDizesi;
            VeritabaniBaglantisi.TablolariHazirla(_baglantiMetni);
        }

        /// <summary>
        /// Parolayı BCrypt.Net-Next ile rastgele tuz (salt) üreterek özetler.
        /// </summary>
        public static string ParolaOzetle(string parola)
        {
            if (string.IsNullOrWhiteSpace(parola))
                throw new ArgumentException("Parola boş olamaz.", nameof(parola));

            return BCrypt.Net.BCrypt.HashPassword(parola, workFactor: BcryptMaliyetFaktoru);
        }

        /// <summary>
        /// Girilen düz metin parolayı kayıtlı BCrypt özetiyle doğrular.
        /// </summary>
        public static bool ParolaDogrula(string parola, string sifreHash)
        {
            if (string.IsNullOrEmpty(parola) || string.IsNullOrWhiteSpace(sifreHash))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(parola, sifreHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                return false;
            }
        }

        /// <summary>
        /// Yeni kullanıcı kaydı oluşturur. Parola BCrypt.HashPassword ile tuzlanarak saklanır.
        /// </summary>
        public Kullanici KayitOl(string kullaniciAdi, string parola, string adSoyad = "", string rol = "Ogrenci")
        {
            if (string.IsNullOrWhiteSpace(kullaniciAdi))
                throw new ArgumentException("Kullanıcı adı boş olamaz.", nameof(kullaniciAdi));

            string temizKullaniciAdi = kullaniciAdi.Trim();
            string temizAdSoyad = (adSoyad ?? string.Empty).Trim();
            string gecerliRol = string.IsNullOrWhiteSpace(rol) ? "Ogrenci" : rol.Trim();
            string sifreHash = ParolaOzetle(parola);

            using var baglanti = VeritabaniBaglantisi.Ac(_baglantiMetni);
            using var islem = baglanti.BeginTransaction();

            const string sql = @"
                INSERT INTO kullanicilar (kullanici_adi, ad_soyad, sifre_hash, rol, aktif_mi)
                VALUES (@kullanici_adi, @ad_soyad, @sifre_hash, @rol, TRUE)
                RETURNING id, olusturma_tarihi;";

            using var komut = new NpgsqlCommand(sql, baglanti, islem);
            komut.Parameters.AddWithValue("@kullanici_adi", temizKullaniciAdi);
            komut.Parameters.AddWithValue("@ad_soyad", temizAdSoyad);
            komut.Parameters.AddWithValue("@sifre_hash", sifreHash);
            komut.Parameters.AddWithValue("@rol", gecerliRol);

            using var okuyucu = komut.ExecuteReader();
            okuyucu.Read();
            int id = okuyucu.GetInt32(0);
            DateTime olusturma = okuyucu.GetDateTime(1);
            okuyucu.Close();

            islem.Commit();

            return new Kullanici
            {
                Id = id,
                KullaniciAdi = temizKullaniciAdi,
                AdSoyad = temizAdSoyad,
                SifreHash = sifreHash,
                Rol = gecerliRol,
                AktifMi = true,
                OlusturmaTarihi = olusturma
            };
        }

        /// <summary>
        /// Kullanıcı girişini BCrypt.Verify ile doğrular. Başarılıysa son giriş tarihini günceller
        /// ve kullanıcı nesnesini döndürür; hatalıysa null döner.
        /// </summary>
        public Kullanici? GirisYap(string kullaniciAdi, string parola)
        {
            if (string.IsNullOrWhiteSpace(kullaniciAdi) || string.IsNullOrEmpty(parola))
                return null;

            string temizKullaniciAdi = kullaniciAdi.Trim();

            using var baglanti = VeritabaniBaglantisi.Ac(_baglantiMetni);

            const string secimSql = @"
                SELECT id, kullanici_adi, ad_soyad, sifre_hash, rol, aktif_mi, olusturma_tarihi, son_giris_tarihi
                FROM kullanicilar
                WHERE kullanici_adi = @kullanici_adi AND aktif_mi = TRUE;";

            Kullanici? kullanici = null;
            using (var komut = new NpgsqlCommand(secimSql, baglanti))
            {
                komut.Parameters.AddWithValue("@kullanici_adi", temizKullaniciAdi);
                using var okuyucu = komut.ExecuteReader();
                if (okuyucu.Read())
                {
                    kullanici = new Kullanici
                    {
                        Id = okuyucu.GetInt32(0),
                        KullaniciAdi = okuyucu.GetString(1),
                        AdSoyad = okuyucu.GetString(2),
                        SifreHash = okuyucu.GetString(3),
                        Rol = okuyucu.GetString(4),
                        AktifMi = okuyucu.GetBoolean(5),
                        OlusturmaTarihi = okuyucu.GetDateTime(6),
                        SonGirisTarihi = okuyucu.IsDBNull(7) ? null : okuyucu.GetDateTime(7)
                    };
                }
            }

            if (kullanici == null || !ParolaDogrula(parola, kullanici.SifreHash))
                return null;

            const string guncelleSql = @"
                UPDATE kullanicilar
                SET son_giris_tarihi = CURRENT_TIMESTAMP
                WHERE id = @id
                RETURNING son_giris_tarihi;";

            using (var guncelle = new NpgsqlCommand(guncelleSql, baglanti))
            {
                guncelle.Parameters.AddWithValue("@id", kullanici.Id);
                if (guncelle.ExecuteScalar() is DateTime sonGiris)
                    kullanici.SonGirisTarihi = sonGiris;
            }

            return kullanici;
        }

        /// <summary>
        /// Mevcut parolayı BCrypt.Verify ile doğruladıktan sonra yeni parolayı
        /// BCrypt.HashPassword ile tuzlayarak günceller.
        /// </summary>
        public bool SifreGuncelle(string kullaniciAdi, string mevcutParola, string yeniParola)
        {
            if (string.IsNullOrWhiteSpace(kullaniciAdi) ||
                string.IsNullOrEmpty(mevcutParola) ||
                string.IsNullOrWhiteSpace(yeniParola))
            {
                return false;
            }

            Kullanici? dogrulanan = GirisYap(kullaniciAdi, mevcutParola);
            if (dogrulanan == null)
                return false;

            string yeniHash = ParolaOzetle(yeniParola);

            using var baglanti = VeritabaniBaglantisi.Ac(_baglantiMetni);
            using var islem = baglanti.BeginTransaction();

            const string sql = @"
                UPDATE kullanicilar
                SET sifre_hash = @sifre_hash
                WHERE id = @id;";

            using var komut = new NpgsqlCommand(sql, baglanti, islem);
            komut.Parameters.AddWithValue("@sifre_hash", yeniHash);
            komut.Parameters.AddWithValue("@id", dogrulanan.Id);

            int etkilenen = komut.ExecuteNonQuery();
            islem.Commit();
            return etkilenen > 0;
        }
    }
}
