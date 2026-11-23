using System;
using System.IO;
using System.Linq;
using System.Threading;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using KademeliEgitimPlatformu.Models;
using KademeliEgitimPlatformu.Services;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Xunit;

namespace KademeliEgitimPlatformu.Tests
{
    public class BelgeSoruDonusturucuTests
    {
        [Fact]
        public void KlasikTestFormati_FarkliSoruVeSikKaliplarini_DogruAyristirir()
        {
            string metin = @"
1. Aşağıdakilerden hangisi iki tabloyu ortak sütun üzerinden birleştirir?
A) INNER JOIN
B) GROUP BY
C) ORDER BY
D) DISTINCT
Doğru Cevap: A
Zorluk: Kolay
Modül: Veri Tabanları
Açıklama: JOIN ifadesi ilişkisel tablolardaki kayıtları ortak anahtar üzerinden eşleştirir.

2) Parolaları veritabanında güvenli saklamak için hangi yöntem kullanılmalıdır?
a) Düz metin
b) Tuzlu BCrypt özetleme
c) Base64 kodlama
d) Sabit tuzsuz MD5
Yanıt: b
Zorluk: Zor
Modül: Siber Güvenlik
Neden Yanlış: Tuzlu ve yavaş özetleme (BCrypt) kaba kuvvet saldırılarını zorlaştırır.

Soru 3: WinForms arayüzünün donmasını önlemek için hangi desen tercih edilir?
A. Thread.Sleep kullanmak
B. Application.DoEvents çağırmak
C. async/await ve Task.Run kullanmak
D. Timer ile sonsuz döngü kurmak
Cevap: C
";

            BelgeDonusturmeSonucu sonuc = BelgeSoruDonusturucu.MetindenSorulariAyristir(
                metin,
                varsayilanDisiplin: DisiplinTuru.GorselProgramlama);

            Assert.Equal(3, sonuc.Kayitlar.Count);
            Assert.Equal(3, sonuc.GecerliSayisi);
            Assert.Equal(0, sonuc.HataliSayisi);
            Assert.Empty(sonuc.Uyarilar);

            Soru s1 = sonuc.GecerliSorular[0];
            Assert.Equal(DisiplinTuru.VeriTabanlari, s1.Disiplin);
            Assert.Equal(ZorlukSeviyesi.Kolay, s1.Zorluk);
            Assert.Equal(0, s1.DogruSecenekIndex);
            Assert.Equal("INNER JOIN", s1.Secenekler[0]);
            Assert.Contains("JOIN ifadesi", s1.YanlisCevapAciklamalari[1]);
            Assert.False(s1.YanlisCevapAciklamalari.ContainsKey(0));

            Soru s2 = sonuc.GecerliSorular[1];
            Assert.Equal(DisiplinTuru.SiberGuvenlik, s2.Disiplin);
            Assert.Equal(ZorlukSeviyesi.Zor, s2.Zorluk);
            Assert.Equal(1, s2.DogruSecenekIndex);
            Assert.Contains("BCrypt", s2.YanlisCevapAciklamalari[0]);

            // 3. soruda Zorluk ve Modül yok -> varsayılan Orta ve varsayılan GorselProgramlama olmalı
            Soru s3 = sonuc.GecerliSorular[2];
            Assert.Equal(DisiplinTuru.GorselProgramlama, s3.Disiplin);
            Assert.Equal(ZorlukSeviyesi.Orta, s3.Zorluk);
            Assert.Equal(2, s3.DogruSecenekIndex);
        }

        [Fact]
        public void TopluCevapAnahtari_TireVeNoktaFormatlari_SorularlaEslesir()
        {
            string metinTireli = @"
1. SQL Injection saldırılarına karşı en etkili önlem nedir?
A) Metin birleştirme
B) Parametreli sorgu kullanımı
C) Hata mesajlarını gizleme
D) Tablo adlarını kısaltma

2. PostgreSQL varsayılan portu hangisidir?
A) 5432
B) 3306
C) 1433
D) 27017

Cevap Anahtarı: 1-B, 2-A
";

            BelgeDonusturmeSonucu sonucTireli = BelgeSoruDonusturucu.MetindenSorulariAyristir(
                metinTireli,
                DisiplinTuru.SiberGuvenlik);

            Assert.Equal(2, sonucTireli.GecerliSayisi);
            Assert.Equal(1, sonucTireli.GecerliSorular[0].DogruSecenekIndex);
            Assert.Equal(0, sonucTireli.GecerliSorular[1].DogruSecenekIndex);

            string metinNoktali = @"
