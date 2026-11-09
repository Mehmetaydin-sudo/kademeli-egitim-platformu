using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using KademeliEgitimPlatformu.Models;
using KademeliEgitimPlatformu.Services;

namespace KademeliEgitimPlatformu
{
    public partial class Form1 : Form
    {
        private const string Surum = "v0.4.2";

        private readonly SoruServisi _soruServisi = new();
        private readonly IlerlemeServisi _ilerlemeServisi = new();

        private List<Soru> _aktifSorular = new();
        private List<bool> _cevaplar = new();
        private int _mevcutSoruIndex = 0;
        private int _dogruSayisi = 0;
        private int _gecenSaniye = 0;
        private System.Windows.Forms.Timer _sinavTimer = new();

        // SQLite (ilerleme.db) üzerinden yüklenen kalıcı kullanıcı verileri
        // Yeni kullanıcı, kazanmadığı bir başarı yüzdesiyle başlamaz (hepsi 0).
        private int _ipucuJetonu = 3;
        private int _gorselBasari = 0;
        private int _veriTabaniBasari = 0;
        private int _siberBasari = 0;
        private bool _simulatorOduluAlindi = false;

        // Ana Yerleşim: Sol Sidebar + Sağ İçerik Alanı
        private Panel _pnlSidebar = new();
        private Panel _pnlIcerikAlani = new();
        private Button _btnMenuAna = new();
        private Button _btnMenuSinav = new();
        private Button _btnMenuSimulator = new();

        // 3 Ana Ekran Paneli
        private Panel _pnlAnaMenu = new();
        private Panel _pnlSinavEkrani = new();
        private Panel _pnlSimulatorVeKarne = new();

        // Sınav Ekranı Bileşenleri
        private Label _lblDisiplinBaslik = new();
        private Label _lblSoruSayaci = new();
        private Label _lblSureSayaci = new();
        private Label _lblJetonSayaci = new();
        private Label _lblSoruMetni = new();
        private RichTextBox _txtKodBlogu = new();
        private RichTextBox _lblKodSatirNo = new();
        private Button[] _secenekButonlari = new Button[4];
        private Panel _pnlNedenYanlis = new();
        private Label _lblNedenYanlisBaslik = new();
        private Label _lblNedenYanlisAciklama = new();
        private Button _btnSonrakiSoru = new();
        private Button _btnIpucuKullan = new();

        // Simülatör Bileşenleri
        private RichTextBox _txtSimulatorKod = new();
        private Label _lblSimulatorSonuc = new();

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;

            // Açılışta SQLite ilerleme.db'den kayıtlı verileri yükle
            KaliciIlerlemeyiYukle();

            _sinavTimer.Interval = 1000;
            _sinavTimer.Tick += (s, e) =>
            {
                _gecenSaniye++;
                int dk = _gecenSaniye / 60;
                int sn = _gecenSaniye % 60;
                _lblSureSayaci.Text = $"⏱️ {dk:00}:{sn:00}   ";
            };
            ArayuzuOlustur();
        }

