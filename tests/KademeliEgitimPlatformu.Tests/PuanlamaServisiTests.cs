using System;
using System.Collections.Generic;
using KademeliEgitimPlatformu.Models;
using KademeliEgitimPlatformu.Services;
using Xunit;

namespace KademeliEgitimPlatformu.Tests
{
    public class PuanlamaServisiTests
    {
        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(0, 4, 0)]
        [InlineData(1, 2, 50)]
        [InlineData(2, 3, 67)]
        [InlineData(4, 4, 100)]
        public void Yuzde_DogruHesaplanir(int dogru, int toplam, int beklenen)
        {
            Assert.Equal(beklenen, PuanlamaServisi.Yuzde(dogru, toplam));
        }

        [Fact]
        public void DisiplinBazindaHesapla_HerDisipliniAyriDegerlendirir()
        {
            var sorular = new List<Soru>
            {
                Soru(DisiplinTuru.GorselProgramlama),
                Soru(DisiplinTuru.GorselProgramlama),
                Soru(DisiplinTuru.VeriTabanlari),
                Soru(DisiplinTuru.SiberGuvenlik)
            };
            var cevaplar = new List<bool> { true, false, true, false };

            var sonuclar = PuanlamaServisi.DisiplinBazindaHesapla(sorular, cevaplar);

            Assert.Equal(3, sonuclar.Count);
            Assert.Equal(new DisiplinSonucu(DisiplinTuru.GorselProgramlama, 1, 2), sonuclar[0]);
            Assert.Equal(new DisiplinSonucu(DisiplinTuru.VeriTabanlari, 1, 1), sonuclar[1]);
            Assert.Equal(new DisiplinSonucu(DisiplinTuru.SiberGuvenlik, 0, 1), sonuclar[2]);
            Assert.Equal(50, sonuclar[0].Yuzde);
            Assert.Equal(100, sonuclar[1].Yuzde);
        }

        [Fact]
        public void Birlestir_EnIyiSonucuKorur_ve_JetonEkler()
        {
            var mevcut = new IlerlemeDurumu(80, 10, 40, 2, false);
            var sonuclar = new List<DisiplinSonucu>
            {
                new(DisiplinTuru.GorselProgramlama, 1, 4), // %25 - düşürmemeli
                new(DisiplinTuru.VeriTabanlari, 3, 4),     // %75 - yükseltmeli
                new(DisiplinTuru.SiberGuvenlik, 0, 4)      // %0  - düşürmemeli
            };

            IlerlemeDurumu sonuc = PuanlamaServisi.Birlestir(mevcut, sonuclar, kazanilanJeton: 1);

            Assert.Equal(80, sonuc.GorselBasari);
            Assert.Equal(75, sonuc.VeriTabaniBasari);
            Assert.Equal(40, sonuc.SiberBasari);
            Assert.Equal(3, sonuc.IpucuJetonu);
            Assert.False(sonuc.HataAvcisiKazanildi);
        }

        [Fact]
        public void Birlestir_NegatifJetonEklenmez()
        {
            var mevcut = new IlerlemeDurumu(0, 0, 0, 5, false);

            IlerlemeDurumu sonuc = PuanlamaServisi.Birlestir(
                mevcut,
                Array.Empty<DisiplinSonucu>(),
                kazanilanJeton: -3);

            Assert.Equal(5, sonuc.IpucuJetonu);
        }

        private static Soru Soru(DisiplinTuru disiplin) => new()
        {
            Disiplin = disiplin,
            Baslik = "Test",
            Secenekler = new List<string> { "a", "b", "c", "d" }
        };
    }
}