1. C# dilinde null kontrolü yapan operatör hangisidir?
A) ??
B) &&
C) ||
D) ++

2. WinForms'ta yeniden boyutlandırma uyumu hangi özellikle sağlanır?
A) TabIndex
B) Anchor ve Dock
C) Tag
D) Opacity

1. A
2. B
";

            BelgeDonusturmeSonucu sonucNoktali = BelgeSoruDonusturucu.MetindenSorulariAyristir(
                metinNoktali,
                DisiplinTuru.GorselProgramlama);

            Assert.Equal(2, sonucNoktali.GecerliSayisi);
            Assert.Equal(0, sonucNoktali.GecerliSorular[0].DogruSecenekIndex);
            Assert.Equal(1, sonucNoktali.GecerliSorular[1].DogruSecenekIndex);
        }

        [Fact]
        public void EksikSikVeCevapYoksa_SessizceAtlamaz_SoruNoVeSebepIleUyarir()
        {
            string metin = @"
1. Geçerli soru örneği hangisidir?
A) Birinci
B) İkinci
C) Üçüncü
D) Dördüncü
Cevap: B

2. D şıkkı eksik bırakılmış soru hangisidir?
A) Seçenek 1
B) Seçenek 2
C) Seçenek 3
Cevap: A

3. Doğru cevabı yazılmamış soru hangisidir?
A) Bir
B) İki
C) Üç
D) Dört
";

            BelgeDonusturmeSonucu sonuc = BelgeSoruDonusturucu.MetindenSorulariAyristir(
                metin,
                DisiplinTuru.VeriTabanlari);

            Assert.Equal(3, sonuc.Kayitlar.Count);
            Assert.Equal(1, sonuc.GecerliSayisi);
            Assert.Equal(2, sonuc.HataliSayisi);

            BelgeSoruKaydi ikinci = sonuc.Kayitlar[1];
            Assert.Equal(2, ikinci.SoruNo);
            Assert.False(ikinci.GecerliMi);
            Assert.Contains(ikinci.Hatalar, h => h.Contains("D şıkkı"));

            BelgeSoruKaydi ucuncu = sonuc.Kayitlar[2];
            Assert.Equal(3, ucuncu.SoruNo);
            Assert.False(ucuncu.GecerliMi);
            Assert.Contains(ucuncu.Hatalar, h => h.Contains("Doğru cevap"));

            Assert.Contains(sonuc.Uyarilar, u => u.SoruNo == 2 && u.Sebep.Contains("D şıkkı"));
            Assert.Contains(sonuc.Uyarilar, u => u.SoruNo == 3 && u.Sebep.Contains("Doğru cevap"));
        }

        [Fact]
        public void TurkceKarakterler_VeAyniSatirdakiSiklar_BozulmadanAyristirilir()
        {
            string metin = @"
1. Şifreleme, güvenlik, öğretmen, çözüm, ışık ve İSTANBUL ifadeleri UTF-8 ile doğru okunuyor mu?
A) Çoğul öğe   B) Şifre özeti   C) İşlem güvenliği   D) Güncel ölçüm
Cevap: C
Açıklama: Türkçe ç, ğ, ı, İ, ö, ş, ü karakterleri kayıpsız korunmalıdır.
";

            BelgeDonusturmeSonucu sonuc = BelgeSoruDonusturucu.MetindenSorulariAyristir(
                metin,
                DisiplinTuru.SiberGuvenlik);

            Assert.Equal(1, sonuc.GecerliSayisi);
            Soru soru = sonuc.GecerliSorular[0];
            Assert.Contains("Şifreleme, güvenlik, öğretmen, çözüm, ışık ve İSTANBUL", soru.Baslik);
            Assert.Equal("Çoğul öğe", soru.Secenekler[0]);
            Assert.Equal("Şifre özeti", soru.Secenekler[1]);
            Assert.Equal("İşlem güvenliği", soru.Secenekler[2]);
            Assert.Equal("Güncel ölçüm", soru.Secenekler[3]);
            Assert.Equal(2, soru.DogruSecenekIndex);
            Assert.Contains("ç, ğ, ı, İ, ö, ş, ü", soru.YanlisCevapAciklamalari[0]);
        }

        [Fact]
        public void DocxDosyasi_ParagrafVeTabloFormatini_BirlikteOkuyupAyristirir()
        {
            string geciciKlasor = Path.Combine(Path.GetTempPath(), "kademeli-docx-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(geciciKlasor);
            string docxYolu = Path.Combine(geciciKlasor, "ornek_sorular.docx");

            try
            {
                OrnekDocxOlustur(docxYolu);

                DocxOkumaSonucu hamOkuma = BelgeSoruDonusturucu.DocxOku(docxYolu);
                Assert.Single(hamOkuma.Tablolar);
                Assert.Contains("PostgreSQL", hamOkuma.ParagrafMetni);

                BelgeDonusturmeSonucu sonuc = BelgeSoruDonusturucu.DosyadanDonustur(
                    docxYolu,
                    DisiplinTuru.VeriTabanlari);

                // Tablodan 1 geçerli + 1 eksik şıklı soru, paragraftan 1 geçerli soru -> toplam 3 kayıt, 2 geçerli, 1 hatalı
                Assert.Equal(3, sonuc.Kayitlar.Count);
                Assert.Equal(2, sonuc.GecerliSayisi);
                Assert.Equal(1, sonuc.HataliSayisi);

                Soru tabloSorusu = sonuc.GecerliSorular.First(s => s.Baslik.Contains("BCrypt"));
                Assert.Equal(ZorlukSeviyesi.Kolay, tabloSorusu.Zorluk);
                Assert.Equal(1, tabloSorusu.DogruSecenekIndex);
                Assert.Contains("Tuzlu özetleme", tabloSorusu.YanlisCevapAciklamalari[0]);

                Soru paragrafSorusu = sonuc.GecerliSorular.First(s => s.Baslik.Contains("PostgreSQL"));
                Assert.Equal(DisiplinTuru.VeriTabanlari, paragrafSorusu.Disiplin);
                Assert.Equal(0, paragrafSorusu.DogruSecenekIndex);

                Assert.Contains(sonuc.Uyarilar, u => u.Sebep.Contains("D şıkkı"));
            }
            finally
            {
                try { Directory.Delete(geciciKlasor, recursive: true); } catch { }
            }
        }

        [Fact]
        public void PdfDosyasi_SayfaSayfaTurkceMetinCikarirVeAyristirir()
        {
            string geciciKlasor = Path.Combine(Path.GetTempPath(), "kademeli-pdf-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(geciciKlasor);
            string pdfYolu = Path.Combine(geciciKlasor, "ornek_sorular.pdf");

            try
            {
                bool turkceFontKullanildi = OrnekPdfOlustur(pdfYolu);

                string metin = BelgeSoruDonusturucu.PdfdenMetinCikar(pdfYolu);
                Assert.False(string.IsNullOrWhiteSpace(metin));

                BelgeDonusturmeSonucu sonuc = BelgeSoruDonusturucu.DosyadanDonustur(
                    pdfYolu,
                    DisiplinTuru.SiberGuvenlik);

                Assert.Equal(2, sonuc.Kayitlar.Count);
                Assert.Equal(2, sonuc.GecerliSayisi);
                Assert.Equal(0, sonuc.HataliSayisi);
                Assert.Equal(1, sonuc.GecerliSorular[0].DogruSecenekIndex);
                Assert.Equal(2, sonuc.GecerliSorular[1].DogruSecenekIndex);

                if (turkceFontKullanildi)
                {
                    Assert.Contains("Şifreleme", sonuc.GecerliSorular[0].Baslik);
                    Assert.Contains("Güvenlik", sonuc.GecerliSorular[0].Secenekler[1]);
                }
            }
            finally
            {
                try { Directory.Delete(geciciKlasor, recursive: true); } catch { }
            }
        }

        [Fact]
        public void BelgeOnizlemeFormu_DocxIleOlusturulabilir()
        {
            string geciciKlasor = Path.Combine(Path.GetTempPath(), "kademeli-ui-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(geciciKlasor);
            string docxYolu = Path.Combine(geciciKlasor, "onizleme.docx");
            OrnekDocxOlustur(docxYolu);

            Exception? hata = null;
            string? baslik = null;

            try
            {
                var isParcacigi = new Thread(() =>
                {
                    try
                    {
                        using var form = new BelgeOnizlemeFormu(docxYolu, DisiplinTuru.SiberGuvenlik);
                        baslik = form.Text;
                    }
                    catch (Exception ex)
                    {
                        hata = ex;
                    }
                })
                {
                    IsBackground = true
                };
                isParcacigi.SetApartmentState(ApartmentState.STA);
                isParcacigi.Start();
                Assert.True(isParcacigi.Join(TimeSpan.FromSeconds(30)));

                Assert.Null(hata);
                Assert.NotNull(baslik);
                Assert.Contains("onizleme.docx", baslik);
            }
            finally
            {
                try { Directory.Delete(geciciKlasor, recursive: true); } catch { }
            }
        }

        private static void OrnekDocxOlustur(string dosyaYolu)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Create(dosyaYolu, WordprocessingDocumentType.Document);
            MainDocumentPart mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            Body body = mainPart.Document.Body!;

            // 1) Tablo formatında sorular (Soru | A | B | C | D | Cevap | Zorluk | Açıklama)
            var tablo = new Table();
            tablo.AppendChild(TabloSatiri("Soru", "A", "B", "C", "D", "Cevap", "Zorluk", "Açıklama"));
            tablo.AppendChild(TabloSatiri(
                "BCrypt algoritmasının temel avantajı nedir?",
                "Şifreyi geri çözülebilir saklaması",
                "Rastgele tuz ve ayarlanabilir maliyetle kaba kuvveti yavaşlatması",
                "Metni sıkıştırması",
                "Sadece sayısal değer üretmesi",
                "B",
                "Kolay",
                "Tuzlu özetleme gökkuşağı tablolarına karşı koruma sağlar."));
            tablo.AppendChild(TabloSatiri(
                "Eksik şıklı tablo sorusu?",
                "Şık A",
                "Şık B",
                "Şık C",
                "",
                "A",
                "Orta",
                ""));
            body.AppendChild(tablo);

            // 2) Paragraf formatında soru (Türkçe karakterli)
            string[] paragraflar =
            {
                "1. PostgreSQL'de bağlantı sızıntısını önlemek için hangi C# bloğu kullanılmalıdır?",
                "A) using bloğu",
                "B) goto deyimi",
                "C) sleep çağrısı",
                "D) global değişken",
                "Doğru Cevap: A",
                "Zorluk: Orta",
                "Açıklama: using bloğu iş bitiminde bağlantıyı güvenli biçimde kapatır."
            };

            foreach (string satir in paragraflar)
            {
                body.AppendChild(new Paragraph(new Run(new Text(satir))));
            }

            mainPart.Document.Save();
        }

        private static TableRow TabloSatiri(params string[] hucreler)
        {
            var satir = new TableRow();
            foreach (string metin in hucreler)
            {
                satir.AppendChild(new TableCell(new Paragraph(new Run(new Text(metin)))));
            }
            return satir;
        }

        private static bool OrnekPdfOlustur(string dosyaYolu)
        {
            var builder = new PdfDocumentBuilder();
            string arialYolu = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
                "arial.ttf");

            PdfDocumentBuilder.AddedFont font;
            bool turkceFont = false;

            if (File.Exists(arialYolu))
            {
                font = builder.AddTrueTypeFont(File.ReadAllBytes(arialYolu));
                turkceFont = true;
            }
            else
            {
                font = builder.AddStandard14Font(Standard14Font.Helvetica);
            }

            // Sayfa 1
            var sayfa1 = builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4);
            string[] sayfa1Satirlari = turkceFont
                ? new[]
                {
                    "1. Şifreleme ve özetleme arasındaki temel fark nedir?",
                    "A) İkisi de tamamen aynıdır",
                    "B) Güvenlik için özetleme tek yönlüdür, şifreleme çift yönlüdür",
                    "C) Şifreleme yalnızca sayılarla çalışır",
                    "D) Özetleme anahtar gerektirmez ve çözülebilir"
                }
                : new[]
                {
                    "1. What is the difference between encryption and hashing?",
                    "A) Both are identical",
                    "B) Hashing is one-way while encryption is two-way",
                    "C) Encryption only uses numbers",
                    "D) Hashing is reversible"
                };

            double y = 760;
            foreach (string s in sayfa1Satirlari)
            {
                sayfa1.AddText(s, 11, new PdfPoint(50, y), font);
                y -= 22;
            }

            // Sayfa 2 (2. soru + toplu cevap anahtarı)
            var sayfa2 = builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4);
            string[] sayfa2Satirlari = turkceFont
                ? new[]
                {
                    "2. Aşağıdakilerden hangisi ilişkisel veritabanı yönetim sistemidir?",
                    "A) HTML",
                    "B) CSS",
                    "C) PostgreSQL",
                    "D) PNG",
                    "Cevap Anahtarı: 1-B, 2-C"
                }
                : new[]
                {
                    "2. Which of the following is an RDBMS?",
                    "A) HTML",
                    "B) CSS",
                    "C) PostgreSQL",
                    "D) PNG",
                    "Cevap Anahtari: 1-B, 2-C"
                };

            y = 760;
            foreach (string s in sayfa2Satirlari)
            {
                sayfa2.AddText(s, 11, new PdfPoint(50, y), font);
                y -= 22;
            }

            File.WriteAllBytes(dosyaYolu, builder.Build());
            return turkceFont;
        }
    }
}
