using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using KademeliEgitimPlatformu.Models;
using KademeliEgitimPlatformu.Services;
using Microsoft.Data.Sqlite;
using Xunit;

namespace KademeliEgitimPlatformu.Tests
{
    public class SoruServisiTests
    {
        private static Soru GecerliSoru() => new()
        {
            Baslik = "Geçerli soru",
            Disiplin = DisiplinTuru.GorselProgramlama,
            Zorluk = ZorlukSeviyesi.Kolay,
            Secenekler = new List<string> { "a", "b", "c", "d" },
            DogruSecenekIndex = 1,
            YanlisCevapAciklamalari = new Dictionary<int, string> { [0] = "a yanlış" }
        };

        [Fact]
        public void GecerliSoru_KabulEdilir()
        {
            Assert.True(SoruServisi.SoruGecerliMi(GecerliSoru()));
        }

        [Fact]
        public void NullSoru_Reddedilir()
        {
            Assert.False(SoruServisi.SoruGecerliMi(null));
        }

        [Fact]
        public void BosBaslik_Reddedilir()
        {
            Soru soru = GecerliSoru();
            soru.Baslik = "   ";

            Assert.False(SoruServisi.SoruGecerliMi(soru));
        }

        [Fact]
        public void DorttenAzSecenek_Reddedilir()
        {
            Soru soru = GecerliSoru();
            soru.Secenekler = new List<string> { "a", "b", "c" };

            Assert.False(SoruServisi.SoruGecerliMi(soru));
        }

