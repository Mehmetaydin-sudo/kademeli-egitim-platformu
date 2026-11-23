using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using KademeliEgitimPlatformu.Models;
using KademeliEgitimPlatformu.Services;
using Xunit;

namespace KademeliEgitimPlatformu.Tests
{
    public class KisisellestirmeServisiTests
    {
        [Fact]
        public void XpVeSeviyeHesapla_IlerlemeVeEkstraXpIleDogruSeviyeUretir()
        {
            var servis = new KisisellestirmeServisi();
            var tercih = new KullaniciTemaTercihi { KazanilanEkstraXp = 100 };

            // (50 + 50 + 50) * 4 = 600 + 150 (orta mod) + 200 (simülatör) + 100 = 1050 XP -> Seviye 5, kalan 50/250
            var sonuc = servis.XpVeSeviyeHesapla(tercih, 50, 50, 50, ortaModGecti: true, simulatorOduluAlindi: true);

            Assert.Equal(1050, sonuc.ToplamXp);
            Assert.Equal(5, sonuc.Seviye);
            Assert.Equal(50, sonuc.MevcutSeviyeXp);
            Assert.Equal(250, sonuc.SonrakiSeviyeGereken);
            Assert.False(string.IsNullOrWhiteSpace(sonuc.SeviyeUnvani));
        }

        [Fact]
        public void RozetleriHesapla_SekizKategorideKademeGecisleriniDogruBelirler()
        {
            var servis = new KisisellestirmeServisi();
            var tercih = new KullaniciTemaTercihi
            {
                ToplamCozulenSoru = 80,
                GunlukSeri = 6,
                ToplamCalismaDakikasi = 65
            };

            var rozetler = servis.RozetleriHesapla(
                tercih,
                gorselBasari: 95,
                veriTabaniBasari: 60,
                siberBasari: 20,
                ortaModGecti: true,
                simulatorOduluAlindi: true);

            Assert.Equal(8, rozetler.Count);
            Assert.Equal(RozetKademesi.Elmas, rozetler.Single(r => r.Kod == "GORSEL").Kademe);
            Assert.Equal(RozetKademesi.Gumus, rozetler.Single(r => r.Kod == "SQL").Kademe);
            Assert.Equal(RozetKademesi.Kilitli, rozetler.Single(r => r.Kod == "SIBER").Kademe);
            Assert.Equal(RozetKademesi.Altin, rozetler.Single(r => r.Kod == "SORU").Kademe);
            Assert.Equal(RozetKademesi.Gumus, rozetler.Single(r => r.Kod == "SERI").Kademe);
            Assert.Equal(RozetKademesi.Gumus, rozetler.Single(r => r.Kod == "SURE").Kademe);
            Assert.Equal(RozetKademesi.Altin, rozetler.Single(r => r.Kod == "SIM").Kademe);
        }

        [Fact]
        public void GunlukDurumuSenkronizeEt_ArdisikGunlerdeSeriyiArtirirVeGorevOlusturur()
        {
            var servis = new KisisellestirmeServisi();
            var bugun = new DateTime(2026, 11, 23);
            var tercih = new KullaniciTemaTercihi
            {
                SonCalismaGunu = "2026-11-22",
                GunlukSeri = 3
            };

            servis.GunlukDurumuSenkronizeEt(tercih, bugun);

            Assert.Equal(4, tercih.GunlukSeri);
            Assert.Equal("2026-11-23", tercih.SonCalismaGunu);
            Assert.Equal(3, tercih.GunlukGorevler.Count);
            Assert.Single(tercih.HaftalikGecmis);
        }

