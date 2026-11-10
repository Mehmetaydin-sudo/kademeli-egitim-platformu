using System.Collections.Generic;
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
    }
}
