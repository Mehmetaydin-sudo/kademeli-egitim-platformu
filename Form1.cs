using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using KademeliEgitimPlatformu.Models;
using KademeliEgitimPlatformu.Services;

namespace KademeliEgitimPlatformu
{
    public partial class Form1 : Form
    {
        private readonly SoruServisi _soruServisi = new();
        private List<Soru> _aktifSorular = new();
        private int _mevcutSoruIndex = 0;
        private int _dogruSayisi = 0;

        // Arayüz Panelleri
        private Panel _pnlAnaMenu = new();
        private Panel _pnlSinavEkrani = new();

        // Sınav Ekranı Bileşenleri
        private Label _lblDisiplinBaslik = new();
        private Label _lblSoruSayaci = new();
        private Label _lblSoruMetni = new();
        private RichTextBox _txtKodBlogu = new();
        private Button[] _secenekButonlari = new Button[4];
        private Panel _pnlNedenYanlis = new();
        private Label _lblNedenYanlisBaslik = new();
        private Label _lblNedenYanlisAciklama = new();
        private Button _btnSonrakiSoru = new();

        public Form1()
        {
            InitializeComponent();
            ArayuzuOlustur();
        }

        private void ArayuzuOlustur()
        {
            // Ana Pencere Ayarları (Slayttaki Koyu Lacivert Tema)
            this.Text = "Kademeli Bilişim ve Siber Güvenlik Eğitim Platformu (%100 Çevrimdışı)";
            this.Size = new Size(1050, 760);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(15, 23, 42); // Koyu Lacivert
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            AnaMenuyuCiz();
            SinavEkraniniCiz();

            this.Controls.Add(_pnlAnaMenu);
            this.Controls.Add(_pnlSinavEkrani);
            _pnlSinavEkrani.Visible = false;
        }

        private void AnaMenuyuCiz()
        {
            _pnlAnaMenu.Dock = DockStyle.Fill;
            _pnlAnaMenu.BackColor = Color.FromArgb(15, 23, 42);

            // Üst Başlık
            Label lblBaslik = new()
            {
                Text = "Kademeli Bilişim ve Siber Güvenlik\nEğitim Platformu",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(40, 30),
                AutoSize = true
            };

            // Çevrimdışı Rozeti (Slayt 13)
            Label lblCevrimdisi = new()
            {
                Text = "  ● %100 Çevrimdışı (Yerel Veri Tabanı Aktif)  ",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 222, 128),
                BackColor = Color.FromArgb(20, 83, 45),
                Location = new Point(680, 45),
                AutoSize = true,
                Padding = new Padding(8)
            };

            Label lblAltBaslik = new()
            {
                Text = "Eğitim Modülleri — Başlamak istediğiniz disiplini seçin:",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(40, 130),
                AutoSize = true
            };

            // 3 Çekirdek Disiplin Kartı (Slayt 13)
            Panel kartGorsel = ModulKartiOlustur("Görsel Programlama", "C# Form, Olay (Event) ve Asenkron Mimari", "%65", Color.FromArgb(20, 184, 166), 40, 180, DisiplinTuru.GorselProgramlama);
            Panel kartVeriTabani = ModulKartiOlustur("Veri Tabanları", "SQLite, İlişkisel Tablolar ve Sorgu Tasarımı", "%40", Color.FromArgb(245, 158, 11), 365, 180, DisiplinTuru.VeriTabanlari);
            Panel kartSiber = ModulKartiOlustur("Siber Güvenlik", "Güvenli Kodlama, Hashleme ve Zafiyet Analizi", "%10", Color.FromArgb(236, 72, 153), 690, 180, DisiplinTuru.SiberGuvenlik);

            // Zorluk Seviyesi Bilgi Alanı (Slayt 13 Alt Kısım)
            Label lblZorlukBaslik = new()
            {
                Text = "Kademeli Zorluk Sistemi ve Baraj Kuralları",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(40, 440),
                AutoSize = true
            };

