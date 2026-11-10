using System;
using System.IO;
using KademeliEgitimPlatformu.Services;
using Xunit;

namespace KademeliEgitimPlatformu.Tests
{
    public class IlerlemeServisiTests
    {
        [Fact]
        public void BosVeritabani_VarsayilanDegerleriDondurur()
        {
            using var gecici = new GeciciVeritabani();
            var servis = new IlerlemeServisi(gecici.DosyaYolu);

            IlerlemeDurumu durum = servis.Yukle();

            Assert.Equal(0, durum.GorselBasari);
            Assert.Equal(0, durum.VeriTabaniBasari);
            Assert.Equal(0, durum.SiberBasari);
            Assert.Equal(3, durum.IpucuJetonu);
            Assert.False(durum.HataAvcisiKazanildi);
        }

        [Fact]
        public void Kaydet_ve_Yukle_AyniDegerleriKorur()
        {
            using var gecici = new GeciciVeritabani();
            var servis = new IlerlemeServisi(gecici.DosyaYolu);
            var beklenen = new IlerlemeDurumu(80, 65, 90, 7, true);

            servis.Kaydet(beklenen);
            IlerlemeDurumu yuklenen = servis.Yukle();

            Assert.Equal(beklenen, yuklenen);
        }

        [Fact]
        public void Kaydet_UzerineYazar()
        {
            using var gecici = new GeciciVeritabani();
            var servis = new IlerlemeServisi(gecici.DosyaYolu);

            servis.Kaydet(new IlerlemeDurumu(10, 20, 30, 1, false));
            servis.Kaydet(new IlerlemeDurumu(40, 50, 60, 2, true));

            IlerlemeDurumu durum = servis.Yukle();

            Assert.Equal(40, durum.GorselBasari);
            Assert.Equal(50, durum.VeriTabaniBasari);
            Assert.Equal(60, durum.SiberBasari);
            Assert.Equal(2, durum.IpucuJetonu);
            Assert.True(durum.HataAvcisiKazanildi);
        }

        private sealed class GeciciVeritabani : IDisposable
        {
            private readonly string _klasor;

            public GeciciVeritabani()
            {
                _klasor = Path.Combine(Path.GetTempPath(), "kademeli-test", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(_klasor);
                DosyaYolu = Path.Combine(_klasor, "ilerleme.db");
            }

            public string DosyaYolu { get; }

            public void Dispose()
            {
                try
                {
                    Directory.Delete(_klasor, recursive: true);
                }
                catch (IOException)
                {
                }
            }
        }
    }
}
