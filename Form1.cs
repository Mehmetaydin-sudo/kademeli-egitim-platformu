using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using KademeliEgitimPlatformu.Models;
using KademeliEgitimPlatformu.Services;

namespace KademeliEgitimPlatformu
{
    /// <summary>
    /// Uygulamanın ana formu. Ekran çizimleri kendi dosyalarına bölünmüştür:
    ///   Form1.AnaMenu.cs    — kontrol paneli, modül ve zorluk kartları, USB paket yükleme
    ///   Form1.Sinav.cs      — sınav ekranı, soru akışı, puanlama, klavye kısayolları
    ///   Form1.Simulator.cs  — Roslyn kod simülatörü ve hata karnesi
    /// </summary>
    public partial class Form1 : Form
    {
        private const string Surum = "v0.6.0";

        // Servisler yalnızca ilk ihtiyaç duyulduğunda oluşturulur.
        // Böylece açılışta gereksiz veritabanı ve Roslyn hazırlığı yapılmaz.
        private SoruServisi? _soruServisi;
        private IlerlemeServisi? _ilerlemeServisi;
        private KarneServisi? _karneServisi;
        private SimulatorServisi? _simulatorServisi;

        private SoruServisi SoruDeposu => _soruServisi ??= new SoruServisi();
        private IlerlemeServisi IlerlemeDeposu => _ilerlemeServisi ??= new IlerlemeServisi();
        private KarneServisi KarneDeposu => _karneServisi ??= new KarneServisi();
        private SimulatorServisi Simulator => _simulatorServisi ??= new SimulatorServisi();

        // Ekranlar yalnızca ilk kez gösterildiğinde kurulur; her geçişte yeniden çizilmez.
        private bool _anaMenuKirli = true;
        private bool _sinavHazir;
        private bool _simulatorHazir;

        private List<Soru> _aktifSorular = new();
        private List<SoruGorunumu> _gorunumler = new();
        private List<bool> _cevaplar = new();
        private List<YanlisKaydi> _oturumYanlislari = new();
        private int _mevcutSoruIndex = 0;
        private int _gecenSaniye = 0;
        private DateTime _sinavBaslangic;
        private System.Windows.Forms.Timer _sinavTimer = new();

        // SQLite (ilerleme.db) üzerinden yüklenen kalıcı kullanıcı verileri
        // Yeni kullanıcı, kazanmadığı bir başarı yüzdesiyle başlamaz (hepsi 0).
        private int _ipucuJetonu = 3;
        private int _gorselBasari = 0;
        private int _veriTabaniBasari = 0;
        private int _siberBasari = 0;
        private bool _simulatorOduluAlindi = false;

        // Ana Yerleşim: Sol Sidebar + Sağ İçerik Alanı
        private Panel _pnlSidebar = new BufferedPanel();
        private Panel _pnlIcerikAlani = new BufferedPanel();
        private Button _btnMenuAna = new();
        private Button _btnMenuSinav = new();
        private Button _btnMenuSimulator = new();

        // 3 Ana Ekran Paneli
        private Panel _pnlAnaMenu = new BufferedPanel();
        private Panel _pnlSinavEkrani = new BufferedPanel();
        private Panel _pnlSimulatorVeKarne = new BufferedPanel();

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
        private RichTextBox _txtSimulatorSonuc = new();

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.KeyPreview = true;

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
                IlerlemeDurumu durum = IlerlemeDeposu.Yukle();
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
            // Başarı yüzdeleri/jeton değişti; ana menü bir sonraki gösterimde tazelenir.
            _anaMenuKirli = true;

            try
            {
                IlerlemeDeposu.Kaydet(new IlerlemeDurumu(
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

        /// <summary>1-4 tuşlarıyla şık seçimi, Enter ile sonraki soru.</summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_pnlSinavEkrani.Visible && _secenekButonlari.Length == 4 && _secenekButonlari[0] is not null)
            {
                int secim = keyData switch
                {
                    Keys.D1 or Keys.NumPad1 => 0,
                    Keys.D2 or Keys.NumPad2 => 1,
                    Keys.D3 or Keys.NumPad3 => 2,
                    Keys.D4 or Keys.NumPad4 => 3,
                    _ => -1
                };

                if (secim >= 0 && _secenekButonlari[secim].Enabled)
                {
                    SecenekSecildi(secim);
                    return true;
                }

                if (keyData == Keys.Enter && _btnSonrakiSoru.Visible)
                {
                    SonrakiSoru_Click(this, EventArgs.Empty);
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
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
            this.Text = $"Kademeli Bilişim ve Siber Güvenlik Eğitim Platformu — {Surum} (SQLite + Roslyn)";
            // 1366x768 gibi düşük çözünürlüklü ekranlarda da pencere ekrana sığar.
            this.Size = new Size(1280, 800);
            this.MinimumSize = new Size(1024, 700);
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(11, 17, 32);
            this.ForeColor = Color.White;

            SidebarOlustur();

            _pnlIcerikAlani.Dock = DockStyle.Fill;
            _pnlIcerikAlani.BackColor = Color.FromArgb(15, 23, 42);

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
                Text = "⚡ EDUSEC\n    SQLITE + ROSLYN",
                Font = FontOnbellegi.Al("Segoe UI", 13f, FontStyle.Bold),
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
                Font = FontOnbellegi.Al("Segoe UI", 8.5f),
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
                Font = FontOnbellegi.Al("Segoe UI", 10.5f, FontStyle.Bold),
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

            // Ana menü yalnızca ilk açılışta veya veriler değiştiğinde yeniden kurulur.
            if (ekranKodu == "ANA" && _anaMenuKirli)
                AnaMenuyuCiz();

            // Sınav ve simülatör ekranları ilk gösterimde bir kez oluşturulur.
            if (ekranKodu == "SINAV" && !_sinavHazir)
            {
                SinavEkraniniCiz();
                _sinavHazir = true;
            }

            if (ekranKodu == "SIM")
            {
                if (!_simulatorHazir)
                {
                    SimulatorEkraniniCiz();
                    _simulatorHazir = true;
                }
                else
                {
                    BeceriEtiketleriniGuncelle();
                }
            }

            _btnMenuAna.BackColor = ekranKodu == "ANA" ? Color.FromArgb(30, 41, 59) : Color.FromArgb(9, 13, 24);
            _btnMenuAna.ForeColor = ekranKodu == "ANA" ? Color.FromArgb(56, 189, 248) : Color.FromArgb(203, 213, 225);

            _btnMenuSinav.BackColor = ekranKodu == "SINAV" ? Color.FromArgb(30, 41, 59) : Color.FromArgb(9, 13, 24);
            _btnMenuSinav.ForeColor = ekranKodu == "SINAV" ? Color.FromArgb(56, 189, 248) : Color.FromArgb(203, 213, 225);

            _btnMenuSimulator.BackColor = ekranKodu == "SIM" ? Color.FromArgb(30, 41, 59) : Color.FromArgb(9, 13, 24);
            _btnMenuSimulator.ForeColor = ekranKodu == "SIM" ? Color.FromArgb(56, 189, 248) : Color.FromArgb(203, 213, 225);
        }

        private Panel CerceveliPanelOlustur(Color cerceveRengi, Color arkaPlan, int kalinlik = 1)
        {
            // BufferedPanel: yeniden boyutlandırma ve ekran geçişlerinde titremeyi önler.
            Panel p = new BufferedPanel
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
            return p;
        }
    }
}
