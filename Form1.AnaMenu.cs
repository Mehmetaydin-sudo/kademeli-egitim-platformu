using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using KademeliEgitimPlatformu.Models;
using KademeliEgitimPlatformu.Services;

namespace KademeliEgitimPlatformu
{
    public partial class Form1
    {
        // ==================== 1. ANA MENÜ ====================
        private void AnaMenuyuCiz()
        {
            // Çok sayıda kontrol eklenirken ara yerleşim hesaplarını engeller.
            _pnlAnaMenu.SuspendLayout();
            PanelTemizle(_pnlAnaMenu);
            _pnlAnaMenu.Dock = DockStyle.Fill;
            _pnlAnaMenu.Padding = new Padding(40, 28, 40, 28);
            _pnlAnaMenu.BackColor = Color.FromArgb(15, 23, 42);

            TableLayoutPanel anaIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6
            };
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 85F));
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 28F));
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 75F));

            Panel pnlUst = new() { Dock = DockStyle.Fill };
            Label lblBaslik = new()
            {
                Text = "Kademeli Bilişim ve Siber Güvenlik Eğitim Platformu",
                Font = FontOnbellegi.Al("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Left,
                AutoSize = true
            };

            Button btnUsbPaketYukle = new()
            {
                Text = "📥 USB Paket Yükle (.json)",
                Font = FontOnbellegi.Al("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.FromArgb(125, 211, 252),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(245, 44),
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };
            btnUsbPaketYukle.FlatAppearance.BorderColor = Color.FromArgb(56, 189, 248);
            btnUsbPaketYukle.Click += UsbPaketYukle_Click;

            Label lblCevrimdisi = new()
            {
                Text = "  ● %100 Çevrimdışı (SQLite Aktif)  ",
                Font = FontOnbellegi.Al("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 222, 128),
                BackColor = Color.FromArgb(20, 65, 40),
                Dock = DockStyle.Right,
                AutoSize = true,
                Padding = new Padding(10, 11, 10, 11)
            };

            Panel pnlBosluk = new() { Width = 15, Dock = DockStyle.Right };
            pnlUst.Controls.Add(lblBaslik);
            pnlUst.Controls.Add(lblCevrimdisi);
            pnlUst.Controls.Add(pnlBosluk);
            pnlUst.Controls.Add(btnUsbPaketYukle);

            Label lblAltBaslik = new()
            {
                Text = $"Eğitim Modülleri   •   SQLite Soru Havuzu: {SoruDeposu.SoruSayisi()} Soru   •   🔑 İpucu Jetonu: {_ipucuJetonu}",
                Font = FontOnbellegi.Al("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            };

            TableLayoutPanel modullerIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 8, 0, 8)
            };
            modullerIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            modullerIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            modullerIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            modullerIzgara.Controls.Add(ModulKartiOlustur("</>  Görsel Programlama", "Bileşen tanımları, form kontrolleri, olay (event) yönetimi ve asenkron iş parçacığı mimarisi.", _gorselBasari, Color.FromArgb(20, 184, 166), DisiplinTuru.GorselProgramlama), 0, 0);
            modullerIzgara.Controls.Add(ModulKartiOlustur("🗄️  Veri Tabanları", "Yerel SQLite mimarisi, tablo yapıları, ilişkisel sorgu tasarımı ve veri bütünlüğü.", _veriTabaniBasari, Color.FromArgb(245, 158, 11), DisiplinTuru.VeriTabanlari), 1, 0);
            modullerIzgara.Controls.Add(ModulKartiOlustur("🛡️  Siber Güvenlik", "Güvenli kodlama ilkeleri, SHA-256 özetleme (hash), saldırı senaryoları ve zafiyet analizi.", _siberBasari, Color.FromArgb(236, 72, 153), DisiplinTuru.SiberGuvenlik), 2, 0);

            Label lblZorlukBaslik = new()
            {
                Text = "Zorluk Seviyesi Seçimi ve Kademeli Geçiş Kuralları",
                Font = FontOnbellegi.Al("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            };

            TableLayoutPanel zorlukIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 8, 0, 8)
            };
            zorlukIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            zorlukIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            zorlukIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            bool ortaAcik = EnYuksekBasari() >= 75;
            zorlukIzgara.Controls.Add(ZorlukBilgiKarti(
                "Kolay Mod (Açık)",
                "✓ Her Zaman Açık",
                "Temel kavramlar ve rehberli adımlar. Başlamak için tıklayın.",
                Color.FromArgb(34, 197, 94),
                () => SinaviBaslat(null, ZorlukSeviyesi.Kolay, "Kolay Mod — Karma Değerlendirme")), 0, 0);

            zorlukIzgara.Controls.Add(ZorlukBilgiKarti(
                "Orta Mod (%75 Başarı Barajı)",
                ortaAcik ? "✓ Kilidi Açık" : "🔒 %75 Baraj",
                "Giriş için en az bir modülde %75 başarı gereklidir.",
                Color.FromArgb(245, 158, 11),
                OrtaModuAc), 1, 0);

            zorlukIzgara.Controls.Add(ZorlukBilgiKarti(
                "Zor Mod (Simülatör)",
                _simulatorOduluAlindi ? "✓ Tamamlandı" : "🛠️ Canlı Analiz",
                "Gizli syntax tuzakları, gerçek Roslyn derleyicisiyle denetlenir.",
                Color.FromArgb(139, 92, 246),
                () => EkranDegistir("SIM")), 2, 0);

            TableLayoutPanel altButonIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            altButonIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            altButonIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            Button btnTumunuCoz = new()
            {
                Text = "🚀 Karma Değerlendirme Sınavını Başlat (Tüm Modüller)",
                Font = FontOnbellegi.Al("Segoe UI", 11.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Margin = new Padding(10, 8, 10, 0),
                Cursor = Cursors.Hand
            };
            btnTumunuCoz.FlatAppearance.BorderSize = 0;
            btnTumunuCoz.Click += (s, e) => SinaviBaslat(null, null, "Karma Değerlendirme Sınavı — Tüm Disiplinler");

            Button btnSimulatorAc = new()
            {
                Text = "🛠️ Gömülü Kod Simülatörü & Çevrimdışı Hata Karnesi",
                Font = FontOnbellegi.Al("Segoe UI", 11.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(124, 58, 237),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Margin = new Padding(10, 8, 10, 0),
                Cursor = Cursors.Hand
            };
            btnSimulatorAc.FlatAppearance.BorderSize = 0;
            btnSimulatorAc.Click += (s, e) => EkranDegistir("SIM");

            altButonIzgara.Controls.Add(btnTumunuCoz, 0, 0);
            altButonIzgara.Controls.Add(btnSimulatorAc, 1, 0);

            anaIzgara.Controls.Add(pnlUst, 0, 0);
            anaIzgara.Controls.Add(lblAltBaslik, 0, 1);
            anaIzgara.Controls.Add(modullerIzgara, 0, 2);
            anaIzgara.Controls.Add(lblZorlukBaslik, 0, 3);
            anaIzgara.Controls.Add(zorlukIzgara, 0, 4);
            anaIzgara.Controls.Add(altButonIzgara, 0, 5);

            _pnlAnaMenu.Controls.Add(anaIzgara);
            _pnlAnaMenu.ResumeLayout(true);
            _anaMenuKirli = false;
        }

        private Panel ModulKartiOlustur(string baslik, string aciklama, int ilerlemeYuzdesi, Color vurguRengi, DisiplinTuru disiplin)
        {
            Panel kart = CerceveliPanelOlustur(vurguRengi, Color.FromArgb(22, 32, 50), 2);
            kart.Margin = new Padding(10);
            kart.Padding = new Padding(22);

            TableLayoutPanel icIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = Color.Transparent
            };
            icIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            icIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            icIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            icIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            icIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

            Label lblAd = new()
            {
                Text = baslik,
                Font = FontOnbellegi.Al("Segoe UI", 14.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill
            };

            Label lblAciklama = new()
            {
                Text = aciklama,
                Font = FontOnbellegi.Al("Segoe UI", 10f),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Fill
            };

            Label lblGelisim = new()
            {
                Text = $"Gelişim: %{ilerlemeYuzdesi}",
                Font = FontOnbellegi.Al("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = vurguRengi,
                Dock = DockStyle.Fill
            };

            Panel pnlOzelBar = new()
            {
                Dock = DockStyle.Top,
                Height = 10,
                BackColor = Color.FromArgb(11, 17, 32)
            };
            pnlOzelBar.Paint += (s, e) =>
            {
                int doluGenislik = (int)(pnlOzelBar.Width * (Math.Min(100, ilerlemeYuzdesi) / 100.0));
                using SolidBrush firca = new(vurguRengi);
                e.Graphics.FillRectangle(firca, 0, 0, doluGenislik, pnlOzelBar.Height);
            };

            Button btnBasla = new()
            {
                Text = ilerlemeYuzdesi > 20 ? "Devam Et →" : "Başla →",
                Font = FontOnbellegi.Al("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand
            };
            btnBasla.FlatAppearance.BorderColor = vurguRengi;
            btnBasla.FlatAppearance.BorderSize = 1;
            btnBasla.FlatAppearance.MouseOverBackColor = vurguRengi;
            btnBasla.Click += (s, e) => SinaviBaslat(disiplin, null, $"{baslik} — Kademeli Eğitim Modülü");

            icIzgara.Controls.Add(lblAd, 0, 0);
            icIzgara.Controls.Add(lblAciklama, 0, 1);
            icIzgara.Controls.Add(lblGelisim, 0, 2);
            icIzgara.Controls.Add(pnlOzelBar, 0, 3);
            icIzgara.Controls.Add(btnBasla, 0, 4);

            kart.Controls.Add(icIzgara);
            return kart;
        }

        private Panel ZorlukBilgiKarti(string baslik, string rozetMetin, string altMetin, Color renk, Action? tiklama = null)
        {
            Panel p = CerceveliPanelOlustur(renk, Color.FromArgb(22, 32, 50), 1);
            p.Margin = new Padding(10);
            p.Padding = new Padding(18);

            Label lblRozet = new()
            {
                Text = $" {rozetMetin} ",
                Font = FontOnbellegi.Al("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = renk,
                BackColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Right,
                AutoSize = true,
                Padding = new Padding(6, 4, 6, 4)
            };

            Label l1 = new()
            {
                Text = baslik,
                Font = FontOnbellegi.Al("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 32
            };

            Label l2 = new()
            {
                Text = altMetin,
                Font = FontOnbellegi.Al("Segoe UI", 10f),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Fill
            };

            if (tiklama != null)
            {
                p.Cursor = Cursors.Hand;
                l1.Cursor = Cursors.Hand;
                l2.Cursor = Cursors.Hand;
                lblRozet.Cursor = Cursors.Hand;

                p.Click += (s, e) => tiklama();
                l1.Click += (s, e) => tiklama();
                l2.Click += (s, e) => tiklama();
                lblRozet.Click += (s, e) => tiklama();
            }

            p.Controls.Add(l2);
            p.Controls.Add(l1);
            p.Controls.Add(lblRozet);
            return p;
        }

        /// <summary>Üç modül içindeki en yüksek başarı yüzdesi.</summary>
        private int EnYuksekBasari() =>
            Math.Max(_gorselBasari, Math.Max(_veriTabaniBasari, _siberBasari));

        /// <summary>Orta Mod, en az bir modülde %75 başarı gerektirir (README'deki baraj).</summary>
        private void OrtaModuAc()
        {
            int enYuksek = EnYuksekBasari();
            if (enYuksek < 75)
            {
                MessageBox.Show(
                    "Orta Mod henüz kilitli.\n\n" +
                    "Giriş için en az bir modülde %75 başarı gerekiyor.\n" +
                    $"Mevcut en yüksek başarınız: %{enYuksek}",
                    "Kilitli Mod",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            SinaviBaslat(null, ZorlukSeviyesi.Orta, "Orta Mod — Karma Değerlendirme");
        }

        private void UsbPaketYukle_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog ofd = new()
            {
                Title = "Çevrimdışı Soru Paketi Seçin (.json)",
                Filter = "JSON Soru Paketi (*.json)|*.json"
            };

            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                int eklenen = SoruDeposu.DisaridanPaketYukle(ofd.FileName);
                string atlananBilgi = SoruDeposu.SonPaketAtlananSayisi > 0
                    ? $"\nGeçersiz Olduğu İçin Atlanan: {SoruDeposu.SonPaketAtlananSayisi}"
                    : "";

                MessageBox.Show(
                    $"Paket SQLite veri tabanına aktarıldı!\n\nYeni Eklenen Soru Sayısı: {eklenen}{atlananBilgi}\nToplam Soru Havuzu: {SoruDeposu.SoruSayisi()}",
                    "Çevrimdışı Paket Yükleme Başarılı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                AnaMenuyuCiz();
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show(
                    "Seçilen paket dosyası bulunamadı. Dosya taşınmış veya silinmiş olabilir.",
                    "Dosya Bulunamadı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (InvalidDataException ex)
            {
                MessageBox.Show(
                    "Paket biçimi geçersiz. Dosya geçerli bir soru paketi değil.\n\n" + ex.Message,
                    "Paket Biçimi Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(
                    "Paket dosyasına erişim izni yok. Dosyayı farklı bir klasöre kopyalayıp tekrar deneyin.\n\n" + ex.Message,
                    "Erişim Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Paket yüklenirken beklenmeyen bir hata oluştu.\n\n" + ex.Message,
                    "Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
