using System;
using System.Collections.Generic;
using System.Linq;
using KademeliEgitimPlatformu.Models;
using Xunit;

namespace KademeliEgitimPlatformu.Tests
{
    public class SoruGorunumuTests
    {
        private static Soru OrnekSoru() => new()
        {
            Id = 1,
            Disiplin = DisiplinTuru.GorselProgramlama,
            Zorluk = ZorlukSeviyesi.Kolay,
            Baslik = "Örnek soru",
            Secenekler = new List<string> { "A şıkkı", "B şıkkı", "C şıkkı", "D şıkkı" },
            DogruSecenekIndex = 2,
            YanlisCevapAciklamalari = new Dictionary<int, string>
            {
                [0] = "A neden yanlış",
                [1] = "B neden yanlış",
                [3] = "D neden yanlış"
            }
        };

        [Fact]
        public void Karistirmadan_DogruCevapKorunur()
        {
            Soru soru = OrnekSoru();

            SoruGorunumu gorunum = SoruGorunumu.Olustur(soru, karistir: false);

            Assert.Equal(soru.Secenekler, gorunum.Secenekler);
            Assert.Equal(soru.DogruSecenekIndex, gorunum.DogruSecenekIndex);
            Assert.Equal("C şıkkı", gorunum.DogruCevap);
            Assert.Equal("A neden yanlış", gorunum.Aciklama(0));
        }

        [Fact]
        public void Karistirinca_DogruCevapVeAciklamalarTasinir()
        {
            Soru soru = OrnekSoru();

            // Farklı rastgele sıralamalar denenir; değişmez her seferinde korunmalı.
            for (int deneme = 0; deneme < 50; deneme++)
            {
                SoruGorunumu gorunum = SoruGorunumu.Olustur(soru, karistir: true, new Random(deneme));

                Assert.Equal(4, gorunum.Secenekler.Count);
                Assert.Equal(
                    soru.Secenekler.OrderBy(x => x),
                    gorunum.Secenekler.OrderBy(x => x));

                Assert.Equal("C şıkkı", gorunum.DogruCevap);

                // Her yanlış şıkkın açıklaması kendi metniyle birlikte taşınmalı.
                for (int i = 0; i < gorunum.Secenekler.Count; i++)
                {
                    if (i == gorunum.DogruSecenekIndex)
                    {
                        Assert.Null(gorunum.Aciklama(i));
                        continue;
                    }

                    string metin = gorunum.Secenekler[i];
                    int eskiIndex = soru.Secenekler.IndexOf(metin);
                    Assert.Equal(soru.YanlisCevapAciklamalari[eskiIndex], gorunum.Aciklama(i));
                }
            }
        }

        [Fact]
        public void AyniRastgeleTohumu_AyniSirayiUretir()
        {
            Soru soru = OrnekSoru();

            SoruGorunumu birinci = SoruGorunumu.Olustur(soru, karistir: true, new Random(42));
            SoruGorunumu ikinci = SoruGorunumu.Olustur(soru, karistir: true, new Random(42));

            Assert.Equal(birinci.Secenekler, ikinci.Secenekler);
            Assert.Equal(birinci.DogruSecenekIndex, ikinci.DogruSecenekIndex);
        }

        [Fact]
        public void NullSoru_HataFirlatir()
        {
            Assert.Throws<ArgumentNullException>(() => SoruGorunumu.Olustur(null!, karistir: true));
        }
    }
}
