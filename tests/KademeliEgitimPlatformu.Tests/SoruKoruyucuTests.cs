using System.Text;
using KademeliEgitimPlatformu.Services;
using Xunit;

namespace KademeliEgitimPlatformu.Tests
{
    public class SoruKoruyucuTests
    {
        [Fact]
        public void PaketSifrele_ve_Coz_AyniVeriyiKorur()
        {
            byte[] duzVeri = Encoding.UTF8.GetBytes("Şıklar ve doğru cevap burada.");

            byte[] paket = SoruKoruyucu.PaketSifrele(duzVeri);
            byte[] cozulen = SoruKoruyucu.PaketCoz(paket);

            Assert.Equal(duzVeri, cozulen);
            Assert.True(SoruKoruyucu.PaketMi(paket));
        }

        [Fact]
        public void Paket_DuzMetinSoruIcermez()
        {
            const string gizliMetin = "Bu sorunun cevabı kesinlikle gizli olmalı.";
            byte[] paket = SoruKoruyucu.PaketSifrele(Encoding.UTF8.GetBytes(gizliMetin));

            string paketMetni = Encoding.UTF8.GetString(paket);

            Assert.DoesNotContain(gizliMetin, paketMetni);
            Assert.StartsWith("KEP1", paketMetni);
        }

        [Fact]
        public void MetinSifrele_ve_Coz_AyniMetniKorur()
        {
            const string metin = "Türkçe karakterler: ığüşöçİĞÜŞÖÇ";

            string sifreli = SoruKoruyucu.MetinSifrele(metin);
            string cozulen = SoruKoruyucu.MetinCoz(sifreli);

            Assert.NotEqual(metin, sifreli);
            Assert.StartsWith("KEP1:", sifreli);
            Assert.Equal(metin, cozulen);
        }

        [Fact]
        public void EskiDuzMetin_MetinCoz_AynenDoner()
        {
            const string eskiKayit = "{\"Baslik\":\"v0.6 düz metin kaydı\"}";

            Assert.Equal(eskiKayit, SoruKoruyucu.MetinCoz(eskiKayit));
            Assert.False(SoruKoruyucu.SifreliMi(eskiKayit));
        }

        [Fact]
        public void BaslikOzeti_AyniBasliktaAyni_FarkliBasliktaFarkli()
        {
            string birinci = SoruKoruyucu.BaslikOzeti("Aynı soru başlığı");
            string ikinci = SoruKoruyucu.BaslikOzeti("Aynı soru başlığı");
            string farkli = SoruKoruyucu.BaslikOzeti("Farklı soru başlığı");

            Assert.Equal(birinci, ikinci);
            Assert.NotEqual(birinci, farkli);
            Assert.True(SoruKoruyucu.OzetMi(birinci));
            Assert.False(SoruKoruyucu.OzetMi("düz başlık"));
        }
    }
}