        [Fact]
        public void TekRenktenPaletUret_KoyuVeAcikModlardaGecerliHexUretir()
        {
            OzelRenkPaleti koyu = KisisellestirmeServisi.TekRenktenPaletUret("Test Koyu", Color.FromArgb(14, 165, 233), koyuMod: true);
            OzelRenkPaleti acik = KisisellestirmeServisi.TekRenktenPaletUret("Test Açık", Color.FromArgb(14, 165, 233), koyuMod: false);

            Assert.Equal("#0EA5E9", koyu.VurguHex);
            Assert.Equal("#F3F4F6", koyu.YaziHex);
            Assert.Equal("#1E242B", acik.YaziHex);
            Assert.True(KisisellestirmeServisi.ParlakMi(KisisellestirmeServisi.HexToColor(acik.ArkaPlanHex, Color.Black)));
            Assert.False(KisisellestirmeServisi.ParlakMi(KisisellestirmeServisi.HexToColor(koyu.ArkaPlanHex, Color.White)));
        }

        [Fact]
        public void AcikTemaUygulandiginda_TemaYaziVeKartEtiketleriKontrastiniKorur()
        {
            Exception? hata = null;
            Color yaziRengi = Color.Empty;
            Color arkaPlanRengi = Color.Empty;
            Color kartRengi = Color.Empty;

            var t = new Thread(() =>
            {
                try
                {
                    var servis = new KisisellestirmeServisi();
                    var tercih = new KullaniciTemaTercihi { TemaAdi = "Açık Beyaz" };

                    using var form = new Form();
                    using var sidebar = new Panel();
                    using var icerik = new Panel();
                    using var profil = new Panel();
                    using var anaMenu = new Panel();
                    using var kart = new Panel { Tag = "TemaKart" };
                    using var baslik = new Label { ForeColor = Color.White, Tag = "TemaYazi" };

                    kart.Controls.Add(baslik);
                    anaMenu.Controls.Add(kart);
                    icerik.Controls.Add(anaMenu);
                    icerik.Controls.Add(profil);

                    servis.TemayiFormUzerineUygula(form, sidebar, icerik, profil, tercih);

                    yaziRengi = baslik.ForeColor;
                    arkaPlanRengi = anaMenu.BackColor;
                    kartRengi = kart.BackColor;
                }
                catch (Exception ex)
                {
                    hata = ex;
                }
            })
            { IsBackground = true };

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            Assert.True(t.Join(TimeSpan.FromSeconds(15)));

            Assert.Null(hata);
            Assert.True(KisisellestirmeServisi.ParlakMi(arkaPlanRengi));
            Assert.True(KisisellestirmeServisi.ParlakMi(kartRengi));
            // Açık zeminde yazı rengi koyu kalmalı (beyaz-üzeri-beyaz olmamalı)
            Assert.False(KisisellestirmeServisi.ParlakMi(yaziRengi));
        }

        [Fact]
        public void TercihiKaydetVeYukle_PostgreSqlMetaTablosuIleCalisirVeBaseDirectoryyeYazmaz()
        {
            string testAnahtari = "test_kisisellestirme_" + Guid.NewGuid().ToString("N");
            string geciciYedek = Path.Combine(Path.GetTempPath(), testAnahtari + ".cfg");
            var servis = new KisisellestirmeServisi(metaAnahtari: testAnahtari, ozelYedekDosyaYolu: geciciYedek);

            try
            {
                var kaydedilecek = new KullaniciTemaTercihi
                {
                    OgrenciAdi = "Yağız Test",
                    Avatar = "🏆",
                    TemaAdi = "Zümrüt Koyu",
                    KazanilanEkstraXp = 175,
                    HedefSoruSayisi = 250
                };

                servis.TercihiKaydet(kaydedilecek);

                // Program Files / BaseDirectory altına asla JSON tercih dosyası yazılmamalıdır.
                Assert.False(File.Exists(Path.Combine(AppContext.BaseDirectory, "kullanici_tema_tercihi.json")));

                KullaniciTemaTercihi yuklenen = servis.TercihiYukle();
                Assert.Equal("Yağız Test", yuklenen.OgrenciAdi);
                Assert.Equal("🏆", yuklenen.Avatar);
                Assert.Equal("Zümrüt Koyu", yuklenen.TemaAdi);
                Assert.Equal(175, yuklenen.KazanilanEkstraXp);
                Assert.Equal(250, yuklenen.HedefSoruSayisi);
            }
            finally
            {
                servis.Temizle();
            }
        }
    }
}
