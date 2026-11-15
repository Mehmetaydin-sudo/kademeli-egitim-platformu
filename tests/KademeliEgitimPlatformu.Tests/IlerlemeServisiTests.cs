using System;
using KademeliEgitimPlatformu.Services;
using Xunit;

namespace KademeliEgitimPlatformu.Tests
{
    public class IlerlemeServisiTests
    {
        [Fact]
        public void BosProfil_VarsayilanDegerleriDondurur()
        {
            var servis = new IlerlemeServisi(anahtarOneki: "test-" + Guid.NewGuid().ToString("N"));
            try
            {
                IlerlemeDurumu durum = servis.Yukle();

                Assert.Equal(0, durum.GorselBasari);
                Assert.Equal(0, durum.VeriTabaniBasari);
                Assert.Equal(0, durum.SiberBasari);
                Assert.Equal(3, durum.IpucuJetonu);
                Assert.False(durum.HataAvcisiKazanildi);
                Assert.False(durum.GorselAraSinavGecti);
                Assert.False(durum.VeriTabaniAraSinavGecti);
                Assert.False(durum.SiberAraSinavGecti);
                Assert.False(durum.OrtaModGecti);
            }
            finally
            {
                servis.Temizle();
            }
        }

        [Fact]
        public void Kaydet_ve_Yukle_AyniDegerleriKorur()
        {
            var servis = new IlerlemeServisi(anahtarOneki: "test-" + Guid.NewGuid().ToString("N"));
            try
            {
                var beklenen = new IlerlemeDurumu(80, 65, 90, 7, true, true, false, true, true);

                servis.Kaydet(beklenen);
                IlerlemeDurumu yuklenen = servis.Yukle();

                Assert.Equal(beklenen, yuklenen);
            }
            finally
            {
                servis.Temizle();
            }
        }

        [Fact]
        public void Kaydet_UzerineYazar()
        {
            var servis = new IlerlemeServisi(anahtarOneki: "test-" + Guid.NewGuid().ToString("N"));
            try
            {
                servis.Kaydet(new IlerlemeDurumu(10, 20, 30, 1, false));
                servis.Kaydet(new IlerlemeDurumu(40, 50, 60, 2, true));

                IlerlemeDurumu durum = servis.Yukle();

                Assert.Equal(40, durum.GorselBasari);
                Assert.Equal(50, durum.VeriTabaniBasari);
                Assert.Equal(60, durum.SiberBasari);
                Assert.Equal(2, durum.IpucuJetonu);
                Assert.True(durum.HataAvcisiKazanildi);
            }
            finally
            {
                servis.Temizle();
            }
        }

        [Fact]
        public void SeviyeGecisSinaviKaydet_PostgreSqlTablosunaYazarVeOkur()
        {
            var servis = new IlerlemeServisi(anahtarOneki: "test-" + Guid.NewGuid().ToString("N"));
            var kayit = new SeviyeGecisSinavKaydi(
                GecisTuru: "KolaydanOrtaya",
                Disiplin: null,
                HedefZorluk: KademeliEgitimPlatformu.Models.ZorlukSeviyesi.Orta,
                ToplamSoru: 5,
                DogruSayisi: 4,
                BasariYuzdesi: 80,
                BarajYuzdesi: 70,
                BasariliMi: true,
                GecenSureSaniye: 45);

            servis.SeviyeGecisSinaviKaydet(kayit);
            var sonKayitlar = servis.SonSeviyeGecisSinavlari(5);

            Assert.NotEmpty(sonKayitlar);
            Assert.Equal("KolaydanOrtaya", sonKayitlar[0].GecisTuru);
            Assert.Equal(80, sonKayitlar[0].BasariYuzdesi);
            Assert.True(sonKayitlar[0].BasariliMi);
        }
    }
}
