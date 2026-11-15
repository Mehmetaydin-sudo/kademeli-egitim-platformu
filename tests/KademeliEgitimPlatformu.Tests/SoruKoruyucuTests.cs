using System;
using KademeliEgitimPlatformu.Services;
using Xunit;

namespace KademeliEgitimPlatformu.Tests
{
    public class SoruKoruyucuTests
    {
        [Fact]
        public void Ozetle_ve_Dogrula_BcryptIleCalisir()
        {
            const string parola = "GucluParola!2026";

            string ozet = SoruKoruyucu.Ozetle(parola);

            Assert.True(SoruKoruyucu.BcryptOzetiMi(ozet));
            Assert.True(SoruKoruyucu.Dogrula(parola, ozet));
            Assert.False(SoruKoruyucu.Dogrula("YanlisParola", ozet));
        }

        [Fact]
        public void Ozetle_AyniParolaIcinFarkliTuzUretir()
        {
            const string parola = "AyniParola123";

            string birinci = SoruKoruyucu.Ozetle(parola);
            string ikinci = SoruKoruyucu.Ozetle(parola);

            Assert.NotEqual(birinci, ikinci);
            Assert.True(SoruKoruyucu.Dogrula(parola, birinci));
            Assert.True(SoruKoruyucu.Dogrula(parola, ikinci));
        }

        [Fact]
        public void KullaniciServisi_ParolaOzetle_ve_Dogrula()
        {
            const string parola = "OgrenciSifre#42";

            string hash = KullaniciServisi.ParolaOzetle(parola);

            Assert.True(SoruKoruyucu.BcryptOzetiMi(hash));
            Assert.True(KullaniciServisi.ParolaDogrula(parola, hash));
            Assert.False(KullaniciServisi.ParolaDogrula("HataliSifre", hash));
        }

        [Fact]
        public void BosParola_OzetlenirkenHataFirlatir()
        {
            Assert.Throws<ArgumentException>(() => SoruKoruyucu.Ozetle("   "));
            Assert.Throws<ArgumentException>(() => KullaniciServisi.ParolaOzetle(""));
        }
    }
}
