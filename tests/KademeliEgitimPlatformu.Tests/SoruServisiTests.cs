using System;
using System.Collections.Generic;
using System.IO;
using KademeliEgitimPlatformu.Models;
using KademeliEgitimPlatformu.Services;
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
        public void PostgreSql_VarsayilanSoruHavuzu_UcModulIcinAraSinavIcerir()
        {
            var servis = new SoruServisi();

            Assert.True(servis.SoruSayisi() >= 18);
            Assert.True(servis.AraSinavSorulariniGetir(DisiplinTuru.GorselProgramlama).Count >= 5);
            Assert.True(servis.AraSinavSorulariniGetir(DisiplinTuru.VeriTabanlari).Count >= 5);
            Assert.True(servis.AraSinavSorulariniGetir(DisiplinTuru.SiberGuvenlik).Count >= 5);
        }

        [Fact]
        public void ExcelDisariVeIceAktarim_PostgreSqlIleCalisir()
        {
            string klasor = Path.Combine(Path.GetTempPath(), "kademeli-xlsx-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(klasor);

            try
            {
                string xlsxYolu = Path.Combine(klasor, "sorular.xlsx");
                string csvPaketYolu = Path.Combine(klasor, "usb_paket.csv");
                var servis = new SoruServisi();

                int disariAdet = servis.DisariAktar(xlsxYolu);
                Assert.True(File.Exists(xlsxYolu));
                Assert.True(disariAdet >= 18);

                int paketAdet = SoruServisi.PaketOlustur(xlsxYolu, csvPaketYolu);
                Assert.Equal(disariAdet, paketAdet);
                Assert.True(File.Exists(csvPaketYolu));

                int yeniEklenen = servis.DisaridanPaketYukle(csvPaketYolu);
                Assert.Equal(0, yeniEklenen); // Aynı başlıklı sorular tekrar eklenmez
            }
            finally
            {
                try { Directory.Delete(klasor, recursive: true); } catch { }
            }
        }

        [Fact]
        public void SeviyeGecisSorulari_KolayOrtaVeOrtaZorIcinGetirilir()
        {
            var servis = new SoruServisi();

            var kolayOrtaSorular = servis.SeviyeGecisSorulariniGetir(ZorlukSeviyesi.Orta);
            var ortaZorSorular = servis.SeviyeGecisSorulariniGetir(ZorlukSeviyesi.Zor);

            Assert.NotEmpty(kolayOrtaSorular);
            Assert.NotEmpty(ortaZorSorular);
        }

        [Fact]
        public void VarsayilanSoruHavuzu_KolayOrtaVeZorSeviyeleriniIcerir()
        {
            var servis = new SoruServisi();

            Assert.NotEmpty(servis.TumSorulariGetir(ZorlukSeviyesi.Kolay));
            Assert.NotEmpty(servis.TumSorulariGetir(ZorlukSeviyesi.Orta));
            Assert.NotEmpty(servis.TumSorulariGetir(ZorlukSeviyesi.Zor));
        }

        [Fact]
        public void OtomatikSemaKontrolu_TumZorunluTablolariDogrular()
        {
            VeritabaniBaglantisi.TablolariHazirla();
            using var baglanti = VeritabaniBaglantisi.Ac();

            IReadOnlyList<string> eksikTablolar = VeritabaniBaglantisi.EksikTablolariGetir(baglanti);
            Assert.Empty(eksikTablolar);
        }

        [Fact]
        public void VeriYolu_SaltOkunurDosyalariBaseDirectoryden_CiktilariBelgelerimdenAlir()
        {
            Assert.StartsWith(AppContext.BaseDirectory, VeriYolu.AyarDosyasiYolu, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(AppContext.BaseDirectory, VeriYolu.SemaDosyasiYolu, StringComparison.OrdinalIgnoreCase);

            string ciktiYolu = VeriYolu.GuvenliCiktiYolu("ornek_disa_aktarim.xlsx");
            Assert.Equal(Path.Combine(VeriYolu.VeriKlasoru, "ornek_disa_aktarim.xlsx"), ciktiYolu);
            Assert.DoesNotContain(AppContext.BaseDirectory.TrimEnd('\\', '/'), ciktiYolu, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TurkceWindows1254Csv_BozulmadanOkunurVeDogrulanir()
        {
            string klasor = Path.Combine(Path.GetTempPath(), "kademeli-cp1254-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(klasor);

            try
            {
                string csvYolu = Path.Combine(klasor, "turkce_ansi.csv");
                string ciktiXlsx = Path.Combine(klasor, "dogrulanmis.xlsx");
                string icerik =
                    "Disiplin,Zorluk,Etiket,Baslik,KodBlogu,SecenekA,SecenekB,SecenekC,SecenekD,DogruSecenekIndex,AciklamaA,AciklamaB,AciklamaC,AciklamaD\r\n" +
                    "SiberGuvenlik,Orta,,\"Şifreleme ve güvenlik ölçümü nedir?\",,\"Çözüm A\",\"Öğrenim B\",\"İşlem C\",\"Güncel D\",1,\"Yanlış A\",,\"Yanlış C\",\"Yanlış D\"\r\n";

                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                File.WriteAllBytes(csvYolu, System.Text.Encoding.GetEncoding(1254).GetBytes(icerik));

                int adet = SoruServisi.PaketOlustur(csvYolu, ciktiXlsx);
                Assert.Equal(1, adet);
                Assert.True(File.Exists(ciktiXlsx));
            }
            finally
            {
                try { Directory.Delete(klasor, recursive: true); } catch { }
            }
        }

        [Fact]
        public void KullaniciDostuHataMesaji_BaglantiVeJsonHatalariniTurkceAciklar()
        {
            string baglantiMesaji = VeritabaniBaglantisi.KullaniciDostuHataMesaji(
                new Npgsql.NpgsqlException("Connection refused"));
            Assert.Contains("PostgreSQL 18 servisine bağlanılamadı", baglantiMesaji);

            string jsonMesaji = VeritabaniBaglantisi.KullaniciDostuHataMesaji(
                new InvalidDataException("Failed to load appsettings.json"));
            Assert.Contains("appsettings.json", jsonMesaji);
        }
    }
}