        private void KaliciIlerlemeyiYukle()
        {
            try
            {
                IlerlemeDurumu durum = _ilerlemeServisi.Yukle();
                _gorselBasari = durum.GorselBasari;
                _veriTabaniBasari = durum.VeriTabaniBasari;
                _siberBasari = durum.SiberBasari;
                _ipucuJetonu = durum.IpucuJetonu;
                _simulatorOduluAlindi = durum.HataAvcisiKazanildi;
            }
            catch (Exception ex)
            {
                // Bozuk kilitli veritabanı uygulamayı açılmaz hale getirmesin.
                MessageBox.Show(
                    "Kayıtlı ilerleme okunamadı, varsayılan değerlerle başlanıyor.\n\n" + ex.Message,
                    "İlerleme Yüklenemedi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void KaliciIlerlemeyiKaydet()
        {
            try
            {
                _ilerlemeServisi.Kaydet(new IlerlemeDurumu(
                    _gorselBasari,
                    _veriTabaniBasari,
                    _siberBasari,
                    _ipucuJetonu,
                    _simulatorOduluAlindi));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "İlerleme kaydedilemedi. Veri klasörünün yazılabilir olduğundan emin olun.\n\n" + ex.Message,
                    "Kayıt Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _sinavTimer.Stop();
            _sinavTimer.Dispose();
            base.OnFormClosed(e);
        }

        private static void PanelTemizle(Control kapsayici)
        {
            while (kapsayici.Controls.Count > 0)
            {
                Control c = kapsayici.Controls[0];
                kapsayici.Controls.RemoveAt(0);
                c.Dispose();
            }
        }

        private void ArayuzuOlustur()
        {
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.Text = $"Kademeli Bilişim ve Siber Güvenlik Eğitim Platformu — {Surum} (SQLite Kalıcı Mimari)";
            this.Size = new Size(1600, 950);
            this.MinimumSize = new Size(1280, 800);
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(11, 17, 32);
            this.ForeColor = Color.White;

            SidebarOlustur();

            _pnlIcerikAlani.Dock = DockStyle.Fill;
            _pnlIcerikAlani.BackColor = Color.FromArgb(15, 23, 42);

            SinavEkraniniCiz();
            SimulatorEkraniniCiz();

            _pnlIcerikAlani.Controls.Add(_pnlAnaMenu);
            _pnlIcerikAlani.Controls.Add(_pnlSinavEkrani);
            _pnlIcerikAlani.Controls.Add(_pnlSimulatorVeKarne);

            this.Controls.Add(_pnlIcerikAlani);
            this.Controls.Add(_pnlSidebar);

            EkranDegistir("ANA");
        }

        // ==================== SOL YAN NAVİGASYON (SIDEBAR) ====================
        private void SidebarOlustur()
        {
            _pnlSidebar.Dock = DockStyle.Left;
            _pnlSidebar.Width = 235;
            _pnlSidebar.BackColor = Color.FromArgb(9, 13, 24);
            _pnlSidebar.Padding = new Padding(12, 25, 12, 20);
            _pnlSidebar.Paint += (s, e) =>
            {
                using Pen cizgi = new(Color.FromArgb(30, 41, 59), 1);
                e.Graphics.DrawLine(cizgi, _pnlSidebar.Width - 1, 0, _pnlSidebar.Width - 1, _pnlSidebar.Height);
            };

            Label lblLogo = new()
            {
                Text = "⚡ EDUSEC\n    SQLITE MİMARİ",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Dock = DockStyle.Top,
                Height = 70
            };

            _btnMenuAna = SidebarButonu("🏠  Kontrol Paneli", () => EkranDegistir("ANA"));
            _btnMenuSinav = SidebarButonu("📝  Karma Sınav", () => SinaviBaslat(null, null, "Karma Değerlendirme Sınavı — Tüm Disiplinler"));
            _btnMenuSimulator = SidebarButonu("🛠️  Kod Simülatörü", () => EkranDegistir("SIM"));

            Label lblAltBilgi = new()
            {
                Text = $"{Surum} • %100 Çevrimdışı\nsorular.db & ilerleme.db\n\nMehmet • Ünal\nİsmail Can • Yağız",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Bottom,
                Height = 95
            };

            _pnlSidebar.Controls.Add(_btnMenuSimulator);
            _pnlSidebar.Controls.Add(_btnMenuSinav);
            _pnlSidebar.Controls.Add(_btnMenuAna);
            _pnlSidebar.Controls.Add(lblLogo);
            _pnlSidebar.Controls.Add(lblAltBilgi);
        }

        private Button SidebarButonu(string metin, Action tiklamaOlayi)
        {
            Button btn = new()
            {
                Text = metin,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(203, 213, 225),
                BackColor = Color.FromArgb(9, 13, 24),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                Dock = DockStyle.Top,
                Height = 52,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 41, 59);
            btn.Click += (s, e) => tiklamaOlayi();
            return btn;
        }

        private void EkranDegistir(string ekranKodu)
        {
            _pnlAnaMenu.Visible = ekranKodu == "ANA";
            _pnlSinavEkrani.Visible = ekranKodu == "SINAV";
            _pnlSimulatorVeKarne.Visible = ekranKodu == "SIM";

            if (ekranKodu != "SINAV") _sinavTimer.Stop();
            if (ekranKodu == "ANA") AnaMenuyuCiz();
            if (ekranKodu == "SIM") SimulatorEkraniniCiz();

            _btnMenuAna.BackColor = ekranKodu == "ANA" ? Color.FromArgb(30, 41, 59) : Color.FromArgb(9, 13, 24);
            _btnMenuAna.ForeColor = ekranKodu == "ANA" ? Color.FromArgb(56, 189, 248) : Color.FromArgb(203, 213, 225);

            _btnMenuSinav.BackColor = ekranKodu == "SINAV" ? Color.FromArgb(30, 41, 59) : Color.FromArgb(9, 13, 24);
            _btnMenuSinav.ForeColor = ekranKodu == "SINAV" ? Color.FromArgb(56, 189, 248) : Color.FromArgb(203, 213, 225);

            _btnMenuSimulator.BackColor = ekranKodu == "SIM" ? Color.FromArgb(30, 41, 59) : Color.FromArgb(9, 13, 24);
            _btnMenuSimulator.ForeColor = ekranKodu == "SIM" ? Color.FromArgb(56, 189, 248) : Color.FromArgb(203, 213, 225);
        }

        private Panel CerceveliPanelOlustur(Color cerceveRengi, Color arkaPlan, int kalinlik = 1)
        {
            Panel p = new()
            {
                Dock = DockStyle.Fill,
                BackColor = arkaPlan
            };
            p.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using Pen kalem = new(cerceveRengi, kalinlik);
                e.Graphics.DrawRectangle(kalem, 1, 1, p.Width - 3, p.Height - 3);
            };
            p.Resize += (s, e) => p.Invalidate();
            return p;
        }

        // ==================== 1. ANA MENÜ ====================
        private void AnaMenuyuCiz()
        {
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
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Left,
                AutoSize = true
            };

            Button btnUsbPaketYukle = new()
            {
                Text = "📥 USB Paket Yükle (.json)",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
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
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
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
                Text = $"Eğitim Modülleri   •   SQLite Soru Havuzu: {_soruServisi.TumSorulariGetir().Count} Soru   •   🔑 İpucu Jetonu: {_ipucuJetonu}",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
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
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
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
                "İlk bakışta gizlenen syntax tuzakları. Simülatöre gitmek için tıklayın.",
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
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
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
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
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
                Font = new Font("Segoe UI", 14.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill
            };

            Label lblAciklama = new()
            {
                Text = aciklama,
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Fill
            };

            Label lblGelisim = new()
            {
                Text = $"Gelişim: %{ilerlemeYuzdesi}",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
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
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
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
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = renk,
                BackColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Right,
                AutoSize = true,
                Padding = new Padding(6, 4, 6, 4)
            };

            Label l1 = new()
            {
                Text = baslik,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 32
            };

            Label l2 = new()
            {
                Text = altMetin,
                Font = new Font("Segoe UI", 10f),
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

        // ==================== 2. SINAV EKRANI ====================
        private void SinavEkraniniCiz()
        {
            PanelTemizle(_pnlSinavEkrani);
            _pnlSinavEkrani.Dock = DockStyle.Fill;
            _pnlSinavEkrani.Padding = new Padding(45, 25, 45, 25);
            _pnlSinavEkrani.BackColor = Color.FromArgb(15, 23, 42);

            TableLayoutPanel sinavIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6
            };
            sinavIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            sinavIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 68F));
            sinavIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));
            sinavIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));
            sinavIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            sinavIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));

            Panel pnlUst = new() { Dock = DockStyle.Fill };
            _lblDisiplinBaslik = new()
            {
                Text = "Görsel Programlama — Orta Mod",
                Font = new Font("Segoe UI", 17, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                AutoSize = true
            };

            _lblSoruSayaci = new()
            {
                Text = "Soru 1 / 3",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Dock = DockStyle.Bottom,
                AutoSize = true
            };

            Button btnAnaMenuyeDon = new()
            {
                Text = "✕ Kapat",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 40),
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };
            btnAnaMenuyeDon.FlatAppearance.BorderSize = 0;
            btnAnaMenuyeDon.Click += (s, e) => EkranDegistir("ANA");

            _lblSureSayaci = new()
            {
                Text = "⏱️ 00:00   ",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = true
            };

            _lblJetonSayaci = new()
            {
                Text = $"🔑 İpucu Jetonu: {_ipucuJetonu}   ",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = true
            };

            pnlUst.Controls.Add(_lblDisiplinBaslik);
            pnlUst.Controls.Add(_lblSoruSayaci);
            pnlUst.Controls.Add(_lblJetonSayaci);
            pnlUst.Controls.Add(_lblSureSayaci);
            pnlUst.Controls.Add(btnAnaMenuyeDon);

            _lblSoruMetni = new()
            {
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(241, 245, 249),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            Panel pnlKodKapsayici = CerceveliPanelOlustur(Color.FromArgb(51, 65, 85), Color.FromArgb(9, 13, 22), 1);
            pnlKodKapsayici.Margin = new Padding(0, 4, 0, 10);

            Panel pnlKodUstBar = new()
            {
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = Color.FromArgb(19, 27, 46),
                Padding = new Padding(12, 4, 0, 0)
            };
            Label lblKodDosyaAdi = new()
            {
                Text = "🔴 🟡 🟢    OrnekKodBlogu.cs — Salt Okunur İnceleme Modu",
                Font = new Font("Consolas", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true
            };
            pnlKodUstBar.Controls.Add(lblKodDosyaAdi);

            _lblKodSatirNo = new RichTextBox
            {
                Text = "1\n2\n3\n4\n5\n6\n7\n8",
                Font = new Font("Consolas", 12.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                BackColor = Color.FromArgb(13, 18, 30),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.None,
                Width = 46,
                Dock = DockStyle.Left,
                TabStop = false
            };

            _txtKodBlogu = new()
            {
                Font = new Font("Consolas", 12.5f),
                BackColor = Color.FromArgb(9, 13, 22),
                ForeColor = Color.FromArgb(125, 211, 252),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                Dock = DockStyle.Fill
            };
            _txtKodBlogu.VScroll += (s, e) => KodSatirlariniHizala();

            pnlKodKapsayici.Controls.Add(_txtKodBlogu);
            pnlKodKapsayici.Controls.Add(_lblKodSatirNo);
            pnlKodKapsayici.Controls.Add(pnlKodUstBar);

            TableLayoutPanel siklarIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Margin = new Padding(0, 4, 0, 8)
            };
            for (int i = 0; i < 4; i++)
            {
                siklarIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
                int secenekIndex = i;
                Button btn = new()
                {
                    Font = new Font("Segoe UI", 11.5f),
                    BackColor = Color.FromArgb(24, 34, 52),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(18, 0, 0, 0),
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0, 4, 0, 4),
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(38, 52, 77);
                btn.Click += (s, e) => SecenekSecildi(secenekIndex);

                _secenekButonlari[i] = btn;
                siklarIzgara.Controls.Add(btn, 0, i);
            }

            _pnlNedenYanlis = CerceveliPanelOlustur(Color.FromArgb(234, 88, 12), Color.FromArgb(43, 27, 23), 1);
            _pnlNedenYanlis.Padding = new Padding(18, 10, 18, 10);
            _pnlNedenYanlis.Visible = false;

            _lblNedenYanlisAciklama = new()
            {
                Font = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(254, 215, 170),
                Dock = DockStyle.Fill
            };

            _lblNedenYanlisBaslik = new()
            {
                Text = "Neden Yanlış Açıklaması",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(251, 146, 60),
                Dock = DockStyle.Top,
                Height = 28
            };

            _pnlNedenYanlis.Controls.Add(_lblNedenYanlisAciklama);
            _pnlNedenYanlis.Controls.Add(_lblNedenYanlisBaslik);

            Panel pnlAlt = new() { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) };

            _btnIpucuKullan = new()
            {
                Text = "💡 İpucu Kullan (2 Yanlış Şıkkı Ele)",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.FromArgb(250, 204, 21),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(300, 46),
                Dock = DockStyle.Left,
                Cursor = Cursors.Hand
            };
            _btnIpucuKullan.FlatAppearance.BorderColor = Color.FromArgb(250, 204, 21);
            _btnIpucuKullan.Click += IpucuKullan_Click;

            _btnSonrakiSoru = new()
            {
                Text = "Sonraki Soru →",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(210, 46),
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnSonrakiSoru.FlatAppearance.BorderSize = 0;
            _btnSonrakiSoru.Click += SonrakiSoru_Click;

            pnlAlt.Controls.Add(_btnIpucuKullan);
            pnlAlt.Controls.Add(_btnSonrakiSoru);

            sinavIzgara.Controls.Add(pnlUst, 0, 0);
            sinavIzgara.Controls.Add(_lblSoruMetni, 0, 1);
            sinavIzgara.Controls.Add(pnlKodKapsayici, 0, 2);
            sinavIzgara.Controls.Add(siklarIzgara, 0, 3);
            sinavIzgara.Controls.Add(_pnlNedenYanlis, 0, 4);
            sinavIzgara.Controls.Add(pnlAlt, 0, 5);

            _pnlSinavEkrani.Controls.Add(sinavIzgara);
        }

        // ==================== 3. SİMÜLATÖR VE HATA KARNESİ ====================
        private void SimulatorEkraniniCiz()
        {
            PanelTemizle(_pnlSimulatorVeKarne);
            _pnlSimulatorVeKarne.Dock = DockStyle.Fill;
            _pnlSimulatorVeKarne.Padding = new Padding(40, 28, 40, 28);
            _pnlSimulatorVeKarne.BackColor = Color.FromArgb(15, 23, 42);

            TableLayoutPanel simAnaIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            simAnaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 65F));
            simAnaIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Panel pnlUst = new() { Dock = DockStyle.Fill };
            Label lblBaslik = new()
            {
                Text = "🛠️ Gömülü Kod Simülatörü ve Çevrimdışı Hata Karnesi",
                Font = new Font("Segoe UI", 19, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Left,
                AutoSize = true
            };

            Button btnGeri = new()
            {
                Text = "✕ Ana Menüye Dön",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(180, 42),
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };
            btnGeri.FlatAppearance.BorderSize = 0;
            btnGeri.Click += (s, e) => EkranDegistir("ANA");
            pnlUst.Controls.Add(lblBaslik);
            pnlUst.Controls.Add(btnGeri);

            TableLayoutPanel govdeIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            govdeIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            govdeIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));

            // SOL PANEL: Canlı Simülatör
            Panel pnlSol = CerceveliPanelOlustur(Color.FromArgb(56, 189, 248), Color.FromArgb(22, 32, 50), 1);
            pnlSol.Margin = new Padding(0, 8, 12, 0);
            pnlSol.Padding = new Padding(22);

            TableLayoutPanel solIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5
            };
            solIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            solIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            solIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 64F));
            solIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 55F));
            solIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 36F));

            Label lblSimBaslik = new()
            {
                Text = "Zor Mod — Gizli Syntax Tuzağı Analizi (Canlı Editör)",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Dock = DockStyle.Fill
            };

            Label lblSimYonerge = new()
            {
                Text = "Aşağıdaki C# kodunda bulunan 2 gizli sözdizimi/mantık tuzağını düzeltip 'Kontrolcüyü Çalıştır' butonuna basın:",
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(203, 213, 225),
                Dock = DockStyle.Fill
            };

            _txtSimulatorKod = new()
            {
                Font = new Font("Consolas", 12.5f),
                BackColor = Color.FromArgb(9, 13, 22),
                ForeColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Text = "// GÖREV: Aşağıdaki 2 gizli syntax hatasını düzeltin:\n" +
                       "// 1. if bloğunun sonundaki noktalı virgül (;) tuzağını kaldırın.\n" +
                       "// 2. Atama operatörü (=) yerine karşılaştırma (==) kullanın.\n\n" +
                       "public void YetkiKontrol(int kullaniciRol)\n" +
                       "{\n" +
                       "    int adminRol = 1;\n\n" +
                       "    if (kullaniciRol = adminRol);\n" +
                       "    {\n" +
                       "        Console.WriteLine(\"Yönetici girişi başarılı.\");\n" +
                       "    }\n" +
                       "}"
            };

            Button btnAnalizEt = new()
            {
                Text = "▶ Yerel Sözdizimi Kontrolcüsünü Çalıştır",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.FromArgb(15, 23, 42),
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 8, 0, 8),
                Cursor = Cursors.Hand
            };
            btnAnalizEt.FlatAppearance.BorderSize = 0;

            _lblSimulatorSonuc = new()
            {
                Text = "⚠️ Yerel Sözdizimi Kontrolcüsü Bekliyor...\n• Satır 9: 'if (kullaniciRol = adminRol);' satırında gizli noktalı virgül tuzağı ve hatalı atama tespit edildi.",
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Color.FromArgb(251, 146, 60),
                BackColor = Color.FromArgb(43, 27, 23),
                Padding = new Padding(14),
                Dock = DockStyle.Fill
            };

            // SAĞ PANEL: Hata Karnesi ve Rozetler
            Panel pnlSag = CerceveliPanelOlustur(Color.FromArgb(139, 92, 246), Color.FromArgb(22, 32, 50), 1);
            pnlSag.Margin = new Padding(12, 8, 0, 0);
            pnlSag.Padding = new Padding(22);

            TableLayoutPanel sagIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 7
            };
            sagIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            sagIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            sagIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            sagIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            sagIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            sagIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            sagIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));

            Label lblKarneBaslik = new() { Text = "📊 Çevrimdışı Hata Karnesi & Beceri Düzeyleri", Font = new Font("Segoe UI", 13.5f, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill };
            Label lblBeceri1 = new() { Text = $"• Görsel Programlama Yetkinliği: %{_gorselBasari}", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(20, 184, 166), Dock = DockStyle.Fill };
            Label lblBeceri2 = new() { Text = $"• Veri Tabanları Yetkinliği: %{_veriTabaniBasari}", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(245, 158, 11), Dock = DockStyle.Fill };
            Label lblBeceri3 = new() { Text = $"• Siber Güvenlik Yetkinliği: %{_siberBasari}   |   🔑 İpucu Jetonu: {_ipucuJetonu}", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(236, 72, 153), Dock = DockStyle.Fill };

            btnAnalizEt.Click += (s, e) =>
            {
                string kod = _txtSimulatorKod.Text;
                bool esittirDuzeldi = Regex.IsMatch(kod, @"if\s*\(\s*kullaniciRol\s*==\s*adminRol\s*\)");
                bool noktaliVirgulVar = Regex.IsMatch(kod, @"if\s*\([^)]*\)\s*;");

                if (!noktaliVirgulVar && esittirDuzeldi)
                {
                    _lblSimulatorSonuc.BackColor = Color.FromArgb(20, 50, 35);
                    _lblSimulatorSonuc.ForeColor = Color.FromArgb(74, 222, 128);
                    _lblSimulatorSonuc.Text = "✓ DERLEME BAŞARILI (0 Hata, 0 Uyarı)\n\nTebrikler! Hem 'if' sonundaki gizli noktalı virgül tuzağını hem de '==' karşılaştırma operatörünü başarıyla düzelttiniz. 'Hata Avcısı' rozeti kazanıldı!";

                    if (!_simulatorOduluAlindi)
                    {
                        _simulatorOduluAlindi = true;
                        _siberBasari = Math.Min(100, _siberBasari + 25);
                        lblBeceri3.Text = $"• Siber Güvenlik Yetkinliği: %{_siberBasari}   |   🔑 İpucu Jetonu: {_ipucuJetonu}";
                        KaliciIlerlemeyiKaydet(); // SQLite ilerleme.db'ye kaydet!
                    }
                }
                else
                {
                    _lblSimulatorSonuc.BackColor = Color.FromArgb(69, 26, 26);
                    _lblSimulatorSonuc.ForeColor = Color.FromArgb(252, 165, 165);
                    _lblSimulatorSonuc.Text = "✕ SÖZDİZİMİ HATASI DEVAM EDİYOR:\n" +
                        (noktaliVirgulVar ? "• 'if' parantezinin sonundaki ';' karakterini silmelisiniz.\n" : "") +
                        (!esittirDuzeldi ? "• Koşul içinde '=' yerine '==' karşılaştırma operatörü kullanmalısınız." : "");
                }
            };

            solIzgara.Controls.Add(lblSimBaslik, 0, 0);
            solIzgara.Controls.Add(lblSimYonerge, 0, 1);
            solIzgara.Controls.Add(_txtSimulatorKod, 0, 2);
            solIzgara.Controls.Add(btnAnalizEt, 0, 3);
            solIzgara.Controls.Add(_lblSimulatorSonuc, 0, 4);
            pnlSol.Controls.Add(solIzgara);

            Label lblRozetBaslik = new() { Text = "🏆 Kazanılan Yerel Oyunlaştırma Rozetleri", Font = new Font("Segoe UI", 13.5f, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill };

            TableLayoutPanel rozetIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2
            };
            rozetIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            rozetIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            rozetIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            rozetIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            rozetIzgara.Controls.Add(RozetKarti("🔍 Hata Avcısı", "Gizli syntax tuzaklarını buldu.", Color.FromArgb(239, 68, 68)), 0, 0);
            rozetIzgara.Controls.Add(RozetKarti("🛠️ Kararlı Geliştirici", "%100 çevrimdışı modülleri tamamladı.", Color.FromArgb(245, 158, 11)), 1, 0);
            rozetIzgara.Controls.Add(RozetKarti("🗄️ SQL Ustası", "İlişkisel sorgu testlerini geçti.", Color.FromArgb(56, 189, 248)), 0, 1);
            rozetIzgara.Controls.Add(RozetKarti("🛡️ Siber Muhafız", "Hash ve güvenli kodlama barajını aştı.", Color.FromArgb(34, 197, 94)), 1, 1);

            Button btnKarneIndir = new()
            {
                Text = "💾 Çevrimdışı Hata Karnesini (.txt) Masaüstüne Kaydet",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 6, 0, 0),
                Cursor = Cursors.Hand
            };
            btnKarneIndir.FlatAppearance.BorderSize = 0;
            btnKarneIndir.Click += (s, e) =>
            {
                try
                {
                    string raporYolu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Cevrimdisi_Hata_Karnesi.txt");
                    File.WriteAllText(raporYolu,
                        "=== KADEMELİ BİLİŞİM VE SİBER GÜVENLİK EĞİTİM PLATFORMU ===\n" +
                        $"Rapor Tarihi: {DateTime.Now}\n\n" +
                        $"Görsel Programlama Başarısı: %{_gorselBasari}\n" +
                        $"Veri Tabanları Başarısı: %{_veriTabaniBasari}\n" +
                        $"Siber Güvenlik Başarısı: %{_siberBasari}\n" +
                        $"Kalan İpucu Jetonu: {_ipucuJetonu}\n" +
                        $"Hata Avcısı Rozeti: {(_simulatorOduluAlindi ? "Kazanıldı" : "Henüz Kazanılmadı")}\n" +
                        "Durum: %100 Çevrimdışı SQLite Yerel Doğrulama Tamamlandı.");
                    MessageBox.Show($"Hata karneniz masaüstüne kaydedildi:\n{raporYolu}", "Rapor Kaydedildi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Rapor kaydedilemedi:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            sagIzgara.Controls.Add(lblKarneBaslik, 0, 0);
            sagIzgara.Controls.Add(lblBeceri1, 0, 1);
            sagIzgara.Controls.Add(lblBeceri2, 0, 2);
            sagIzgara.Controls.Add(lblBeceri3, 0, 3);
            sagIzgara.Controls.Add(lblRozetBaslik, 0, 4);
            sagIzgara.Controls.Add(rozetIzgara, 0, 5);
            sagIzgara.Controls.Add(btnKarneIndir, 0, 6);
            pnlSag.Controls.Add(sagIzgara);

            govdeIzgara.Controls.Add(pnlSol, 0, 0);
            govdeIzgara.Controls.Add(pnlSag, 1, 0);

            simAnaIzgara.Controls.Add(pnlUst, 0, 0);
            simAnaIzgara.Controls.Add(govdeIzgara, 0, 1);

            _pnlSimulatorVeKarne.Controls.Add(simAnaIzgara);
        }

        private Panel RozetKarti(string ad, string aciklama, Color renk)
        {
            Panel p = CerceveliPanelOlustur(renk, Color.FromArgb(15, 23, 42), 1);
            p.Margin = new Padding(6);
            p.Padding = new Padding(12);
            Label l2 = new() { Text = aciklama, Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(203, 213, 225), Dock = DockStyle.Fill };
            Label l1 = new() { Text = ad, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = renk, Dock = DockStyle.Top, Height = 26 };
            p.Controls.Add(l2);
            p.Controls.Add(l1);
            return p;
        }

        // ==================== ETKİLEŞİM METOTLARI ====================
        private void UsbPaketYukle_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog ofd = new()
            {
                Title = "Çevrimdışı Soru Paketi Seçin (.json)",
                Filter = "JSON Soru Paketi (*.json)|*.json"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    int eklenen = _soruServisi.DisaridanPaketYukle(ofd.FileName);
                    string atlananBilgi = _soruServisi.SonPaketAtlananSayisi > 0
                        ? $"\nGeçersiz Olduğu İçin Atlanan: {_soruServisi.SonPaketAtlananSayisi}"
                        : "";

                    MessageBox.Show(
                        $"Paket SQLite veri tabanına aktarıldı!\n\nYeni Eklenen Soru Sayısı: {eklenen}{atlananBilgi}\nToplam Soru Havuzu: {_soruServisi.TumSorulariGetir().Count}",
                        "Çevrimdışı Paket Yükleme Başarılı",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    AnaMenuyuCiz();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Paket yüklenirken format hatası oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void IpucuKullan_Click(object? sender, EventArgs e)
        {
            if (_ipucuJetonu <= 0)
            {
                MessageBox.Show("İpucu jetonunuz kalmadı! Doğru cevaplar vererek yeni jeton kazanabilirsiniz.", "Jeton Bitti", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_aktifSorular.Count == 0 || _mevcutSoruIndex < 0 || _mevcutSoruIndex >= _aktifSorular.Count)
                return;

            Soru soru = _aktifSorular[_mevcutSoruIndex];
            int elenen = 0;
            for (int i = 0; i < 4; i++)
            {
                if (i != soru.DogruSecenekIndex && _secenekButonlari[i].Enabled && elenen < 2)
                {
                    _secenekButonlari[i].Enabled = false;
                    _secenekButonlari[i].Text += "  [İpucu ile Elendi]";
                    elenen++;
                }
            }

            _ipucuJetonu--;
            _lblJetonSayaci.Text = $"🔑 İpucu Jetonu: {_ipucuJetonu}   ";
            _btnIpucuKullan.Enabled = false;
            KaliciIlerlemeyiKaydet(); // Jeton harcandığında SQLite'a kaydet
        }

        private void SinaviBaslat(DisiplinTuru? disiplin, ZorlukSeviyesi? zorluk, string baslik)
        {
            _aktifSorular = disiplin.HasValue
                ? _soruServisi.SorulariFiltrele(disiplin.Value, zorluk)
                : _soruServisi.TumSorulariGetir(zorluk);

            if (_aktifSorular.Count == 0)
            {
                MessageBox.Show(
                    "Bu seçimde henüz soru bulunamadı.\n\n" +
                    $"Disiplin: {(disiplin?.ToString() ?? "Tümü")}\n" +
                    $"Zorluk: {(zorluk?.ToString() ?? "Tümü")}\n\n" +
                    "Soru havuzunu Data/Sorular.json ile genişletebilir veya USB paket yükleyebilirsiniz.",
                    "Soru Bulunamadı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Aynı sorular hep aynı sırada gelmesin (ezberleme önlemi).
            Karistir(_aktifSorular);

            _cevaplar = new List<bool>();
            _mevcutSoruIndex = 0;
            _dogruSayisi = 0;
            _gecenSaniye = 0;
            _lblDisiplinBaslik.Text = baslik;
            _lblSureSayaci.Text = "⏱️ 00:00   ";

            EkranDegistir("SINAV");
            _sinavTimer.Start();
            SoruyuEkranaYukle();
        }

        private static void Karistir<T>(IList<T> liste)
        {
            for (int i = liste.Count - 1; i > 0; i--)
            {
                int j = Random.Shared.Next(i + 1);
                (liste[i], liste[j]) = (liste[j], liste[i]);
            }
        }

        // ==================== KOD GÖRÜNTÜLEYİCİ SATIR NUMARALARI ====================
        private const int EM_GETFIRSTVISIBLELINE = 0xCE;
        private const int EM_LINESCROLL = 0xB6;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Kod kutusu kaydırıldığında satır numarası sütununu aynı konuma getirir.
        /// İki kutu da aynı fontu kullandığı için satır yükseklikleri birebir eşleşir.
        /// </summary>
        private void KodSatirlariniHizala()
        {
            if (!_txtKodBlogu.IsHandleCreated || !_lblKodSatirNo.IsHandleCreated)
                return;

            int hedef = (int)SendMessage(_txtKodBlogu.Handle, EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero);
            int mevcut = (int)SendMessage(_lblKodSatirNo.Handle, EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero);

            if (hedef != mevcut)
                SendMessage(_lblKodSatirNo.Handle, EM_LINESCROLL, IntPtr.Zero, (IntPtr)(hedef - mevcut));
        }

        private void SoruyuEkranaYukle()
        {
            Soru soru = _aktifSorular[_mevcutSoruIndex];
            _lblSoruSayaci.Text = $"Soru {_mevcutSoruIndex + 1} / {_aktifSorular.Count}  •  Zorluk: {soru.Zorluk}";
            _lblJetonSayaci.Text = $"🔑 İpucu Jetonu: {_ipucuJetonu}   ";
            _lblSoruMetni.Text = soru.Baslik;
            _txtKodBlogu.Text = soru.KodBlogu ?? "// Bu soru için kod bloğu bulunmamaktadır.";

            int satirSayisi = Math.Max(1, _txtKodBlogu.Text.Split('\n').Length);
            var satirlar = new string[satirSayisi];
            for (int s = 0; s < satirSayisi; s++) satirlar[s] = (s + 1).ToString();
            _lblKodSatirNo.Text = string.Join("\n", satirlar);

            // Yeni soruda her iki görünümü de başa sar ve satır numaralarını hizala.
            _txtKodBlogu.SelectionStart = 0;
            _txtKodBlogu.ScrollToCaret();
            _lblKodSatirNo.SelectionStart = 0;
            KodSatirlariniHizala();

            string[] harfler = { "A", "B", "C", "D" };
            for (int i = 0; i < 4; i++)
            {
                _secenekButonlari[i].Text = $"{harfler[i]})  {soru.Secenekler[i]}";
                _secenekButonlari[i].BackColor = Color.FromArgb(24, 34, 52);
                _secenekButonlari[i].FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
                _secenekButonlari[i].Enabled = true;
            }

            _btnIpucuKullan.Enabled = true;
            _pnlNedenYanlis.Visible = false;
            _btnSonrakiSoru.Visible = false;
            _btnSonrakiSoru.Text = _mevcutSoruIndex == _aktifSorular.Count - 1
                ? "Sınavı Bitir ✓"
                : "Sonraki Soru →";
        }

        private void SecenekSecildi(int secilenIndex)
        {
            Soru soru = _aktifSorular[_mevcutSoruIndex];
            bool dogruMu = secilenIndex == soru.DogruSecenekIndex;

            // Disiplin bazlı değerlendirme için cevabı kaydet.
            if (_cevaplar.Count > _mevcutSoruIndex)
                _cevaplar[_mevcutSoruIndex] = dogruMu;
            else
                _cevaplar.Add(dogruMu);

            foreach (var btn in _secenekButonlari)
                btn.Enabled = false;
            _btnIpucuKullan.Enabled = false;

            _secenekButonlari[soru.DogruSecenekIndex].BackColor = Color.FromArgb(20, 83, 45);
            _secenekButonlari[soru.DogruSecenekIndex].FlatAppearance.BorderColor = Color.FromArgb(34, 197, 94);

            if (dogruMu)
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
                _secenekButonlari[secilenIndex].BackColor = Color.FromArgb(69, 26, 26);
                _secenekButonlari[secilenIndex].FlatAppearance.BorderColor = Color.FromArgb(239, 68, 68);

                _pnlNedenYanlis.BackColor = Color.FromArgb(43, 27, 23);
                _lblNedenYanlisBaslik.Text = "✕ Neden Yanlış Açıklaması";
                _lblNedenYanlisBaslik.ForeColor = Color.FromArgb(251, 146, 60);
                _lblNedenYanlisAciklama.ForeColor = Color.FromArgb(254, 215, 170);

                if (soru.YanlisCevapAciklamalari is not null &&
                    soru.YanlisCevapAciklamalari.TryGetValue(secilenIndex, out string? aciklama) &&
                    !string.IsNullOrWhiteSpace(aciklama))
                {
                    _lblNedenYanlisAciklama.Text = aciklama;
                }
                else
                {
                    _lblNedenYanlisAciklama.Text = "Seçtiğiniz şık bu senaryo için uygun değildir. Yeşil işaretli doğru cevabı inceleyiniz.";
                }
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
                _sinavTimer.Stop();
                int soruSayisi = _aktifSorular.Count;
                double basariOrani = soruSayisi == 0 ? 0 : (double)_dogruSayisi / soruSayisi * 100;

                // Başarıyı her disiplin için ayrı hesapla; karma sınavda
                // bir modülün yüzdesi diğer modüllerin verisini ezmesin.
                var disiplinSonuclari = _aktifSorular
                    .Select((soru, index) => new
                    {
                        soru.Disiplin,
                        Dogru = index < _cevaplar.Count && _cevaplar[index]
                    })
                    .GroupBy(x => x.Disiplin)
                    .Select(g => new
                    {
                        Disiplin = g.Key,
                        Yuzde = (int)Math.Round(g.Count(x => x.Dogru) * 100.0 / g.Count())
                    });

                foreach (var sonuc in disiplinSonuclari)
                {
                    switch (sonuc.Disiplin)
                    {
                        case DisiplinTuru.GorselProgramlama:
                            _gorselBasari = Math.Max(_gorselBasari, sonuc.Yuzde);
                            break;
                        case DisiplinTuru.VeriTabanlari:
                            _veriTabaniBasari = Math.Max(_veriTabaniBasari, sonuc.Yuzde);
                            break;
                        case DisiplinTuru.SiberGuvenlik:
                            _siberBasari = Math.Max(_siberBasari, sonuc.Yuzde);
                            break;
                    }
                }

                if (basariOrani >= 75)
                    _ipucuJetonu++;

                // Yeni başarı yüzdelerini ve jetonu SQLite ilerleme.db'ye kalıcı kaydet!
                KaliciIlerlemeyiKaydet();

                string barajMesaji = basariOrani >= 75
                    ? "🎉 TEBRİKLER! %75 Başarı Barajını geçtiniz, Orta Mod kilidi açıldı ve +1 İpucu Jetonu SQLite profilinize kaydedildi!"
                    : "⚠️ %75 Başarı Barajının altında kaldınız. Orta moda geçmek için yanlış açıklamalarını tekrar gözden geçirin.";

                MessageBox.Show(
                    $"Sınav Tamamlandı!\n\nDoğru Sayısı: {_dogruSayisi} / {_aktifSorular.Count}\nGeçen Süre: {_gecenSaniye} saniye\nBaşarı Yüzdesi: %{basariOrani:0}\n\n{barajMesaji}",
                    "Çevrimdışı Hata Karnesi ve Sonuç",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                EkranDegistir("ANA");
            }
        }
    }
}