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
    }
}
