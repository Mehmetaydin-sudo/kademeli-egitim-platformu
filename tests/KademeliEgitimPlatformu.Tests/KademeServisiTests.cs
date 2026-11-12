using KademeliEgitimPlatformu.Services;
using Xunit;

namespace KademeliEgitimPlatformu.Tests
{
    public class KademeServisiTests
    {
        [Fact]
        public void YeniKullanici_OrtaVeZorModKilitli()
        {
            var durum = new IlerlemeDurumu();

            Assert.False(KademeServisi.OrtaModAcikMi(durum));
            Assert.False(KademeServisi.ZorModAcikMi(durum));
            Assert.Equal(0, KademeServisi.GecilenAraSinavSayisi(durum));
        }

        [Fact]
        public void UcAraSinavGecilince_OrtaModAcilir()
        {
            var durum = new IlerlemeDurumu(
                GorselAraSinavGecti: true,
                VeriTabaniAraSinavGecti: true,
                SiberAraSinavGecti: true);

            Assert.True(KademeServisi.OrtaModAcikMi(durum));
            Assert.False(KademeServisi.ZorModAcikMi(durum));
            Assert.Equal(3, KademeServisi.GecilenAraSinavSayisi(durum));
        }

        [Fact]
        public void OrtaModGecilince_ZorModAcilir()
        {
            var durum = new IlerlemeDurumu(
                GorselAraSinavGecti: true,
                VeriTabaniAraSinavGecti: true,
                SiberAraSinavGecti: true,
                OrtaModGecti: true);

            Assert.True(KademeServisi.OrtaModAcikMi(durum));
            Assert.True(KademeServisi.ZorModAcikMi(durum));
        }

        [Theory]
        [InlineData(4, 5, true)]   // %80
        [InlineData(7, 10, true)]  // %70 (tam baraj)
        [InlineData(3, 5, false)]  // %60
        [InlineData(0, 0, false)]  // soru yok
        public void AraSinavBaraji_DogruHesaplanir(int dogru, int toplam, bool beklenen)
        {
            Assert.Equal(beklenen, KademeServisi.AraSinavGectiMi(dogru, toplam));
        }

        [Theory]
        [InlineData(3, 4, true)]   // %75
        [InlineData(8, 10, true)]  // %80
        [InlineData(7, 10, false)] // %70
        [InlineData(2, 4, false)]  // %50
        public void OrtaModBaraji_DogruHesaplanir(int dogru, int toplam, bool beklenen)
        {
            Assert.Equal(beklenen, KademeServisi.OrtaModGectiMi(dogru, toplam));
        }

        [Fact]
        public void EksikAraSinavlar_AdlariniDondurur()
        {
            var durum = new IlerlemeDurumu(GorselAraSinavGecti: true);

            var eksikler = KademeServisi.EksikAraSinavlar(durum);

            Assert.Equal(2, eksikler.Count);
            Assert.Contains("Veri Tabanları", eksikler);
            Assert.Contains("Siber Güvenlik", eksikler);
        }
    }
}