            Panel pnlKolayBilgi = ZorlukBilgiKarti("✓ Kolay Mod (Açık)", "Temel kavramlar ve rehberli adımlar.", Color.FromArgb(34, 197, 94), 40, 485);
            Panel pnlOrtaBilgi = ZorlukBilgiKarti("🔒 Orta Mod (%75 Başarı Barajı)", "Sistem tepkisi ve modüler kod analizi.", Color.FromArgb(245, 158, 11), 365, 485);
            Panel pnlZorBilgi = ZorlukBilgiKarti("🔒 Zor Mod (Kilitli)", "Gizli syntax tuzakları ve hata avcılığı.", Color.FromArgb(100, 116, 139), 690, 485);

            // Tüm Soruları Karışık Çöz Butonu
            Button btnTumunuCoz = new()
            {
                Text = "🚀 Tüm Modüllerden Karma Demo Sınavını Başlat",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(950, 55),
                Location = new Point(40, 610),
                Cursor = Cursors.Hand
            };
            btnTumunuCoz.FlatAppearance.BorderSize = 0;
            btnTumunuCoz.Click += (s, e) => SinaviBaslat(null, "Karma Değerlendirme Sınavı — Tüm Disiplinler");

            _pnlAnaMenu.Controls.AddRange(new Control[] { lblBaslik, lblCevrimdisi, lblAltBaslik, kartGorsel, kartVeriTabani, kartSiber, lblZorlukBaslik, pnlKolayBilgi, pnlOrtaBilgi, pnlZorBilgi, btnTumunuCoz });
        }

        private Panel ModulKartiOlustur(string baslik, string aciklama, string ilerlemeYuzdesi, Color vurguRengi, int x, int y, DisiplinTuru disiplin)
        {
            Panel kart = new()
            {
                Size = new Size(300, 220),
                Location = new Point(x, y),
                BackColor = Color.FromArgb(30, 41, 59)
            };

            Label lblAd = new()
            {
                Text = baslik,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = vurguRengi,
                Location = new Point(20, 20),
                AutoSize = true
            };

            Label lblAciklama = new()
            {
                Text = aciklama,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(203, 213, 225),
                Location = new Point(20, 55),
                Size = new Size(260, 45)
            };

            Label lblGelisim = new()
            {
                Text = $"Gelişim Durumu: {ilerlemeYuzdesi}",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(20, 110),
                AutoSize = true
            };

            Button btnBasla = new()
            {
                Text = "Modülü Başlat →",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = vurguRengi,
                ForeColor = Color.FromArgb(15, 23, 42),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(260, 42),
                Location = new Point(20, 155),
                Cursor = Cursors.Hand
            };
            btnBasla.FlatAppearance.BorderSize = 0;
            btnBasla.Click += (s, e) => SinaviBaslat(disiplin, $"{baslik} — Kademeli Eğitim Modülü");

            kart.Controls.AddRange(new Control[] { lblAd, lblAciklama, lblGelisim, btnBasla });
            return kart;
        }

        private Panel ZorlukBilgiKarti(string baslik, string altMetin, Color renk, int x, int y)
        {
            Panel p = new()
            {
                Size = new Size(300, 95),
                Location = new Point(x, y),
                BackColor = Color.FromArgb(30, 41, 59)
            };

            Label l1 = new()
            {
                Text = baslik,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = renk,
                Location = new Point(15, 15),
                AutoSize = true
            };

            Label l2 = new()
            {
                Text = altMetin,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(203, 213, 225),
                Location = new Point(15, 45),
                Size = new Size(270, 40)
            };

            p.Controls.AddRange(new Control[] { l1, l2 });
            return p;
        }

