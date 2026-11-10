using System;
using System.Linq;
using KademeliEgitimPlatformu.Services;
using Xunit;

namespace KademeliEgitimPlatformu.Tests
{
    public class SimulatorServisiTests
    {
        private readonly SimulatorServisi _servis = new();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void BosKod_HataDondurur(string? kod)
        {
            SimulatorSonucu sonuc = _servis.Derle(kod);

            Assert.False(sonuc.Basarili);
            Assert.Contains(sonuc.Tanilar, t => t.Kod == "BOŞ");
        }

        [Fact]
        public void GecerliKod_BasariliDerlenir()
        {
            const string kod =
                "public class Hesap\n" +
                "{\n" +
                "    public int Topla(int a, int b)\n" +
                "    {\n" +
                "        return a + b;\n" +
                "    }\n" +
                "}";

            SimulatorSonucu sonuc = _servis.Derle(kod);

            Assert.True(sonuc.Basarili, SimulatorServisi.RaporMetni(sonuc));
            Assert.Equal(0, sonuc.HataSayisi);
        }

        [Fact]
        public void KullanimYonergeliKod_SarmalanmadanDerlenir()
        {
            const string kod =
                "using System;\n\n" +
                "public class Yazici\n" +
                "{\n" +
                "    public void Yaz()\n" +
                "    {\n" +
                "        Console.WriteLine(\"merhaba\");\n" +
                "    }\n" +
                "}";

            SimulatorSonucu sonuc = _servis.Derle(kod);

            Assert.True(sonuc.Basarili, SimulatorServisi.RaporMetni(sonuc));
        }

        [Fact]
        public void EksikNoktaliVirgul_SatirBilgisiyleBildirilir()
        {
            const string kod =
                "public class Ornek\n" +
                "{\n" +
                "    public void Calistir()\n" +
                "    {\n" +
                "        int sayi = 1\n" +
                "    }\n" +
                "}";

            SimulatorSonucu sonuc = _servis.Derle(kod);

            Assert.False(sonuc.Basarili);
            SimulatorTanisi tani = Assert.Single(sonuc.Tanilar, t => t.Kod == "CS1002");
            Assert.True(tani.Satir >= 1);
            Assert.Contains("noktalı virgül", tani.TurkceIpucu);
        }

        [Fact]
        public void AtamaOperatoru_TurkceKarsilastirmaIpucuVerir()
        {
            const string kod =
                "public void Kontrol()\n" +
                "{\n" +
                "    int adminRol = 1;\n" +
                "    if (adminRol = 1);\n" +
                "    {\n" +
                "    }\n" +
                "}";

            SimulatorSonucu sonuc = _servis.Derle(kod);

            Assert.False(sonuc.Basarili);
            Assert.Contains(sonuc.Tanilar, t =>
                t.TurkceIpucu.Contains("karşılaştırma", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void CokUzunKod_Reddedilir()
        {
            string kod = new string('a', SimulatorServisi.MaksKodUzunlugu + 1);

            SimulatorSonucu sonuc = _servis.Derle(kod);

            Assert.False(sonuc.Basarili);
            Assert.Contains(sonuc.Tanilar, t => t.Kod == "UZUN");
        }

        [Fact]
        public void RaporMetni_BasariliVeBasarisizDurumuAyirtEder()
        {
            string basarili = SimulatorServisi.RaporMetni(_servis.Derle("public class A { }"));
            string basarisiz = SimulatorServisi.RaporMetni(_servis.Derle("public class A { void M() { int x = 1 } }"));

            Assert.Contains("DERLEME BAŞARILI", basarili);
            Assert.Contains("DERLEME BAŞARISIZ", basarisiz);
        }
    }
}
