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
        private const string Surum = "v0.9.0";

        // Servisler yalnızca ilk ihtiyaç duyulduğunda oluşturulur.
        // Böylece açılışta gereksiz veritabanı ve Roslyn hazırlığı yapılmaz.
        // Arayüz tipleri, ileride sunucu tabanlı bir depoya geçişi kolaylaştırır.
        private ISoruDeposu? _soruServisi;
        private IIlerlemeDeposu? _ilerlemeServisi;
        private KarneServisi? _karneServisi;
        private SimulatorServisi? _simulatorServisi;

        private ISoruDeposu SoruDeposu => _soruServisi ??= new SoruServisi();
        private IIlerlemeDeposu IlerlemeDeposu => _ilerlemeServisi ??= new IlerlemeServisi();
        private KarneServisi KarneDeposu => _karneServisi ??= new KarneServisi();
        private SimulatorServisi Simulator => _simulatorServisi ??= new SimulatorServisi();

        // Ekranlar yalnızca ilk kez gösterildiğinde kurulur; her geçişte yeniden çizilmez.
        private bool _anaMenuKirli = true;
        private bool _sinavHazir;
        private bool _simulatorHazir;
        private bool _veritabaniUyarisiGosterildi;

        private List<Soru> _aktifSorular = new();
        private List<SoruGorunumu> _gorunumler = new();
        private List<bool> _cevaplar = new();
        private List<YanlisKaydi> _oturumYanlislari = new();
        private int _mevcutSoruIndex = 0;
        private int _gecenSaniye = 0;
        private DateTime _sinavBaslangic;
        private bool _sinavDevamEdiyor;
        private System.Windows.Forms.Timer _sinavTimer = new();
        private readonly ToolTip _ipucuBalonu = new();

        // Aktif sınavın türü ve kapsamı (normal sınav / modül ara sınavı).
        private SinavTuru _aktifSinavTuru = SinavTuru.Normal;
        private DisiplinTuru? _aktifSinavDisiplini;
        private ZorlukSeviyesi? _aktifZorluk;

        // PostgreSQL 18 (kademeli_egitim) üzerinden yüklenen kalıcı kullanıcı verileri
        // Yeni kullanıcı, kazanmadığı bir başarı yüzdesiyle başlamaz (hepsi 0).
        private int _ipucuJetonu = 3;
        private int _gorselBasari = 0;
        private int _veriTabaniBasari = 0;
        private int _siberBasari = 0;
        private bool _simulatorOduluAlindi = false;
        private bool _gorselAraSinavGecti = false;
        private bool _veriTabaniAraSinavGecti = false;
        private bool _siberAraSinavGecti = false;
        private bool _ortaModGecti = false;

        // Ana Yerleşim: Sol Sidebar + Sağ İçerik Alanı
        private Panel _pnlSidebar = new BufferedPanel();
        private Panel _pnlIcerikAlani = new BufferedPanel();
        private Button _btnMenuAna = new();
        private Button _btnMenuSinav = new();
        private Button _btnMenuSimulator = new();
        private Button _btnMenuSoruHavuzu = new();

        // 3 Ana Ekran Paneli
        private Panel _pnlAnaMenu = new BufferedPanel();
        private Panel _pnlSinavEkrani = new BufferedPanel();
        private Panel _pnlSimulatorVeKarne = new BufferedPanel();

        // Sınav Ekranı Bileşenleri
        private Label _lblDisiplinBaslik = new();
        private Label _lblSoruSayaci = new();
        private Label _lblSureSayaci = new();
        private Label _lblJetonSayaci = new();
        private Panel _pnlSinavIlerlemeArka = new();
        private Panel _pnlSinavIlerlemeDolu = new();
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

            // Açılışta PostgreSQL (kademeli_egitim) üzerinden kayıtlı verileri yükle
            KaliciIlerlemeyiYukle();

            _sinavTimer.Interval = 1000;
            _sinavTimer.Tick += (s, e) =>
            {
                _gecenSaniye++;
                int dk = _gecenSaniye / 60;
                int sn = _gecenSaniye % 60;
                _lblSureSayaci.Text = $"⏱️ {dk:00}:{sn:00}   ";
            };
            // Kayıtlı tema ve profil tercihini ilk çizimden önce yükle (açılışta titreme/yanlış tema olmasın).
            _aktifTercih = Kisisellestirme.TercihiYukle();

            ArayuzuOlustur();
            KisisellestirmeModulunuBagla();
        }

        private TemaPaleti AktifTema => Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);

        private Color TemaIkincilYaziRengi(TemaPaleti tema) =>
            KisisellestirmeServisi.ParlakMi(tema.ArkaPlan)
                ? Color.FromArgb(71, 85, 105)
                : Color.FromArgb(148, 163, 184);

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
                _gorselAraSinavGecti = durum.GorselAraSinavGecti;
                _veriTabaniAraSinavGecti = durum.VeriTabaniAraSinavGecti;
                _siberAraSinavGecti = durum.SiberAraSinavGecti;
                _ortaModGecti = durum.OrtaModGecti;
                _veritabaniUyarisiGosterildi = false;
            }
            catch (Exception ex)
            {
                _veritabaniUyarisiGosterildi = true;
                // Veritabanı kapalı veya ayarlar hatalı olduğunda uygulama çökmesin;
                // kullanıcıya anlaşılır Türkçe açıklama ve tek tıkla kurulum sihirbazı sunulsun.
                DialogResult secim = MessageBox.Show(
                    "Kayıtlı ilerleme okunamadı.\n\n" +
                    VeritabaniBaglantisi.KullaniciDostuHataMesaji(ex) +
                    "\n\nPostgreSQL bağlantı ayarlarını şimdi görsel kurulum ekranından düzenlemek ister misiniz?",
                    "PostgreSQL Bağlantı Uyarısı",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (secim == DialogResult.Yes)
                {
                    VeritabaniAyarlariniAc(yenidenYukle: true);
                }
            }
        }

        private void VeritabaniAyarlariniAc(bool yenidenYukle = false)
        {
            using var ayarFormu = new VeritabaniAyarFormu();
            if (ayarFormu.ShowDialog(this) != DialogResult.OK)
                return;

            // Yeni bağlantı bilgileriyle tüm servisleri sıfırdan oluştur.
            _soruServisi = null;
            _ilerlemeServisi = null;
            _karneServisi = null;
            _veritabaniUyarisiGosterildi = false;
            _anaMenuKirli = true;

            if (yenidenYukle)
            {
                KaliciIlerlemeyiYukle();
                _aktifTercih = Kisisellestirme.TercihiYukle();
            }

            if (_pnlAnaMenu.Visible)
                AnaMenuyuCiz();
        }

        private void KaliciIlerlemeyiKaydet()
        {
            // Başarı yüzdeleri/jeton değişti; ana menü bir sonraki gösterimde tazelenir.
            _anaMenuKirli = true;

            try
            {
                IlerlemeDeposu.Kaydet(MevcutIlerleme());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "İlerleme PostgreSQL veritabanına kaydedilemedi.\n\n" +
                    VeritabaniBaglantisi.KullaniciDostuHataMesaji(ex),
                    "Kayıt Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        /// <summary>Formdaki güncel değerlerden kalıcı ilerleme kaydı üretir.</summary>
        private IlerlemeDurumu MevcutIlerleme() => new(
            _gorselBasari,
            _veriTabaniBasari,
            _siberBasari,
            _ipucuJetonu,
            _simulatorOduluAlindi,
            _gorselAraSinavGecti,
            _veriTabaniAraSinavGecti,
            _siberAraSinavGecti,
            _ortaModGecti);

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!SinavdanCikisOnayiAl())
            {
                e.Cancel = true;
                return;
            }

            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _sinavTimer.Stop();
            _sinavTimer.Dispose();
            _ipucuBalonu.Dispose();
            KisisellestirmeTemizle();
            base.OnFormClosed(e);
        }

        private bool SinavdanCikisOnayiAl()
        {
            if (!_sinavDevamEdiyor || !_pnlSinavEkrani.Visible)
                return true;

            DialogResult onay = MessageBox.Show(
                "Devam eden bir sınavınız var. Şimdi çıkarsanız bu oturumdaki cevaplarınız kaydedilmeyecek.\n\n" +
                "Sınavdan çıkmak istediğinize emin misiniz?",
                "Sınavdan Çıkış Onayı",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (onay != DialogResult.Yes)
                return false;

            _sinavDevamEdiyor = false;
            _sinavTimer.Stop();
            return true;
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
            this.Text = $"Kademeli Eğitim Platformu — {Surum}";
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
                Text = "🎓 KADEMELİ EĞİTİM\n    ÇEVRİMDIŞI PLATFORM",
                Font = FontOnbellegi.Al("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = AktifTema.Vurgu,
                Dock = DockStyle.Top,
                Height = 70,
                Tag = "TemaVurgu"
            };

            _btnMenuAna = SidebarButonu("🏠  Kontrol Paneli", () => EkranDegistir("ANA"));
            _btnMenuSinav = SidebarButonu("📝  Karma Sınav", () => SinaviBaslat(null, null, "Karma Değerlendirme Sınavı — Tüm Disiplinler"));
            _btnMenuSimulator = SidebarButonu("🛠️  Kod Simülatörü", SimulatoruAc);
            _btnMenuSoruHavuzu = SidebarButonu("📚  Soru Havuzu", SoruHavuzunuAc);
            _btnMenuKisisellestirme = SidebarButonu("🎨  Profil ve Rozetler", () => EkranDegistir("PROFIL"));

            Label lblAltBilgi = new()
            {
                Text = $"{Surum} • PostgreSQL 18\nkademeli_egitim • BCrypt\n\nMehmet • Ünal\nİsmail Can • Yağız",
                Font = FontOnbellegi.Al("Segoe UI", 8.5f),
                ForeColor = TemaIkincilYaziRengi(AktifTema),
                Dock = DockStyle.Bottom,
                Height = 95,
                Tag = "TemaIkincilYazi"
            };

            _pnlSidebar.Controls.Add(_btnMenuKisisellestirme);
            _pnlSidebar.Controls.Add(_btnMenuSoruHavuzu);
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

        /// <summary>
        /// Zor Mod (kod simülatörü) yalnızca Orta -> Zor Seviye Geçiş Ara Sınavı geçildikten sonra açılır.
        /// Kilitli olduğunda katı engel yerine ilgili Seviye Geçiş Ara Sınavı'nı başlatmayı önerir.
        /// </summary>
        private void SimulatoruAc()
        {
            ZorModuAc(simulatoruAc: true);
        }

        private void SoruHavuzunuAc()
        {
            if (!SinavdanCikisOnayiAl())
                return;

            using var yonetim = new SoruHavuzuYonetimFormu(SoruDeposu);
            yonetim.ShowDialog(this);
            if (yonetim.DegisiklikYapildi)
            {
                _anaMenuKirli = true;
                if (_aktifEkranKodu == "ANA")
                    AnaMenuyuCiz();
            }
        }

        private string _aktifEkranKodu = "ANA";

        private void EkranDegistir(string ekranKodu)
        {
            if (ekranKodu != "SINAV" && !SinavdanCikisOnayiAl())
                return;

            _aktifEkranKodu = ekranKodu;
            _pnlAnaMenu.Visible = ekranKodu == "ANA";
            _pnlSinavEkrani.Visible = ekranKodu == "SINAV";
            _pnlSimulatorVeKarne.Visible = ekranKodu == "SIM";
            _pnlKisisellestirme.Visible = ekranKodu == "PROFIL";

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

            if (ekranKodu == "PROFIL")
            {
                _pnlKisisellestirme.AutoScrollPosition = new Point(0, 0);
                TumProfilVerileriniYenile();
            }

            SidebarAktifButonlariniGuncelle(ekranKodu);
        }

        private void SidebarAktifButonlariniGuncelle(string ekranKodu)
        {
            TemaPaleti tema = Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);
            Color aktifArka = tema.Kart;
            Color pasifArka = tema.Sidebar;
            Color aktifYazi = tema.Vurgu;
            Color pasifYazi = tema.Yazi;

            _btnMenuAna.BackColor = ekranKodu == "ANA" ? aktifArka : pasifArka;
            _btnMenuAna.ForeColor = ekranKodu == "ANA" ? aktifYazi : pasifYazi;

            _btnMenuSinav.BackColor = ekranKodu == "SINAV" ? aktifArka : pasifArka;
            _btnMenuSinav.ForeColor = ekranKodu == "SINAV" ? aktifYazi : pasifYazi;

            _btnMenuSimulator.BackColor = ekranKodu == "SIM" ? aktifArka : pasifArka;
            _btnMenuSimulator.ForeColor = ekranKodu == "SIM" ? aktifYazi : pasifYazi;

            _btnMenuSoruHavuzu.BackColor = pasifArka;
            _btnMenuSoruHavuzu.ForeColor = pasifYazi;

            _btnMenuKisisellestirme.BackColor = ekranKodu == "PROFIL" ? aktifArka : pasifArka;
            _btnMenuKisisellestirme.ForeColor = ekranKodu == "PROFIL" ? aktifYazi : pasifYazi;
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