        private void SinavEkraniniCiz()
        {
            _pnlSinavEkrani.Dock = DockStyle.Fill;
            _pnlSinavEkrani.BackColor = Color.FromArgb(15, 23, 42);

            // Üst Bar
            _lblDisiplinBaslik = new()
            {
                Text = "Görsel Programlama — Orta Mod",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(40, 20),
                AutoSize = true
            };

            _lblSoruSayaci = new()
            {
                Text = "Soru 1 / 3",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Location = new Point(40, 52),
                AutoSize = true
            };

            Button btnAnaMenuyeDon = new()
            {
                Text = "✕ Ana Menüye Dön",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(160, 36),
                Location = new Point(830, 22),
                Cursor = Cursors.Hand
            };
            btnAnaMenuyeDon.FlatAppearance.BorderSize = 0;
            btnAnaMenuyeDon.Click += (s, e) =>
            {
                _pnlSinavEkrani.Visible = false;
                _pnlAnaMenu.Visible = true;
            };

            // Soru Metni
            _lblSoruMetni = new()
            {
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(241, 245, 249),
                Location = new Point(40, 85),
                Size = new Size(950, 50)
            };

            // Kod Bloğu (Slayt 14'teki Kod Kutusu)
            _txtKodBlogu = new()
            {
                Font = new Font("Consolas", 11f),
                BackColor = Color.FromArgb(9, 13, 22),
                ForeColor = Color.FromArgb(125, 211, 252),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Location = new Point(40, 140),
                Size = new Size(950, 135)
            };

            // 4 Şık Butonu (A, B, C, D)
            string[] harfler = { "A", "B", "C", "D" };
            for (int i = 0; i < 4; i++)
            {
                int secenekIndex = i;
                Button btn = new()
                {
                    Font = new Font("Segoe UI", 10.5f),
                    BackColor = Color.FromArgb(30, 41, 59),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(15, 0, 0, 0),
                    Size = new Size(950, 46),
                    Location = new Point(40, 290 + (i * 54)),
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);
                btn.FlatAppearance.BorderSize = 1;
                btn.Click += (s, e) => SecenekSecildi(secenekIndex);

                _secenekButonlari[i] = btn;
                _pnlSinavEkrani.Controls.Add(btn);
            }

            // "Neden Yanlış Açıklaması" Paneli (Slayt 14 Alt Kutu)
            _pnlNedenYanlis = new()
            {
                Location = new Point(40, 515),
                Size = new Size(950, 130),
                BackColor = Color.FromArgb(43, 27, 23),
                Visible = false
            };

            _lblNedenYanlisBaslik = new()
            {
                Text = "Neden Yanlış Açıklaması",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(251, 146, 60),
                Location = new Point(15, 12),
                AutoSize = true
            };

            _lblNedenYanlisAciklama = new()
            {
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(254, 215, 170),
                Location = new Point(15, 40),
                Size = new Size(920, 80)
            };

            _pnlNedenYanlis.Controls.AddRange(new Control[] { _lblNedenYanlisBaslik, _lblNedenYanlisAciklama });

            // Sonraki Soru Butonu
            _btnSonrakiSoru = new()
            {
                Text = "Sonraki Soru →",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(180, 42),
                Location = new Point(810, 660),
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnSonrakiSoru.FlatAppearance.BorderSize = 0;
            _btnSonrakiSoru.Click += SonrakiSoru_Click;

            _pnlSinavEkrani.Controls.AddRange(new Control[]
            {
                _lblDisiplinBaslik, _lblSoruSayaci, btnAnaMenuyeDon,
                _lblSoruMetni, _txtKodBlogu, _pnlNedenYanlis, _btnSonrakiSoru
            });
        }

        private void SinaviBaslat(DisiplinTuru? disiplin, string baslik)
        {
            _aktifSorular = disiplin.HasValue
                ? _soruServisi.SorulariFiltrele(disiplin.Value)
                : _soruServisi.TumSorulariGetir();

            if (_aktifSorular.Count == 0)
            {
                MessageBox.Show("Bu modülde henüz soru bulunamadı! Data/Sorular.json dosyasını kontrol edin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _mevcutSoruIndex = 0;
            _dogruSayisi = 0;
            _lblDisiplinBaslik.Text = baslik;

            _pnlAnaMenu.Visible = false;
            _pnlSinavEkrani.Visible = true;

            SoruyuEkranaYukle();
        }

        private void SoruyuEkranaYukle()
        {
            Soru soru = _aktifSorular[_mevcutSoruIndex];
            _lblSoruSayaci.Text = $"Soru {_mevcutSoruIndex + 1} / {_aktifSorular.Count}  |  Zorluk: {soru.Zorluk}";
            _lblSoruMetni.Text = soru.Baslik;
            _txtKodBlogu.Text = soru.KodBlogu ?? "// Bu soru için kod bloğu bulunmamaktadır.";

            string[] harfler = { "A", "B", "C", "D" };
            for (int i = 0; i < 4; i++)
            {
                _secenekButonlari[i].Text = $"{harfler[i]})  {soru.Secenekler[i]}";
                _secenekButonlari[i].BackColor = Color.FromArgb(30, 41, 59);
                _secenekButonlari[i].FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);
                _secenekButonlari[i].Enabled = true;
            }

            _pnlNedenYanlis.Visible = false;
            _btnSonrakiSoru.Visible = false;
        }

        private void SecenekSecildi(int secilenIndex)
        {
            Soru soru = _aktifSorular[_mevcutSoruIndex];

            // Şıkları kilitle
            foreach (var btn in _secenekButonlari)
                btn.Enabled = false;

            // Doğru şıkkı her durumda yeşil yak (Slayt 14)
            _secenekButonlari[soru.DogruSecenekIndex].BackColor = Color.FromArgb(20, 83, 45);
            _secenekButonlari[soru.DogruSecenekIndex].FlatAppearance.BorderColor = Color.FromArgb(34, 197, 94);

            if (secilenIndex == soru.DogruSecenekIndex)
            {
                _dogruSayisi++;
                _pnlNedenYanlis.BackColor = Color.FromArgb(20, 50, 35);
                _lblNedenYanlisBaslik.Text = "✓ Doğru Cevap!";
                _lblNedenYanlisBaslik.ForeColor = Color.FromArgb(74, 222, 128);
                _lblNedenYanlisAciklama.Text = "Tebrikler! Konunun temel mantığını doğru kavradınız. Sonraki soruya geçebilirsiniz.";
                _lblNedenYanlisAciklama.ForeColor = Color.FromArgb(187, 247, 208);
            }
            else
            {
                // Yanlış seçilen şıkkı kırmızı yak (Slayt 14)
                _secenekButonlari[secilenIndex].BackColor = Color.FromArgb(69, 26, 26);
                _secenekButonlari[secilenIndex].FlatAppearance.BorderColor = Color.FromArgb(239, 68, 68);

                _pnlNedenYanlis.BackColor = Color.FromArgb(43, 27, 23);
                _lblNedenYanlisBaslik.Text = "✕ Neden Yanlış Açıklaması";
                _lblNedenYanlisBaslik.ForeColor = Color.FromArgb(251, 146, 60);
                _lblNedenYanlisAciklama.ForeColor = Color.FromArgb(254, 215, 170);

                if (soru.YanlisCevapAciklamalari.TryGetValue(secilenIndex, out string? aciklama))
                    _lblNedenYanlisAciklama.Text = aciklama;
                else
                    _lblNedenYanlisAciklama.Text = "Seçtiğiniz şık bu senaryo için uygun değildir. Yeşil işaretli doğru cevabı inceleyiniz.";
            }

            _pnlNedenYanlis.Visible = true;
            _btnSonrakiSoru.Visible = true;
        }

        private void SonrakiSoru_Click(object? sender, EventArgs e)
        {
            _mevcutSoruIndex++;
            if (_mevcutSoruIndex < _aktifSorular.Count)
            {
                SoruyuEkranaYukle();
            }
            else
            {
                double basariOrani = (double)_dogruSayisi / _aktifSorular.Count * 100;
                string barajMesaji = basariOrani >= 75
                    ? "🎉 TEBRİKLER! %75 Başarı Barajını geçtiniz, bir üst modülün kilidi açıldı!"
                    : "⚠️ %75 Başarı Barajının altında kaldınız. Orta moda geçmek için yanlış açıklamalarını tekrar gözden geçirin.";

                MessageBox.Show(
                    $"Sınav Tamamlandı!\n\nDoğru Sayısı: {_dogruSayisi} / {_aktifSorular.Count}\nBaşarı Yüzdesi: %{basariOrani:0}\n\n{barajMesaji}",
                    "Çevrimdışı Hata Karnesi ve Sonuç",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                _pnlSinavEkrani.Visible = false;
                _pnlAnaMenu.Visible = true;
            }
        }
    }
}