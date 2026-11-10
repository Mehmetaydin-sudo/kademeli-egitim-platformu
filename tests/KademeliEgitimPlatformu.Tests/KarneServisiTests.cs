using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KademeliEgitimPlatformu.Models;
using KademeliEgitimPlatformu.Services;
using Xunit;

namespace KademeliEgitimPlatformu.Tests
{
    public class KarneServisiTests
    {
        [Fact]
        public void Kaydet_ve_SonKayitlar_EnYeniyiBastaDondurur()
        {
            using var gecici = new GeciciVeritabani();
            var servis = new KarneServisi(gecici.DosyaYolu);

            servis.Kaydet(new[]
            {
                Kayit("İlk soru"),
                Kayit("İkinci soru")
            });

            List<YanlisKaydi> kayitlar = servis.SonKayitlar();

            Assert.Equal(2, kayitlar.Count);
            Assert.Equal("İkinci soru", kayitlar[0].Baslik);
            Assert.Equal("İlk soru", kayitlar[1].Baslik);
            Assert.Equal(DisiplinTuru.SiberGuvenlik, kayitlar[0].Disiplin);
            Assert.Equal(ZorlukSeviyesi.Zor, kayitlar[0].Zorluk);
        }

        [Fact]
        public void Temizle_TumKayitlariSiler()
        {
            using var gecici = new GeciciVeritabani();
            var servis = new KarneServisi(gecici.DosyaYolu);

            servis.Kaydet(new[] { Kayit("Silinecek") });
            servis.Temizle();

            Assert.Empty(servis.SonKayitlar());
        }

        [Fact]
        public void MaksKayitSayisi_Asilmaz()
        {
            using var gecici = new GeciciVeritabani();
            var servis = new KarneServisi(gecici.DosyaYolu);

            var kayitlar = Enumerable.Range(0, KarneServisi.MaksKayitSayisi + 5)
                .Select(i => Kayit($"Soru {i}"))
                .ToList();

            servis.Kaydet(kayitlar);

            List<YanlisKaydi> sonuc = servis.SonKayitlar(KarneServisi.MaksKayitSayisi);

            Assert.Equal(KarneServisi.MaksKayitSayisi, sonuc.Count);
            Assert.Equal("Soru 304", sonuc[0].Baslik); // en yeni kayıt
        }

        [Fact]
        public void KarneRaporu_BeceriVeYanlislariIcerir()
        {
            var durum = new IlerlemeDurumu(80, 60, 40, 2, true);
            var yanlislar = new List<YanlisKaydi>
            {
                Kayit("Kapanmayan parantez")
            };

            string rapor = KarneRaporu.Olustur(
                durum,
                yanlislar,
                sonSinavSuresi: TimeSpan.FromSeconds(125),
                raporTarihi: new DateTime(2026, 11, 11, 10, 30, 0),
                surum: "v0.5.0");

            Assert.Contains("Görsel Programlama : %80", rapor);
            Assert.Contains("Veri Tabanları     : %60", rapor);
            Assert.Contains("Siber Güvenlik     : %40", rapor);
            Assert.Contains("Hata Avcısı Rozeti : Kazanıldı", rapor);
            Assert.Contains("02:05", rapor);
            Assert.Contains("Kapanmayan parantez", rapor);
            Assert.Contains("v0.5.0", rapor);
            Assert.Contains("11.11.2026 10:30", rapor);
        }

        [Fact]
        public void KarneRaporu_YanlisYoksaTebrikMesajiYazar()
        {
            string rapor = KarneRaporu.Olustur(new IlerlemeDurumu(), Array.Empty<YanlisKaydi>());

            Assert.Contains("Kayıtlı yanlış cevap bulunmuyor. Tebrikler!", rapor);
        }

        private static YanlisKaydi Kayit(string baslik) => new(
            new DateTime(2026, 11, 11, 10, 0, 0, DateTimeKind.Local),
            DisiplinTuru.SiberGuvenlik,
            ZorlukSeviyesi.Zor,
            baslik,
            "Seçilen",
            "Doğru",
            "Açıklama");

        /// <summary>Test başına benzersiz, geçici SQLite dosyası üretir.</summary>
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
                    // Test sonrası geçici klasör silinemezse sorun değil.
                }
            }
        }
    }
}