        [Fact]
        public void BosSecenek_Reddedilir()
        {
            Soru soru = GecerliSoru();
            soru.Secenekler[2] = " ";

            Assert.False(SoruServisi.SoruGecerliMi(soru));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        public void GecersizDogruIndex_Reddedilir(int index)
        {
            Soru soru = GecerliSoru();
            soru.DogruSecenekIndex = index;

            Assert.False(SoruServisi.SoruGecerliMi(soru));
        }

        [Fact]
        public void TanimsizEnumDegeri_Reddedilir()
        {
            Soru soru = GecerliSoru();
            soru.Disiplin = (DisiplinTuru)9;

            Assert.False(SoruServisi.SoruGecerliMi(soru));
        }

        [Fact]
        public void GecersizAciklamaAnahtari_Reddedilir()
        {
            Soru soru = GecerliSoru();
            soru.YanlisCevapAciklamalari[9] = "taşan anahtar";

            Assert.False(SoruServisi.SoruGecerliMi(soru));
        }

        [Fact]
        public void NullAciklamalar_KabulEdilir()
        {
            Soru soru = GecerliSoru();
            soru.YanlisCevapAciklamalari = null!;

            Assert.True(SoruServisi.SoruGecerliMi(soru));
        }

        [Fact]
        public void SoruSayisi_JsonIleSenkronizeEdilenSorulariSayar()
        {
            string klasor = GeciciKlasor();
            try
            {
                string jsonYolu = Path.Combine(klasor, "Sorular.json");
                string dbYolu = Path.Combine(klasor, "sorular.db");
                File.WriteAllText(jsonYolu, JsonSerializer.Serialize(new[] { GecerliSoru() }));

                var servis = new SoruServisi(jsonYolu, dbYolu);

                Assert.Equal(1, servis.SoruSayisi());
                Assert.Single(servis.TumSorulariGetir());
            }
            finally
            {
                GeciciKlasoruSil(klasor);
            }
        }

        [Fact]
        public void AyniJsonIkinciAcilista_TekrarEklenmez()
        {
            string klasor = GeciciKlasor();
            try
            {
                string jsonYolu = Path.Combine(klasor, "Sorular.json");
                string dbYolu = Path.Combine(klasor, "sorular.db");
                File.WriteAllText(jsonYolu, JsonSerializer.Serialize(new[] { GecerliSoru() }));

                var ilk = new SoruServisi(jsonYolu, dbYolu);
                var ikinci = new SoruServisi(jsonYolu, dbYolu);

                Assert.Equal(1, ilk.SoruSayisi());
                Assert.Equal(1, ikinci.SoruSayisi());
            }
            finally
            {
                GeciciKlasoruSil(klasor);
            }
        }

        [Fact]
        public void AraSinavSorulari_SadeceEtiketliOlanlariGetirir()
        {
            string klasor = GeciciKlasor();
            try
            {
                string jsonYolu = Path.Combine(klasor, "Sorular.json");
                string dbYolu = Path.Combine(klasor, "sorular.db");

                Soru normal = GecerliSoru();
                normal.Baslik = "Normal modül sorusu";

                Soru araSinav = GecerliSoru();
                araSinav.Baslik = "Ara sınav sorusu";
                araSinav.Etiket = "AraSinav";

                File.WriteAllText(jsonYolu, JsonSerializer.Serialize(new[] { normal, araSinav }));

                var servis = new SoruServisi(jsonYolu, dbYolu);
                var sonuc = servis.AraSinavSorulariniGetir(DisiplinTuru.GorselProgramlama);

                Assert.Single(sonuc);
                Assert.Equal("Ara sınav sorusu", sonuc[0].Baslik);
                Assert.Equal(2, servis.SoruSayisi());
            }
            finally
            {
                GeciciKlasoruSil(klasor);
            }
        }

        [Fact]
        public void Veritabani_DuzMetinSoruIcermez()
        {
            string klasor = GeciciKlasor();
            try
            {
                string jsonYolu = Path.Combine(klasor, "Sorular.json");
                string dbYolu = Path.Combine(klasor, "sorular.db");

                const string gizliBaslik = "Veritabanında düz metin görünmemeli";
                Soru soru = GecerliSoru();
                soru.Baslik = gizliBaslik;
                File.WriteAllText(jsonYolu, JsonSerializer.Serialize(new[] { soru }));

                _ = new SoruServisi(jsonYolu, dbYolu);

                using var baglanti = new SqliteConnection($"Data Source={dbYolu}");
                baglanti.Open();
                using var komut = baglanti.CreateCommand();
                komut.CommandText = "SELECT Baslik, Veri FROM Sorular;";
                using var okuyucu = komut.ExecuteReader();

                Assert.True(okuyucu.Read());
                string dbBaslik = okuyucu.GetString(0);
                string dbVeri = okuyucu.GetString(1);

                Assert.NotEqual(gizliBaslik, dbBaslik);
                Assert.True(SoruKoruyucu.OzetMi(dbBaslik));
                Assert.DoesNotContain(gizliBaslik, dbVeri);
                Assert.True(SoruKoruyucu.SifreliMi(dbVeri));
            }
            finally
            {
                GeciciKlasoruSil(klasor);
            }
        }

        [Fact]
        public void PaketOlustur_SifreliPaketUretir_ve_ServisOkuyabilir()
        {
            string klasor = GeciciKlasor();
            try
            {
                string jsonYolu = Path.Combine(klasor, "Sorular.json");
                string paketYolu = Path.Combine(klasor, "Sorular.dat");
                string dbYolu = Path.Combine(klasor, "sorular.db");

                const string gizliBaslik = "Şifreli paketteki gizli soru";
                Soru soru = GecerliSoru();
                soru.Baslik = gizliBaslik;
                File.WriteAllText(jsonYolu, JsonSerializer.Serialize(new[] { soru }));

                int adet = SoruServisi.PaketOlustur(jsonYolu, paketYolu);

                Assert.Equal(1, adet);
                byte[] paket = File.ReadAllBytes(paketYolu);
                Assert.True(SoruKoruyucu.PaketMi(paket));
                Assert.DoesNotContain(gizliBaslik, System.Text.Encoding.UTF8.GetString(paket));

                var servis = new SoruServisi(paketYolu, dbYolu);

                Assert.Equal(1, servis.SoruSayisi());
                Assert.Equal(gizliBaslik, servis.TumSorulariGetir()[0].Baslik);
            }
            finally
            {
                GeciciKlasoruSil(klasor);
            }
        }

        [Fact]
        public void EskiDuzMetinVeritabani_SifreliSekleYukseltilir()
        {
            string klasor = GeciciKlasor();
            try
            {
                string jsonYolu = Path.Combine(klasor, "Sorular.json");
                string dbYolu = Path.Combine(klasor, "sorular.db");

                Soru soru = GecerliSoru();
                soru.Baslik = "Eski düz metin sorusu";

                using (var baglanti = new SqliteConnection($"Data Source={dbYolu}"))
                {
                    baglanti.Open();
                    using var kur = baglanti.CreateCommand();
                    kur.CommandText =
                        "CREATE TABLE Sorular (" +
                        "  Id INTEGER PRIMARY KEY AUTOINCREMENT," +
                        "  Baslik TEXT NOT NULL UNIQUE," +
                        "  Disiplin INTEGER NOT NULL," +
                        "  Zorluk INTEGER NOT NULL," +
                        "  Veri TEXT NOT NULL);" +
                        "CREATE TABLE Meta (Anahtar TEXT PRIMARY KEY, Deger TEXT NOT NULL);";
                    kur.ExecuteNonQuery();

                    using var ekle = baglanti.CreateCommand();
                    ekle.CommandText =
                        "INSERT INTO Sorular (Baslik, Disiplin, Zorluk, Veri) " +
                        "VALUES ($baslik, $disiplin, $zorluk, $veri);";
                    ekle.Parameters.AddWithValue("$baslik", soru.Baslik);
                    ekle.Parameters.AddWithValue("$disiplin", (int)soru.Disiplin);
                    ekle.Parameters.AddWithValue("$zorluk", (int)soru.Zorluk);
                    ekle.Parameters.AddWithValue("$veri", JsonSerializer.Serialize(soru));
                    ekle.ExecuteNonQuery();
                }

                File.WriteAllText(jsonYolu, JsonSerializer.Serialize(new[] { soru }));

                var servis = new SoruServisi(jsonYolu, dbYolu);

                using var kontrol = new SqliteConnection($"Data Source={dbYolu}");
                kontrol.Open();
                using var komut = kontrol.CreateCommand();
                komut.CommandText = "SELECT Baslik, Veri FROM Sorular;";
                using var okuyucu = komut.ExecuteReader();

                Assert.True(okuyucu.Read());
                Assert.True(SoruKoruyucu.OzetMi(okuyucu.GetString(0)));
                Assert.True(SoruKoruyucu.SifreliMi(okuyucu.GetString(1)));
                Assert.Equal(1, servis.SoruSayisi());
            }
            finally
            {
                GeciciKlasoruSil(klasor);
            }
        }

        [Fact]
        public void GercekPaket_UcModulIcinAraSinavIcermelidir()
        {
            string paketYolu = Path.Combine(AppContext.BaseDirectory, "Data", "Sorular.dat");
            Assert.True(File.Exists(paketYolu),
                "Şifreli soru paketi bulunamadı. tools/SoruPaketleyici ile Data/Sorular.dat üretilmeli.");

            string klasor = GeciciKlasor();
            try
            {
                string dbYolu = Path.Combine(klasor, "sorular.db");
                var servis = new SoruServisi(paketYolu, dbYolu);

                Assert.True(servis.SoruSayisi() >= 18, "Soru havuzu beklenen büyüklükte değil.");
                Assert.Equal(5, servis.AraSinavSorulariniGetir(DisiplinTuru.GorselProgramlama).Count);
                Assert.Equal(5, servis.AraSinavSorulariniGetir(DisiplinTuru.VeriTabanlari).Count);
                Assert.Equal(5, servis.AraSinavSorulariniGetir(DisiplinTuru.SiberGuvenlik).Count);
            }
            finally
            {
                GeciciKlasoruSil(klasor);
            }
        }

        private static string GeciciKlasor()
        {
            string klasor = Path.Combine(
                Path.GetTempPath(), "kademeli-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(klasor);
            return klasor;
        }

        private static void GeciciKlasoruSil(string klasor)
        {
            try
            {
                Directory.Delete(klasor, recursive: true);
            }
            catch (IOException)
            {
                // SQLite WAL dosyaları kısa süre kilitli kalabilir; test sonucu etkilenmez.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
