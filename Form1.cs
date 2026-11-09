using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
        private int _ipucuJetonu = 3;

        private int _gorselBasari = 65;
        private int _veriTabaniBasari = 40;
        private int _siberBasari = 10;

        // 3 Ana Ekran Paneli
        private Panel _pnlAnaMenu = new();
        private Panel _pnlSinavEkrani = new();
        private Panel _pnlSimulatorVeKarne = new();

        // Sınav Ekranı Bileşenleri
        private Label _lblDisiplinBaslik = new();
        private Label _lblSoruSayaci = new();
        private Label _lblJetonSayaci = new();
        private Label _lblSoruMetni = new();
        private RichTextBox _txtKodBlogu = new();
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
            ArayuzuOlustur();
        }

        private void ArayuzuOlustur()
        {
            // DPI ve 1080p Tam Ekran Responsive Ayarları
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.Text = "Kademeli Bilişim ve Siber Güvenlik Eğitim Platformu — v0.2.1 (1080p Responsive)";
            this.Size = new Size(1600, 950);
            this.MinimumSize = new Size(1280, 800);
            this.WindowState = FormWindowState.Maximized; // Doğrudan 1080p Tam Ekran açılır
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(15, 23, 42);
            this.ForeColor = Color.White;

            AnaMenuyuCiz();
            SinavEkraniniCiz();
            SimulatorEkraniniCiz();

            this.Controls.Add(_pnlAnaMenu);
            this.Controls.Add(_pnlSinavEkrani);
            this.Controls.Add(_pnlSimulatorVeKarne);

            _pnlSinavEkrani.Visible = false;
            _pnlSimulatorVeKarne.Visible = false;
        }

        // ==================== 1. ANA MENÜ (RESPONSIVE GRID) ====================
        private void AnaMenuyuCiz()
        {
            _pnlAnaMenu.Controls.Clear();
            _pnlAnaMenu.Dock = DockStyle.Fill;
            _pnlAnaMenu.Padding = new Padding(45, 30, 45, 30);
            _pnlAnaMenu.BackColor = Color.FromArgb(15, 23, 42);

            TableLayoutPanel anaIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6
            };
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 95F));    // Üst Başlık
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));    // Modüller Alt Başlık
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 52F));     // 3 Disiplin Kartı
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));    // Zorluk Alt Başlık
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 28F));     // 3 Zorluk Kartı
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));    // Alt Aksiyon Butonları

            // 1. Satır: Üst Bar
            Panel pnlUst = new() { Dock = DockStyle.Fill };
            Label lblBaslik = new()
            {
                Text = "Kademeli Bilişim ve Siber Güvenlik Eğitim Platformu",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Left,
                AutoSize = true
            };

            Button btnUsbPaketYukle = new()
            {
                Text = "📥 USB Paket Yükle (.json)",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.FromArgb(125, 211, 252),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(250, 48),
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };
            btnUsbPaketYukle.FlatAppearance.BorderColor = Color.FromArgb(56, 189, 248);
            btnUsbPaketYukle.Click += UsbPaketYukle_Click;

            Label lblCevrimdisi = new()
            {
                Text = "  ● %100 Çevrimdışı (Yerel SQLite/JSON Aktif)  ",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 222, 128),
                BackColor = Color.FromArgb(20, 83, 45),
                Dock = DockStyle.Right,
                AutoSize = true,
                Padding = new Padding(12, 12, 12, 12)
            };

            Panel pnlBosluk = new() { Width = 20, Dock = DockStyle.Right };
            pnlUst.Controls.Add(lblBaslik);
            pnlUst.Controls.Add(lblCevrimdisi);
            pnlUst.Controls.Add(pnlBosluk);
            pnlUst.Controls.Add(btnUsbPaketYukle);

            // 2. Satır: Eğitim Modülleri Başlığı
            Label lblAltBaslik = new()
            {
                Text = $"Eğitim Modülleri — Yerel Soru Havuzu: {_soruServisi.TumSorulariGetir().Count} Soru",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            };

            // 3. Satır: 3 Disiplin Kartı (Yüzdelik Eşit Dağılım)
            TableLayoutPanel modullerIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 10, 0, 10)
            };
            modullerIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            modullerIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            modullerIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            modullerIzgara.Controls.Add(ModulKartiOlustur("Görsel Programlama", "C# Form kontrolleri, olay (event) yönetimi ve asenkron mimari.", _gorselBasari, Color.FromArgb(20, 184, 166), DisiplinTuru.GorselProgramlama), 0, 0);
            modullerIzgara.Controls.Add(ModulKartiOlustur("Veri Tabanları", "SQLite yerel mimari, ilişkisel tablolar ve modüler sorgu tasarımı.", _veriTabaniBasari, Color.FromArgb(245, 158, 11), DisiplinTuru.VeriTabanlari), 1, 0);
            modullerIzgara.Controls.Add(ModulKartiOlustur("Siber Güvenlik", "Güvenli kodlama ilkeleri, SHA-256 hashleme ve zafiyet tespiti.", _siberBasari, Color.FromArgb(236, 72, 153), DisiplinTuru.SiberGuvenlik), 2, 0);

            // 4. Satır: Zorluk Başlığı
            Label lblZorlukBaslik = new()
            {
                Text = "Zorluk Seviyesi Seçimi ve Kademeli Baraj Durumu",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            };

            // 5. Satır: 3 Zorluk Kartı
            TableLayoutPanel zorlukIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 10, 0, 10)
            };
            zorlukIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            zorlukIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            zorlukIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            bool ortaAcik = _gorselBasari >= 75 || _veriTabaniBasari >= 75 || _siberBasari >= 75;
            zorlukIzgara.Controls.Add(ZorlukBilgiKarti("✓ Kolay Mod (Açık)", "Temel kavramlar, bileşen tanımları ve rehberli başlangıç adımları.", Color.FromArgb(34, 197, 94)), 0, 0);
            zorlukIzgara.Controls.Add(ZorlukBilgiKarti(ortaAcik ? "✓ Orta Mod (Kilidi Açıldı!)" : "🔒 Orta Mod (%75 Başarı Barajı)", "Sistem tepkisi, modüler sınıf kurgusu ve sınav başarısı.", Color.FromArgb(245, 158, 11)), 1, 0);
            zorlukIzgara.Controls.Add(ZorlukBilgiKarti("🔒 Zor Mod (Gömülü Simülatör)", "İlk bakışta gizlenen syntax tuzakları ve çalışma zamanı hata analizi.", Color.FromArgb(56, 189, 248)), 2, 0);

            // 6. Satır: Alt Butonlar
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
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 10, 12, 0),
                Cursor = Cursors.Hand
            };
            btnTumunuCoz.FlatAppearance.BorderSize = 0;
            btnTumunuCoz.Click += (s, e) => SinaviBaslat(null, "Karma Değerlendirme Sınavı — Tüm Disiplinler");

            Button btnSimulatorAc = new()
            {
                Text = "🛠️ Gömülü Kod Simülatörü & Hata Karnesi (Zor Mod)",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Color.FromArgb(139, 92, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Margin = new Padding(12, 10, 0, 0),
                Cursor = Cursors.Hand
            };
            btnSimulatorAc.FlatAppearance.BorderSize = 0;
            btnSimulatorAc.Click += (s, e) =>
            {
                _pnlAnaMenu.Visible = false;
                SimulatorEkraniniCiz();
                _pnlSimulatorVeKarne.Visible = true;
            };

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
            Panel kart = new()
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(10),
                Padding = new Padding(22),
                BackColor = Color.FromArgb(30, 41, 59)
            };

            TableLayoutPanel icIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5
            };
            icIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            icIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            icIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            icIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            icIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));

            Label lblAd = new()
            {
                Text = baslik,
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = vurguRengi,
                Dock = DockStyle.Fill
            };

            Label lblAciklama = new()
            {
                Text = aciklama,
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Color.FromArgb(203, 213, 225),
                Dock = DockStyle.Fill
            };

            Label lblGelisim = new()
            {
                Text = $"Gelişim Durumu: %{ilerlemeYuzdesi}",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Fill
            };

            ProgressBar bar = new()
            {
                Minimum = 0,
                Maximum = 100,
                Value = Math.Min(100, ilerlemeYuzdesi),
                Dock = DockStyle.Top,
                Height = 12
            };

            Button btnBasla = new()
            {
                Text = "Modüle Devam Et →",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = vurguRengi,
                ForeColor = Color.FromArgb(15, 23, 42),
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand
            };
            btnBasla.FlatAppearance.BorderSize = 0;
            btnBasla.Click += (s, e) => SinaviBaslat(disiplin, $"{baslik} — Kademeli Eğitim Modülü");

            icIzgara.Controls.Add(lblAd, 0, 0);
            icIzgara.Controls.Add(lblAciklama, 0, 1);
            icIzgara.Controls.Add(lblGelisim, 0, 2);
            icIzgara.Controls.Add(bar, 0, 3);
            icIzgara.Controls.Add(btnBasla, 0, 4);

            kart.Controls.Add(icIzgara);
            return kart;
        }

        private Panel ZorlukBilgiKarti(string baslik, string altMetin, Color renk)
        {
            Panel p = new()
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(10),
                Padding = new Padding(20),
                BackColor = Color.FromArgb(30, 41, 59)
            };

            Label l2 = new()
            {
                Text = altMetin,
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Color.FromArgb(203, 213, 225),
                Dock = DockStyle.Fill
            };

            Label l1 = new()
            {
                Text = baslik,
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = renk,
                Dock = DockStyle.Top,
                Height = 36
            };

            p.Controls.Add(l2);
            p.Controls.Add(l1);
            return p;
        }

        // ==================== 2. SINAV EKRANI (RESPONSIVE GRID) ====================
        private void SinavEkraniniCiz()
        {
            _pnlSinavEkrani.Controls.Clear();
            _pnlSinavEkrani.Dock = DockStyle.Fill;
            _pnlSinavEkrani.Padding = new Padding(50, 30, 50, 30);
            _pnlSinavEkrani.BackColor = Color.FromArgb(15, 23, 42);

            TableLayoutPanel sinavIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6
            };
            sinavIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 75F));   // Üst Bar
            sinavIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 75F));   // Soru Metni
            sinavIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));    // Kod Kutusu
            sinavIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));    // 4 Şık
            sinavIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));    // Neden Yanlış Kutusu
            sinavIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 65F));   // Alt Butonlar

            // 1. Satır: Üst Bar
            Panel pnlUst = new() { Dock = DockStyle.Fill };
            _lblDisiplinBaslik = new()
            {
                Text = "Görsel Programlama — Orta Mod",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                AutoSize = true
            };

            _lblSoruSayaci = new()
            {
                Text = "Soru 1 / 3",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Dock = DockStyle.Bottom,
                AutoSize = true
            };

            Button btnAnaMenuyeDon = new()
            {
                Text = "✕ Ana Menüye Dön",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(190, 44),
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };
            btnAnaMenuyeDon.FlatAppearance.BorderSize = 0;
            btnAnaMenuyeDon.Click += (s, e) =>
            {
                _pnlSinavEkrani.Visible = false;
                AnaMenuyuCiz();
                _pnlAnaMenu.Visible = true;
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
            pnlUst.Controls.Add(btnAnaMenuyeDon);

            // 2. Satır: Soru Metni
            _lblSoruMetni = new()
            {
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(241, 245, 249),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 3. Satır: Kod Bloğu
            _txtKodBlogu = new()
            {
                Font = new Font("Consolas", 13f),
                BackColor = Color.FromArgb(9, 13, 22),
                ForeColor = Color.FromArgb(125, 211, 252),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 0, 12)
            };

            // 4. Satır: 4 Şık Butonu
            TableLayoutPanel siklarIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Margin = new Padding(0, 5, 0, 10)
            };
            for (int i = 0; i < 4; i++)
            {
                siklarIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
                int secenekIndex = i;
                Button btn = new()
                {
                    Font = new Font("Segoe UI", 11.5f),
                    BackColor = Color.FromArgb(30, 41, 59),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(18, 0, 0, 0),
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0, 4, 0, 4),
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);
                btn.FlatAppearance.BorderSize = 1;
                btn.Click += (s, e) => SecenekSecildi(secenekIndex);

                _secenekButonlari[i] = btn;
                siklarIzgara.Controls.Add(btn, 0, i);
            }

            // 5. Satır: Neden Yanlış Paneli
            _pnlNedenYanlis = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 12, 18, 12),
                BackColor = Color.FromArgb(43, 27, 23),
                Visible = false
            };

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
                Height = 30
            };

            _pnlNedenYanlis.Controls.Add(_lblNedenYanlisAciklama);
            _pnlNedenYanlis.Controls.Add(_lblNedenYanlisBaslik);

            // 6. Satır: Alt Butonlar (İpucu & Sonraki Soru)
            Panel pnlAlt = new() { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0) };

            _btnIpucuKullan = new()
            {
                Text = "💡 İpucu Kullan (2 Yanlış Şıkkı Ele)",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.FromArgb(250, 204, 21),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(310, 48),
                Dock = DockStyle.Left,
                Cursor = Cursors.Hand
            };
            _btnIpucuKullan.FlatAppearance.BorderSize = 0;
            _btnIpucuKullan.Click += IpucuKullan_Click;

            _btnSonrakiSoru = new()
            {
                Text = "Sonraki Soru →",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(220, 48),
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
            sinavIzgara.Controls.Add(_txtKodBlogu, 0, 2);
            sinavIzgara.Controls.Add(siklarIzgara, 0, 3);
            sinavIzgara.Controls.Add(_pnlNedenYanlis, 0, 4);
            sinavIzgara.Controls.Add(pnlAlt, 0, 5);

            _pnlSinavEkrani.Controls.Add(sinavIzgara);
        }

        // ==================== 3. SİMÜLATÖR VE KARNE (RESPONSIVE GRID) ====================
        private void SimulatorEkraniniCiz()
        {
            _pnlSimulatorVeKarne.Controls.Clear();
            _pnlSimulatorVeKarne.Dock = DockStyle.Fill;
            _pnlSimulatorVeKarne.Padding = new Padding(45, 30, 45, 30);
            _pnlSimulatorVeKarne.BackColor = Color.FromArgb(15, 23, 42);

            TableLayoutPanel simAnaIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            simAnaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            simAnaIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Üst Bar
            Panel pnlUst = new() { Dock = DockStyle.Fill };
            Label lblBaslik = new()
            {
                Text = "🛠️ Gömülü Kod Simülatörü ve Çevrimdışı Hata Karnesi",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Left,
                AutoSize = true
            };

            Button btnGeri = new()
            {
                Text = "✕ Ana Menüye Dön",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(190, 44),
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };
            btnGeri.FlatAppearance.BorderSize = 0;
            btnGeri.Click += (s, e) =>
            {
                _pnlSimulatorVeKarne.Visible = false;
                AnaMenuyuCiz();
                _pnlAnaMenu.Visible = true;
            };
            pnlUst.Controls.Add(lblBaslik);
            pnlUst.Controls.Add(btnGeri);

            // Alt İkili Gövde (Sol Simülatör %55, Sağ Karne %45)
            TableLayoutPanel govdeIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            govdeIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56F));
            govdeIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44F));

            // SOL PANEL: Kod Simülatörü
            Panel pnlSol = new()
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 10, 12, 0),
                Padding = new Padding(25),
                BackColor = Color.FromArgb(30, 41, 59)
            };

            TableLayoutPanel solIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5
            };
            solIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            solIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            solIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 65F));
            solIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            solIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));

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
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Color.FromArgb(203, 213, 225),
                Dock = DockStyle.Fill
            };

            _txtSimulatorKod = new()
            {
                Font = new Font("Consolas", 13f),
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
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
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
                Font = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(251, 146, 60),
                BackColor = Color.FromArgb(43, 27, 23),
                Padding = new Padding(15),
                Dock = DockStyle.Fill
            };

            btnAnalizEt.Click += (s, e) =>
            {
                string kod = _txtSimulatorKod.Text;
                bool noktaliVirgulKalkti = !kod.Contains("adminRol);");
                bool esittirDuzeldi = kod.Contains("kullaniciRol == adminRol");

                if (noktaliVirgulKalkti && esittirDuzeldi)
                {
                    _lblSimulatorSonuc.BackColor = Color.FromArgb(20, 50, 35);
                    _lblSimulatorSonuc.ForeColor = Color.FromArgb(74, 222, 128);
                    _lblSimulatorSonuc.Text = "✓ DERLEME BAŞARILI (0 Hata, 0 Uyarı)\n\nTebrikler! Hem 'if' sonundaki gizli noktalı virgül tuzağını hem de '==' karşılaştırma operatörünü başarıyla düzelttiniz. 'Hata Avcısı' rozeti kazanıldı!";
                    _siberBasari = Math.Min(100, _siberBasari + 25);
                }
                else
                {
                    _lblSimulatorSonuc.BackColor = Color.FromArgb(69, 26, 26);
                    _lblSimulatorSonuc.ForeColor = Color.FromArgb(252, 165, 165);
                    _lblSimulatorSonuc.Text = "✕ SÖZDİZİMİ HATASI DEVAM EDİYOR:\n" +
                        (!noktaliVirgulKalkti ? "• 'if' parantezinin sonundaki ';' karakterini silmelisiniz.\n" : "") +
                        (!esittirDuzeldi ? "• Koşul içinde '=' yerine '==' karşılaştırma operatörü kullanmalısınız." : "");
                }
            };

            solIzgara.Controls.Add(lblSimBaslik, 0, 0);
            solIzgara.Controls.Add(lblSimYonerge, 0, 1);
            solIzgara.Controls.Add(_txtSimulatorKod, 0, 2);
            solIzgara.Controls.Add(btnAnalizEt, 0, 3);
            solIzgara.Controls.Add(_lblSimulatorSonuc, 0, 4);
            pnlSol.Controls.Add(solIzgara);

            // SAĞ PANEL: Karne ve Rozetler
            Panel pnlSag = new()
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(12, 10, 0, 0),
                Padding = new Padding(25),
                BackColor = Color.FromArgb(30, 41, 59)
            };

            TableLayoutPanel sagIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 7
            };
            sagIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            sagIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            sagIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            sagIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            sagIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            sagIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            sagIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 55F));

            Label lblKarneBaslik = new() { Text = "📊 Çevrimdışı Hata Karnesi & Beceri Düzeyleri", Font = new Font("Segoe UI", 13.5f, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill };
            Label lblBeceri1 = new() { Text = $"• Görsel Programlama Yetkinliği: %{_gorselBasari}", Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = Color.FromArgb(20, 184, 166), Dock = DockStyle.Fill };
            Label lblBeceri2 = new() { Text = $"• Veri Tabanları Yetkinliği: %{_veriTabaniBasari}", Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = Color.FromArgb(245, 158, 11), Dock = DockStyle.Fill };
            Label lblBeceri3 = new() { Text = $"• Siber Güvenlik Yetkinliği: %{_siberBasari}   |   🔑 Jeton: {_ipucuJetonu}", Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = Color.FromArgb(236, 72, 153), Dock = DockStyle.Fill };

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
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 8, 0, 0),
                Cursor = Cursors.Hand
            };
            btnKarneIndir.FlatAppearance.BorderSize = 0;
            btnKarneIndir.Click += (s, e) =>
            {
                string raporYolu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Cevrimdisi_Hata_Karnesi.txt");
                File.WriteAllText(raporYolu,
                    "=== KADEMELİ BİLİŞİM VE SİBER GÜVENLİK EĞİTİM PLATFORMU ===\n" +
                    $"Rapor Tarihi: {DateTime.Now}\n\n" +
                    $"Görsel Programlama Başarısı: %{_gorselBasari}\n" +
                    $"Veri Tabanları Başarısı: %{_veriTabaniBasari}\n" +
                    $"Siber Güvenlik Başarısı: %{_siberBasari}\n" +
                    $"Kalan İpucu Jetonu: {_ipucuJetonu}\n" +
                    "Durum: %100 Çevrimdışı Yerel Doğrulama Tamamlandı.");
                MessageBox.Show($"Hata karneniz masaüstüne kaydedildi:\n{raporYolu}", "Rapor Kaydedildi", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            Panel p = new()
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6),
                Padding = new Padding(14),
                BackColor = Color.FromArgb(15, 23, 42)
            };
            Label l2 = new() { Text = aciklama, Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(203, 213, 225), Dock = DockStyle.Fill };
            Label l1 = new() { Text = ad, Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = renk, Dock = DockStyle.Top, Height = 28 };
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
                    MessageBox.Show(
                        $"Paket başarıyla tarandı!\n\nYeni Eklenen Soru Sayısı: {eklenen}\nToplam Soru Havuzu: {_soruServisi.TumSorulariGetir().Count}",
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
        }

        private void SinaviBaslat(DisiplinTuru? disiplin, string baslik)
        {
            _aktifSorular = disiplin.HasValue
                ? _soruServisi.SorulariFiltrele(disiplin.Value)
                : _soruServisi.TumSorulariGetir();

            if (_aktifSorular.Count == 0)
            {
                MessageBox.Show("Bu modülde henüz soru bulunamadı!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            _lblJetonSayaci.Text = $"🔑 İpucu Jetonu: {_ipucuJetonu}   ";
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

            _btnIpucuKullan.Enabled = true;
            _pnlNedenYanlis.Visible = false;
            _btnSonrakiSoru.Visible = false;
        }

        private void SecenekSecildi(int secilenIndex)
        {
            Soru soru = _aktifSorular[_mevcutSoruIndex];

            foreach (var btn in _secenekButonlari)
                btn.Enabled = false;
            _btnIpucuKullan.Enabled = false;

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
                if (basariOrani >= 75)
                {
                    _gorselBasari = Math.Max(_gorselBasari, (int)basariOrani);
                    _ipucuJetonu++;
                }

                string barajMesaji = basariOrani >= 75
                    ? "🎉 TEBRİKLER! %75 Başarı Barajını geçtiniz, Orta Mod kilidi açıldı ve +1 İpucu Jetonu kazandınız!"
                    : "⚠️ %75 Başarı Barajının altında kaldınız. Orta moda geçmek için yanlış açıklamalarını tekrar gözden geçirin.";

                MessageBox.Show(
                    $"Sınav Tamamlandı!\n\nDoğru Sayısı: {_dogruSayisi} / {_aktifSorular.Count}\nBaşarı Yüzdesi: %{basariOrani:0}\n\n{barajMesaji}",
                    "Çevrimdışı Hata Karnesi ve Sonuç",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                _pnlSinavEkrani.Visible = false;
                AnaMenuyuCiz();
                _pnlAnaMenu.Visible = true;
            }
        }
    }
}