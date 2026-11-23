using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using KademeliEgitimPlatformu.Models;
using KademeliEgitimPlatformu.Services;

namespace KademeliEgitimPlatformu
{
    public partial class Form1
    {
        private KisisellestirmeServisi? _kisisellestirmeServisi;
        private KisisellestirmeServisi Kisisellestirme => _kisisellestirmeServisi ??= new KisisellestirmeServisi();

        private Panel _pnlKisisellestirme = new BufferedPanel();
        private Button _btnMenuKisisellestirme = new();

        // Hero Profil Kartı Bileşenleri
        private Panel _pnlProfilOnizleme = new BufferedPanel();
        private Label _lblAvatar = new();
        private Label _lblOgrenciAdi = new();
        private Label _lblUnvanVeHedef = new();
        private Label _lblSeviyeBaslik = new();
        private Label _lblXpDetay = new();
        private Panel _pnlXpBarArka = new();
        private Panel _pnlXpBarDolu = new();
        private Label _lblSeriVeJeton = new();
        private Button _btnProfilDuzenle = new();
        private Button _btnPngIndir = new();
        private double _sonXpDolulukOrani = 0.05;

        // Sol Kolon Bileşenleri
        private TableLayoutPanel _tblDersIstatistikleri = new();
        private FlowLayoutPanel _flpRozetVitrini = new();
        private FlowLayoutPanel _flpTemaButonlari = new();
        private ComboBox _cmbYaziTipi = new();
        private ComboBox _cmbYaziBoyutu = new();
        private CheckBox _chkCerceve = new();
        private Button _btnOzelTemaSil = new();

        // Sağ Kolon Bileşenleri
        private Panel _pnlGelisimKart = new BufferedPanel();
        private Panel _pnlGorevlerKart = new BufferedPanel();
        private Panel _pnlHaftalikGrafik = new BufferedPanel();
        private Panel _pnlAktivitelerKart = new BufferedPanel();

        // Bildirim Toast & Çalışma Süresi Sayacı
        private Panel _pnlBasariBildirimi = new BufferedPanel();
        private Label _lblBildirimMetni = new();
        private System.Windows.Forms.Timer _tmrBildirim = new();
        private System.Windows.Forms.Timer _tmrCalismaSuresi = new();

        private KullaniciTemaTercihi _aktifTercih = new();
        private bool _kisisellestirmeOlaylariBagli;

        private void KisisellestirmeModulunuBagla()
        {
            _aktifTercih = Kisisellestirme.TercihiYukle();

            _pnlKisisellestirme.Dock = DockStyle.Fill;
            _pnlKisisellestirme.AutoScroll = true;
            _pnlKisisellestirme.AutoScrollMargin = new Size(0, 24);
            _pnlKisisellestirme.Visible = false;
            _pnlKisisellestirme.Padding = new Padding(32, 22, 32, 24);
            _pnlKisisellestirme.Tag = "AnaBolum";
            _pnlIcerikAlani.Controls.Add(_pnlKisisellestirme);

            BasariBildirimiHazirla();
            CalismaSuresiSayaciniBaslat();
            KisisellestirmeArayuzunuInsaaEt();
            CanliUygula();
        }

        /// <summary>Form kapanırken zamanlayıcıları durdurup bellekten temizler.</summary>
        private void KisisellestirmeTemizle()
        {
            _tmrBildirim.Stop();
            _tmrBildirim.Dispose();
            _tmrCalismaSuresi.Stop();
            _tmrCalismaSuresi.Dispose();
        }

        /// <summary>
        /// Sınav veya Simülatör modüllerinden çağrıldığında gerçek XP, günlük görev, seri ve aktivite kaydı işler.
        /// </summary>
        public void OyunEtkinligiKaydet(string olayAnahtari, string aciklama, int kazanilanXp, int cozulenSoruSayisi = 0, string? dersKodu = null)
        {
            Kisisellestirme.GunlukDurumuSenkronizeEt(_aktifTercih);

            if (!string.IsNullOrWhiteSpace(olayAnahtari) && !_aktifTercih.OdullendirilenOlaylar.Add(olayAnahtari))
                kazanilanXp = 0;

            _aktifTercih.KazanilanEkstraXp += Math.Max(0, kazanilanXp);
            _aktifTercih.ToplamCozulenSoru += Math.Max(0, cozulenSoruSayisi);

            string bugun = DateTime.Today.ToString("yyyy-MM-dd");
            GunlukPerformans? gunKaydi = _aktifTercih.HaftalikGecmis.FirstOrDefault(g => g.TarihAnahtari == bugun);
            if (gunKaydi != null) gunKaydi.CozulenSoru += Math.Max(0, cozulenSoruSayisi);

            foreach (GunlukGorev gorev in _aktifTercih.GunlukGorevler)
            {
                if (gorev.Kod == "SORU_5" && cozulenSoruSayisi > 0) gorev.Mevcut += cozulenSoruSayisi;
                if (gorev.Kod == "SINAV_1" && dersKodu != "SIM") gorev.Mevcut += 1;
                if (gorev.Kod == "SQL_5" && dersKodu == "SQL") gorev.Mevcut += Math.Max(1, cozulenSoruSayisi);
                if (gorev.Kod == "SIBER_TEST" && dersKodu == "SIBER") gorev.Mevcut += 1;
                if (gorev.Kod == "SIMULATOR_1" && dersKodu == "SIM") gorev.Mevcut += 1;

                if (gorev.TamamlandiMi && !gorev.OdulVerildi)
                {
                    gorev.OdulVerildi = true;
                    _aktifTercih.KazanilanEkstraXp += gorev.XpOdulu;
                    BasariBildirimiGoster($"🎯 Günlük Görev Tamamlandı: {gorev.Baslik} (+{gorev.XpOdulu} XP)");
                }
            }

            _aktifTercih.SonAktiviteler.Insert(0, new AktiviteKaydi
            {
                Tarih = DateTime.Now,
                Aciklama = aciklama,
                KazanilanXp = kazanilanXp
            });
            _aktifTercih.SonAktiviteler = _aktifTercih.SonAktiviteler.Take(8).ToList();

            Kisisellestirme.TercihiKaydet(_aktifTercih);
            TumProfilVerileriniYenile();
        }

        private void CalismaSuresiSayaciniBaslat()
        {
            _tmrCalismaSuresi.Interval = 60000;
            _tmrCalismaSuresi.Tick += (_, _) =>
            {
                if (IsDisposed) return;

                string bugun = DateTime.Today.ToString("yyyy-MM-dd");
                GunlukPerformans? kayit = _aktifTercih.HaftalikGecmis.FirstOrDefault(g => g.TarihAnahtari == bugun);
                if (kayit == null)
                {
                    kayit = new GunlukPerformans { TarihAnahtari = bugun };
                    _aktifTercih.HaftalikGecmis.Add(kayit);
                }
                kayit.CalismaDakikasi += 1;
                _aktifTercih.ToplamCalismaDakikasi += 1;
                Kisisellestirme.TercihiKaydet(_aktifTercih);

                if (_pnlKisisellestirme.Visible)
                {
                    GelisimKartiniGuncelle();
                    _pnlHaftalikGrafik.Invalidate();
                }
            };
            _tmrCalismaSuresi.Start();
        }

        private void BasariBildirimiHazirla()
        {
            _pnlBasariBildirimi.Size = new Size(390, 46);
            _pnlBasariBildirimi.Location = new Point(Math.Max(20, _pnlIcerikAlani.ClientSize.Width - 415), 16);
            _pnlBasariBildirimi.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _pnlBasariBildirimi.Visible = false;
            _pnlBasariBildirimi.Tag = "SabitRenk";
            _pnlBasariBildirimi.BackColor = Color.FromArgb(24, 28, 36);
            _pnlBasariBildirimi.Paint += (_, e) =>
            {
                TemaPaleti tema = Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);
                KisisellestirmeServisi.DuzKartCiz(e.Graphics, _pnlBasariBildirimi.ClientRectangle, tema.Vurgu, 2f);
            };

            _lblBildirimMetni.Dock = DockStyle.Fill;
            _lblBildirimMetni.ForeColor = Color.White;
            _lblBildirimMetni.Font = FontOnbellegi.Al("Segoe UI", 9.5f, FontStyle.Bold);
            _lblBildirimMetni.TextAlign = ContentAlignment.MiddleCenter;
            _lblBildirimMetni.Tag = "SabitRenk";

            _pnlBasariBildirimi.Controls.Add(_lblBildirimMetni);
            _pnlIcerikAlani.Controls.Add(_pnlBasariBildirimi);

            _tmrBildirim.Interval = 4000;
            _tmrBildirim.Tick += (_, _) =>
            {
                _pnlBasariBildirimi.Visible = false;
                _tmrBildirim.Stop();
            };
        }

        private void BasariBildirimiGoster(string mesaj)
        {
            if (IsDisposed) return;

            _lblBildirimMetni.Text = mesaj;
            _pnlBasariBildirimi.Location = new Point(Math.Max(20, _pnlIcerikAlani.ClientSize.Width - _pnlBasariBildirimi.Width - 24), 16);
            _pnlBasariBildirimi.Visible = true;
            _pnlBasariBildirimi.BringToFront();
            _tmrBildirim.Stop();
            _tmrBildirim.Start();
        }

        private void KisisellestirmeArayuzunuInsaaEt()
        {
            _pnlKisisellestirme.SuspendLayout();
            PanelTemizle(_pnlKisisellestirme);

            // Responsive üst kapsayıcı: 1024x700'den 1920x1080'e kadar tüm genişliğe yayılır.
            var anaIzgara = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 3,
                Tag = "AnaBolum"
            };
            anaIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 166F));
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // 1) ÜST BAŞLIK
            var lblBaslik = new Label
            {
                Text = "Profil, Rozetler && Görünüm",
                Font = FontOnbellegi.Al("Segoe UI", 18f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 14)
            };

            // 2) HERO PROFİL KARTI (tam genişlikte responsive)
            _pnlProfilOnizleme = new BufferedPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 16),
                Padding = new Padding(20, 16, 20, 14),
                Tag = "OnizlemeKarti"
            };
            _pnlProfilOnizleme.Paint += (_, e) =>
            {
                TemaPaleti tema = Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);
                _pnlProfilOnizleme.BackColor = tema.Kart;
                Color kenar = _aktifTercih.CerceveGoster ? tema.Vurgu : Color.FromArgb(65, tema.Yazi);
                KisisellestirmeServisi.DuzKartCiz(e.Graphics, _pnlProfilOnizleme.ClientRectangle, kenar, _aktifTercih.CerceveGoster ? 2f : 1f);
            };

            _lblAvatar = new Label
            {
                Size = new Size(64, 64),
                Location = new Point(20, 16),
                Font = FontOnbellegi.Al("Segoe UI Emoji", 28f),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            _lblAvatar.Click += (_, _) => ProfilDuzenlePenceresiAc();

            _lblOgrenciAdi = new Label
            {
                Location = new Point(96, 16),
                Font = FontOnbellegi.Al("Segoe UI", 14f, FontStyle.Bold),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            _lblUnvanVeHedef = new Label
            {
                Location = new Point(98, 44),
                Font = FontOnbellegi.Al("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = true,
                BackColor = Color.Transparent,
                Tag = "SabitRenk"
            };

            _lblSeriVeJeton = new Label
            {
                Location = new Point(98, 67),
                Font = FontOnbellegi.Al("Segoe UI", 9f),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            _btnPngIndir = new Button
            {
                Text = "📸 PNG İndir",
                Size = new Size(116, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Tag = "VurguButon"
            };
            _btnPngIndir.Click += (_, _) => ProfilKartiniPngKaydet();

            _btnProfilDuzenle = new Button
            {
                Text = "✎ Profili Düzenle",
                Size = new Size(140, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnProfilDuzenle.Click += (_, _) => ProfilDuzenlePenceresiAc();

            _lblSeviyeBaslik = new Label
            {
                Location = new Point(22, 98),
                Font = FontOnbellegi.Al("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            _lblXpDetay = new Label
            {
                Size = new Size(340, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = FontOnbellegi.Al("Segoe UI", 8.5f),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            };

            _pnlXpBarArka = new Panel
            {
                Location = new Point(22, 122),
                Height = 10,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(60, 65, 75),
                Tag = "SabitRenk"
            };

            _pnlXpBarDolu = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(120, 10),
                Tag = "SabitRenk"
            };
            _pnlXpBarArka.Controls.Add(_pnlXpBarDolu);

            _pnlProfilOnizleme.Resize += (_, _) => HeroYerlesiminiHizala();
            _pnlProfilOnizleme.Controls.AddRange(new Control[]
            {
                _lblAvatar, _lblOgrenciAdi, _lblUnvanVeHedef, _lblSeriVeJeton,
                _lblSeviyeBaslik, _lblXpDetay, _pnlXpBarArka,
                _btnProfilDuzenle, _btnPngIndir
            });

            // 3) İKİ KOLONLU RESPONSIVE DASHBOARD (%60 SOL / %40 SAĞ)
            var ikiKolon = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0),
                Tag = "AnaBolum"
            };
            ikiKolon.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            ikiKolon.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            // SOL KOLON
            var solKolon = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(0, 0, 16, 0),
                Tag = "AnaBolum"
            };
            solKolon.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            _tblDersIstatistikleri = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 82,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 14),
                Tag = "AnaBolum"
            };
            _tblDersIstatistikleri.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            _tblDersIstatistikleri.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            _tblDersIstatistikleri.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));

            _flpRozetVitrini = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                MinimumSize = new Size(0, 240),
                Margin = new Padding(0, 0, 0, 14),
                Tag = "AnaBolum"
            };

            var pnlAyarKarti = new BufferedPanel
            {
                Dock = DockStyle.Top,
                Height = 344,
                Padding = new Padding(14),
                Margin = new Padding(0, 0, 0, 12)
            };

            _flpTemaButonlari = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 132,
                AutoScroll = true,
                Tag = "AnaBolum"
            };

            var pnlAltAyarlar = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 8, 0, 0),
                Tag = "AnaBolum"
            };

            var lblVurgu = new Label { Text = "Vurgu:", Font = FontOnbellegi.Al("Segoe UI", 9f, FontStyle.Bold), Location = new Point(2, 14), AutoSize = true };
            var flpVurgu = new FlowLayoutPanel { Location = new Point(62, 10), Size = new Size(460, 32), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Tag = "AnaBolum" };
            Color[] vurguRenkleri =
            {
                Color.FromArgb(0, 122, 204), Color.FromArgb(16, 185, 129), Color.FromArgb(56, 189, 248),
                Color.FromArgb(168, 85, 247), Color.FromArgb(245, 158, 11), Color.FromArgb(244, 63, 94),
                Color.FromArgb(236, 72, 153), Color.FromArgb(100, 116, 139)
            };
            foreach (Color r in vurguRenkleri)
            {
                Color secilen = r;
                var btnR = new Button { Size = new Size(25, 25), BackColor = secilen, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 7, 0), Cursor = Cursors.Hand, Tag = "SabitRenk" };
                btnR.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
                btnR.Click += (_, _) => { _aktifTercih.VurguHex = KisisellestirmeServisi.ColorToHex(secilen); CanliUygula(); };
                flpVurgu.Controls.Add(btnR);
            }

            var lblFont = new Label { Text = "Font:", Font = FontOnbellegi.Al("Segoe UI", 9f, FontStyle.Bold), Location = new Point(2, 54), AutoSize = true };
            _cmbYaziTipi = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbYaziTipi.SetBounds(62, 50, 165, 28);
            foreach (string f in Kisisellestirme.YaziTipleri) _cmbYaziTipi.Items.Add(f);
            _cmbYaziTipi.SelectedIndex = Math.Max(0, Kisisellestirme.YaziTipleri.IndexOf(_aktifTercih.YaziTipi));
            _cmbYaziTipi.SelectedIndexChanged += (_, _) =>
            {
                _aktifTercih.YaziTipi = _cmbYaziTipi.SelectedItem?.ToString() ?? "Segoe UI";
                CanliUygula();
            };

            var lblBoyut = new Label { Text = "Boyut:", Font = FontOnbellegi.Al("Segoe UI", 9f, FontStyle.Bold), Location = new Point(242, 54), AutoSize = true };
            _cmbYaziBoyutu = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbYaziBoyutu.SetBounds(295, 50, 115, 28);
            _cmbYaziBoyutu.Items.AddRange(new object[] { "Küçük", "Normal", "Büyük" });
            _cmbYaziBoyutu.SelectedItem = _aktifTercih.YaziBoyutuModu;
            if (_cmbYaziBoyutu.SelectedIndex < 0) _cmbYaziBoyutu.SelectedIndex = 1;
            _cmbYaziBoyutu.SelectedIndexChanged += (_, _) =>
            {
                _aktifTercih.YaziBoyutuModu = _cmbYaziBoyutu.SelectedItem?.ToString() ?? "Normal";
                CanliUygula();
            };

            _chkCerceve = new CheckBox
            {
                Text = "Profil kartında vurgu çerçevesi göster",
                Checked = _aktifTercih.CerceveGoster,
                AutoSize = true,
                Location = new Point(2, 92)
            };
            _chkCerceve.CheckedChanged += (_, _) => { _aktifTercih.CerceveGoster = _chkCerceve.Checked; _pnlProfilOnizleme.Invalidate(); };

            var btnKaydet = new Button { Text = "💾 Kaydet", Location = new Point(2, 134), Size = new Size(135, 34), FlatStyle = FlatStyle.Flat, Tag = "VurguButon", Cursor = Cursors.Hand };
            btnKaydet.Click += (_, _) =>
            {
                Kisisellestirme.TercihiKaydet(_aktifTercih);
                BasariBildirimiGoster("💾 Tüm profil ve görünüm tercihleri kaydedildi!");
            };

            var btnSifirla = new Button { Text = "↺ Sıfırla", Location = new Point(146, 134), Size = new Size(115, 34), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnSifirla.Click += (_, _) =>
            {
                _aktifTercih.TemaAdi = "Koyu Klasik";
                _aktifTercih.VurguHex = null;
                _aktifTercih.YaziTipi = "Segoe UI";
                _aktifTercih.YaziBoyutuModu = "Normal";
                _aktifTercih.CerceveGoster = true;
                _cmbYaziTipi.SelectedIndex = 0;
                _cmbYaziBoyutu.SelectedIndex = 1;
                _chkCerceve.Checked = true;
                Kisisellestirme.TercihiKaydet(_aktifTercih);
                CanliUygula();
            };

            _btnOzelTemaSil = new Button
            {
                Text = "🗑️ Temayı Sil",
                Location = new Point(270, 134),
                Size = new Size(125, 34),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnOzelTemaSil.Click += (_, _) =>
            {
                _aktifTercih.OzelPaletler.RemoveAll(p => p.Ad.Equals(_aktifTercih.TemaAdi, StringComparison.OrdinalIgnoreCase));
                _aktifTercih.TemaAdi = "Koyu Klasik";
                _aktifTercih.VurguHex = null;
                Kisisellestirme.TercihiKaydet(_aktifTercih);
                CanliUygula();
            };

            pnlAltAyarlar.Controls.AddRange(new Control[]
            {
                lblVurgu, flpVurgu, lblFont, _cmbYaziTipi,
                lblBoyut, _cmbYaziBoyutu, _chkCerceve, btnKaydet, btnSifirla, _btnOzelTemaSil
            });

            pnlAyarKarti.Controls.Add(pnlAltAyarlar);
            pnlAyarKarti.Controls.Add(_flpTemaButonlari);

            solKolon.Controls.Add(BolumBasligiOlustur("Başarı İstatistikleri"), 0, 0);
            solKolon.Controls.Add(_tblDersIstatistikleri, 0, 1);
            solKolon.Controls.Add(BolumBasligiOlustur("Rozet Koleksiyonum (Ünvan takmak için tıklayın)"), 0, 2);
            solKolon.Controls.Add(_flpRozetVitrini, 0, 3);
            solKolon.Controls.Add(BolumBasligiOlustur("Görünüm && Renk Temaları (50 Seçenek)"), 0, 4);
            solKolon.Controls.Add(pnlAyarKarti, 0, 5);

            // SAĞ KOLON
            var sagKolon = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 8,
                Padding = new Padding(8, 0, 0, 0),
                Tag = "AnaBolum"
            };
            sagKolon.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            _pnlGelisimKart = new BufferedPanel { Dock = DockStyle.Top, Height = 168, Margin = new Padding(0, 0, 0, 14), Padding = new Padding(10) };
            _pnlGorevlerKart = new BufferedPanel { Dock = DockStyle.Top, Height = 152, Margin = new Padding(0, 0, 0, 14), Padding = new Padding(12) };
            _pnlHaftalikGrafik = new BufferedPanel { Dock = DockStyle.Top, Height = 150, Margin = new Padding(0, 0, 0, 14) };
            _pnlHaftalikGrafik.Paint += (_, e) => HaftalikGrafikCiz(e.Graphics, _pnlHaftalikGrafik.ClientRectangle);
            _pnlAktivitelerKart = new BufferedPanel { Dock = DockStyle.Top, Height = 172, Padding = new Padding(12) };

            sagKolon.Controls.Add(BolumBasligiOlustur("Gelişimim"), 0, 0);
            sagKolon.Controls.Add(_pnlGelisimKart, 0, 1);
            sagKolon.Controls.Add(BolumBasligiOlustur("🎯 Bugünün Görevleri"), 0, 2);
            sagKolon.Controls.Add(_pnlGorevlerKart, 0, 3);
            sagKolon.Controls.Add(BolumBasligiOlustur("📊 Haftalık Performans (Pzt - Paz)"), 0, 4);
            sagKolon.Controls.Add(_pnlHaftalikGrafik, 0, 5);
            sagKolon.Controls.Add(BolumBasligiOlustur("🕒 Son Aktiviteler"), 0, 6);
            sagKolon.Controls.Add(_pnlAktivitelerKart, 0, 7);

            ikiKolon.Controls.Add(solKolon, 0, 0);
            ikiKolon.Controls.Add(sagKolon, 1, 0);

            anaIzgara.Controls.Add(lblBaslik, 0, 0);
            anaIzgara.Controls.Add(_pnlProfilOnizleme, 0, 1);
            anaIzgara.Controls.Add(ikiKolon, 0, 2);

            _pnlKisisellestirme.Controls.Add(anaIzgara);
            _pnlKisisellestirme.ResumeLayout(true);

            HeroYerlesiminiHizala();
            TumProfilVerileriniYenile();
            _kisisellestirmeOlaylariBagli = true;
        }

        private void HeroYerlesiminiHizala()
        {
            int genislik = Math.Max(520, _pnlProfilOnizleme.ClientSize.Width);
            _btnPngIndir.Location = new Point(genislik - _btnPngIndir.Width - 20, 16);
            _btnProfilDuzenle.Location = new Point(_btnPngIndir.Left - _btnProfilDuzenle.Width - 10, 16);
            _lblXpDetay.Location = new Point(genislik - _lblXpDetay.Width - 22, 98);

            int barGenislik = Math.Max(200, genislik - 44);
            _pnlXpBarArka.Width = barGenislik;
            _pnlXpBarDolu.Width = Math.Clamp((int)(barGenislik * _sonXpDolulukOrani), 8, barGenislik);
        }

        private static Label BolumBasligiOlustur(string metin) => new()
        {
            Text = metin,
            Font = FontOnbellegi.Al("Segoe UI", 11.5f, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6)
        };

        private void TumProfilVerileriniYenile()
        {
            if (!_kisisellestirmeOlaylariBagli && _pnlProfilOnizleme.Controls.Count == 0)
                return;

            List<RozetBilgisi> rozetler = Kisisellestirme.RozetleriHesapla(
                _aktifTercih, _gorselBasari, _veriTabaniBasari, _siberBasari, _ortaModGecti, _simulatorOduluAlindi);

            foreach (RozetBilgisi r in rozetler.Where(x => x.KazanildiMi))
            {
                string anahtar = $"{r.Kod}_{r.Kademe}";
                if (_aktifTercih.BildirilenRozetler.Add(anahtar))
                {
                    Kisisellestirme.TercihiKaydet(_aktifTercih);
                    BasariBildirimiGoster($"🏆 Yeni Rozet Kazanıldı: {r.KategoriIkon} {r.SeriAdi} ({r.KademeAdi})!");
                }
            }

            HeroKartiniGuncelle(rozetler);
            DersIstatistikleriniGuncelle();
            RozetVitrininiGuncelle(rozetler);
            TemaButonlariniYenile();
            GelisimKartiniGuncelle();
            GorevlerKartiniGuncelle();
            _pnlHaftalikGrafik.Invalidate();
            AktivitelerKartiniGuncelle();
        }

        private void HeroKartiniGuncelle(List<RozetBilgisi> rozetler)
        {
            TemaPaleti tema = Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);
            var xpBilgi = Kisisellestirme.XpVeSeviyeHesapla(
                _aktifTercih, _gorselBasari, _veriTabaniBasari, _siberBasari, _ortaModGecti, _simulatorOduluAlindi);

            List<RozetBilgisi> kazanilmislar = rozetler.Where(r => r.KazanildiMi).ToList();
            string aktifUnvan = "Yeni Başlayan";
            Color unvanRengi = tema.Vurgu;

            if (!string.IsNullOrWhiteSpace(_aktifTercih.SeciliUnvan) && kazanilmislar.Any(r => r.Unvan == _aktifTercih.SeciliUnvan))
            {
                RozetBilgisi seciliRozet = kazanilmislar.First(r => r.Unvan == _aktifTercih.SeciliUnvan);
                aktifUnvan = $"{seciliRozet.Ikon} {seciliRozet.Unvan}";
                unvanRengi = seciliRozet.KademeRengi;
            }
            else if (kazanilmislar.Count > 0)
            {
                RozetBilgisi enYuksek = kazanilmislar.OrderByDescending(r => r.Kademe).First();
                aktifUnvan = $"{enYuksek.Ikon} {enYuksek.Unvan}";
                unvanRengi = enYuksek.KademeRengi;
            }

            _lblAvatar.Text = _aktifTercih.Avatar;
            _lblOgrenciAdi.Text = $"{_aktifTercih.OgrenciAdi}   •   {Surum}";
            _lblUnvanVeHedef.Text = $"{aktifUnvan}   ·   🎯 {_aktifTercih.KisiselHedef}";
            _lblUnvanVeHedef.ForeColor = unvanRengi;

            _lblSeriVeJeton.Text = $"🔥 {_aktifTercih.GunlukSeri} günlük seri   •   💡 {_ipucuJetonu} ipucu jetonu   •   ⚡ Toplam {xpBilgi.ToplamXp} XP";

            _lblSeviyeBaslik.Text = $"Seviye {xpBilgi.Seviye:00}  •  {xpBilgi.SeviyeUnvani}";
            int kalanXp = xpBilgi.SonrakiSeviyeGereken - xpBilgi.MevcutSeviyeXp;
            _lblXpDetay.Text = $"{xpBilgi.MevcutSeviyeXp} / {xpBilgi.SonrakiSeviyeGereken} XP  ({kalanXp} XP sonra: {xpBilgi.SonrakiSeviyeUnvani})";

            _sonXpDolulukOrani = (double)xpBilgi.MevcutSeviyeXp / Math.Max(1, xpBilgi.SonrakiSeviyeGereken);
            _pnlXpBarDolu.BackColor = tema.Vurgu;
            HeroYerlesiminiHizala();
            _pnlProfilOnizleme.Invalidate();
        }

        private void DersIstatistikleriniGuncelle()
        {
            _tblDersIstatistikleri.SuspendLayout();
            PanelTemizle(_tblDersIstatistikleri);
            TemaPaleti tema = Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);

            (string Ad, int Yuzde, Color Renk)[] dersler =
            {
                ("Görsel Prog.", _gorselBasari, Color.FromArgb(0, 175, 160)),
                ("Veri Tabanları", _veriTabaniBasari, Color.FromArgb(245, 165, 25)),
                ("Siber Güvenlik", _siberBasari, Color.FromArgb(215, 70, 165))
            };

            for (int i = 0; i < dersler.Length; i++)
            {
                var d = dersler[i];
                var kart = new BufferedPanel
                {
                    Dock = DockStyle.Fill,
                    BackColor = tema.Kart,
                    Margin = new Padding(0, 0, i < dersler.Length - 1 ? 10 : 0, 0),
                    Padding = new Padding(12, 8, 12, 10),
                    Tag = "SabitRenk"
                };
                kart.Paint += (_, e) => KisisellestirmeServisi.DuzKartCiz(e.Graphics, kart.ClientRectangle, Color.FromArgb(65, tema.Yazi), 1f);

                var lblYuzde = new Label { Text = $"%{d.Yuzde}", Font = FontOnbellegi.Al("Segoe UI", 14f, FontStyle.Bold), ForeColor = d.Renk, Dock = DockStyle.Top, Height = 28, Tag = "SabitRenk" };
                var lblAd = new Label { Text = d.Ad, Font = FontOnbellegi.Al("Segoe UI", 8.5f), ForeColor = tema.Yazi, Dock = DockStyle.Top, Height = 20, Tag = "SabitRenk" };

                int yuzdeDeger = Math.Clamp(d.Yuzde, 0, 100);
                var barArka = new Panel { Dock = DockStyle.Bottom, Height = 6, BackColor = Color.FromArgb(55, tema.Yazi), Tag = "SabitRenk" };
                barArka.Paint += (_, e) =>
                {
                    int w = barArka.Width * yuzdeDeger / 100;
                    if (w > 0)
                    {
                        using var firca = new SolidBrush(d.Renk);
                        e.Graphics.FillRectangle(firca, 0, 0, w, barArka.Height);
                    }
                };

                kart.Controls.Add(barArka);
                kart.Controls.Add(lblAd);
                kart.Controls.Add(lblYuzde);
                _tblDersIstatistikleri.Controls.Add(kart, i, 0);
            }

            _tblDersIstatistikleri.ResumeLayout(true);
        }

        private void RozetVitrininiGuncelle(List<RozetBilgisi> rozetler)
        {
            _flpRozetVitrini.SuspendLayout();
            PanelTemizle(_flpRozetVitrini);
            TemaPaleti tema = Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);

            foreach (RozetBilgisi rozet in rozetler)
            {
                bool takiliMi = rozet.KazanildiMi && _aktifTercih.SeciliUnvan == rozet.Unvan;
                var kart = new BufferedPanel
                {
                    Size = new Size(142, 118),
                    BackColor = tema.Kart,
                    Margin = new Padding(0, 0, 10, 10),
                    Cursor = Cursors.Hand,
                    Tag = "SabitRenk"
                };

                kart.Paint += (_, e) =>
                {
                    Color kenar = takiliMi ? tema.Vurgu : (rozet.KazanildiMi ? rozet.KademeRengi : Color.FromArgb(65, tema.Yazi));
                    KisisellestirmeServisi.DuzKartCiz(e.Graphics, kart.ClientRectangle, kenar, takiliMi ? 2.5f : (rozet.KazanildiMi ? 1.8f : 1f));
                };

                var lblIkon = new Label
                {
                    Text = rozet.KazanildiMi ? rozet.KategoriIkon : "🔒",
                    Font = FontOnbellegi.Al("Segoe UI Emoji", 18f),
                    Location = new Point(8, 8),
                    Size = new Size(42, 34),
                    ForeColor = rozet.KazanildiMi ? tema.Yazi : Color.FromArgb(130, tema.Yazi),
                    Tag = "SabitRenk"
                };

                var lblKademe = new Label
                {
                    Text = takiliMi ? "★ TAKILI" : rozet.KademeAdi,
                    Font = FontOnbellegi.Al("Segoe UI", 7.5f, FontStyle.Bold),
                    ForeColor = takiliMi ? tema.Vurgu : rozet.KademeRengi,
                    Location = new Point(54, 16),
                    AutoSize = true,
                    Tag = "SabitRenk"
                };

                var lblSeri = new Label
                {
                    Text = rozet.SeriAdi,
                    Font = FontOnbellegi.Al("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = rozet.KazanildiMi ? tema.Yazi : Color.FromArgb(150, tema.Yazi),
                    Location = new Point(10, 46),
                    AutoSize = true,
                    Tag = "SabitRenk"
                };

                var lblUnvan = new Label
                {
                    Text = rozet.Unvan,
                    Font = FontOnbellegi.Al("Segoe UI", 8f),
                    ForeColor = rozet.KazanildiMi ? rozet.KademeRengi : Color.FromArgb(125, tema.Yazi),
                    Location = new Point(10, 67),
                    Size = new Size(122, 18),
                    AutoEllipsis = true,
                    Tag = "SabitRenk"
                };

                var barArka = new Panel { Location = new Point(10, 94), Size = new Size(122, 4), BackColor = Color.FromArgb(55, tema.Yazi), Tag = "SabitRenk" };
                var barDolu = new Panel { Location = new Point(0, 0), Size = new Size(Math.Clamp(rozet.IlerlemeYuzdesi, 0, 100) * 122 / 100, 4), BackColor = rozet.KazanildiMi ? rozet.KademeRengi : tema.Vurgu, Tag = "SabitRenk" };
                barArka.Controls.Add(barDolu);

                void RozetTiklandi(object? s, EventArgs e)
                {
                    if (rozet.KazanildiMi)
                    {
                        _aktifTercih.SeciliUnvan = rozet.Unvan;
                        Kisisellestirme.TercihiKaydet(_aktifTercih);
                        BasariBildirimiGoster($"{rozet.Ikon} '{rozet.Unvan}' ünvanı profilinize takıldı!");
                        TumProfilVerileriniYenile();
                    }
                    else
                    {
                        MessageBox.Show(
                            $"{rozet.KategoriIkon} {rozet.SeriAdi} ({rozet.Aciklama})\n\nNasıl Kazanılır:\n{rozet.NasilKazanilir}",
                            "Kilitli Rozet Bilgisi",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                }

                kart.Click += RozetTiklandi;
                lblIkon.Click += RozetTiklandi;
                lblKademe.Click += RozetTiklandi;
                lblSeri.Click += RozetTiklandi;
                lblUnvan.Click += RozetTiklandi;

                kart.Controls.AddRange(new Control[] { lblIkon, lblKademe, lblSeri, lblUnvan, barArka });
                _flpRozetVitrini.Controls.Add(kart);
            }

            _flpRozetVitrini.ResumeLayout(true);
        }

        private void GelisimKartiniGuncelle()
        {
            _pnlGelisimKart.SuspendLayout();
            PanelTemizle(_pnlGelisimKart);
            TemaPaleti tema = Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);

            int gercekSoru = Math.Max(_aktifTercih.ToplamCozulenSoru, (_gorselBasari + _veriTabaniBasari + _siberBasari) / 5);
            string sureMetni = _aktifTercih.ToplamCalismaDakikasi >= 60
                ? $"{_aktifTercih.ToplamCalismaDakikasi / 60} sa {_aktifTercih.ToplamCalismaDakikasi % 60} dk"
                : $"{_aktifTercih.ToplamCalismaDakikasi} dk";

            string enGuclu = "Henüz Yok";
            int maxPuan = Math.Max(_gorselBasari, Math.Max(_veriTabaniBasari, _siberBasari));
            if (maxPuan > 0)
            {
                if (_gorselBasari == maxPuan) enGuclu = $"Görsel Prog. (%{maxPuan})";
                else if (_veriTabaniBasari == maxPuan) enGuclu = $"Veri Tabanları (%{maxPuan})";
                else enGuclu = $"Siber Güv. (%{maxPuan})";
            }

            int hedefYuzde = Math.Min(100, gercekSoru * 100 / Math.Max(1, _aktifTercih.HedefSoruSayisi));

            (string Etiket, string Deger)[] kutular =
            {
                ("Çözülen Soru", $"{gercekSoru} / {_aktifTercih.HedefSoruSayisi}"),
                ("Çalışma Süresi", sureMetni),
                ("En Güçlü Alan", enGuclu),
                ("Hedef İlerlemesi", $"%{hedefYuzde} Tamamlandı")
            };

            var izgara = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Tag = "AnaBolum"
            };
            izgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            izgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            izgara.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            izgara.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            for (int i = 0; i < kutular.Length; i++)
            {
                var mini = new Panel
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(4),
                    Padding = new Padding(10, 8, 8, 6),
                    BackColor = tema.ArkaPlan,
                    Tag = "SabitRenk"
                };

                var lblDeger = new Label { Text = kutular[i].Deger, Font = FontOnbellegi.Al("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = tema.Vurgu, Dock = DockStyle.Top, Height = 24, AutoEllipsis = true, Tag = "SabitRenk" };
                var lblEtiket = new Label { Text = kutular[i].Etiket, Font = FontOnbellegi.Al("Segoe UI", 8.5f), ForeColor = Color.FromArgb(180, tema.Yazi), Dock = DockStyle.Fill, Tag = "SabitRenk" };

                mini.Controls.Add(lblEtiket);
                mini.Controls.Add(lblDeger);
                izgara.Controls.Add(mini, i % 2, i / 2);
            }

            _pnlGelisimKart.Controls.Add(izgara);
            _pnlGelisimKart.ResumeLayout(true);
        }

        private void GorevlerKartiniGuncelle()
        {
            _pnlGorevlerKart.SuspendLayout();
            PanelTemizle(_pnlGorevlerKart);
            TemaPaleti tema = Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);

            var izgara = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = _aktifTercih.GunlukGorevler.Count + 1,
                Tag = "AnaBolum"
            };
            izgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 78F));
            izgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));

            int satir = 0;
            foreach (GunlukGorev g in _aktifTercih.GunlukGorevler)
            {
                izgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
                var lblDurum = new Label
                {
                    Text = g.TamamlandiMi ? $"✅ {g.Baslik}" : $"⬜ {g.Baslik} ({g.Mevcut}/{g.Hedef})",
                    Font = FontOnbellegi.Al("Segoe UI", 9f, g.TamamlandiMi ? FontStyle.Strikeout : FontStyle.Regular),
                    ForeColor = g.TamamlandiMi ? Color.FromArgb(140, tema.Yazi) : tema.Yazi,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoEllipsis = true,
                    Tag = "SabitRenk"
                };

                var lblXp = new Label
                {
                    Text = $"+{g.XpOdulu} XP",
                    Font = FontOnbellegi.Al("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = tema.Vurgu,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleRight,
                    Tag = "SabitRenk"
                };

                izgara.Controls.Add(lblDurum, 0, satir);
                izgara.Controls.Add(lblXp, 1, satir);
                satir++;
            }

            izgara.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            int tamamlanan = _aktifTercih.GunlukGorevler.Count(x => x.TamamlandiMi);
            var lblOzet = new Label
            {
                Text = $"Tamamlanan görev: {tamamlanan} / {_aktifTercih.GunlukGorevler.Count}",
                Font = FontOnbellegi.Al("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, tema.Yazi),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                Tag = "SabitRenk"
            };
            izgara.Controls.Add(lblOzet, 0, satir);
            izgara.SetColumnSpan(lblOzet, 2);

            _pnlGorevlerKart.Controls.Add(izgara);
            _pnlGorevlerKart.ResumeLayout(true);
        }

        private void HaftalikGrafikCiz(Graphics g, Rectangle alan)
        {
            if (alan.Width < 120 || alan.Height < 80)
                return;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            TemaPaleti tema = Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);

            int gunFarki = ((int)DateTime.Today.DayOfWeek + 6) % 7;
            DateTime pazartesi = DateTime.Today.AddDays(-gunFarki);
            string[] gunAdlari = { "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz" };

            int[] dakikalar = new int[7];
            for (int i = 0; i < 7; i++)
            {
                string anahtar = pazartesi.AddDays(i).ToString("yyyy-MM-dd");
                dakikalar[i] = _aktifTercih.HaftalikGecmis.FirstOrDefault(x => x.TarihAnahtari == anahtar)?.CalismaDakikasi ?? 0;
            }

            int maks = Math.Max(30, dakikalar.Max());
            int kenarBosluk = 22;
            int kullanilabilirGenislik = alan.Width - (kenarBosluk * 2);
            int aralik = Math.Max(32, kullanilabilirGenislik / 7);
            int barGenislik = Math.Clamp(aralik - 16, 18, 36);
            int baslangicX = kenarBosluk + (kullanilabilirGenislik - (aralik * 7) + (aralik - barGenislik)) / 2;
            int tabanY = alan.Height - 30;
            int maksYukseklik = Math.Max(40, tabanY - 32);

            using var yaziFirca = new SolidBrush(Color.FromArgb(190, tema.Yazi));
            using var barFirca = new SolidBrush(tema.Vurgu);
            using var bosFirca = new SolidBrush(Color.FromArgb(40, tema.Yazi));
            Font font = FontOnbellegi.Al("Segoe UI", 8f);

            for (int i = 0; i < 7; i++)
            {
                int x = baslangicX + i * aralik;
                g.FillRectangle(bosFirca, x, tabanY - maksYukseklik, barGenislik, maksYukseklik);

                int h = dakikalar[i] > 0 ? Math.Max(6, dakikalar[i] * maksYukseklik / maks) : 0;
                if (h > 0)
                    g.FillRectangle(barFirca, x, tabanY - h, barGenislik, h);

                g.DrawString(gunAdlari[i], font, yaziFirca, x + 2, tabanY + 6);
                if (dakikalar[i] > 0)
                    g.DrawString($"{dakikalar[i]}d", font, yaziFirca, x, tabanY - h - 16);
            }
        }

        private void AktivitelerKartiniGuncelle()
        {
            _pnlAktivitelerKart.SuspendLayout();
            PanelTemizle(_pnlAktivitelerKart);
            TemaPaleti tema = Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);

            var izgara = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Tag = "AnaBolum"
            };

            int satir = 0;
            foreach (AktiviteKaydi akt in _aktifTercih.SonAktiviteler.Take(4))
            {
                izgara.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
                string zaman = KisisellestirmeServisi.GoreceliTarihFormatla(akt.Tarih);
                string xpMetni = akt.KazanilanXp > 0 ? $" (+{akt.KazanilanXp} XP)" : "";
                var lbl = new Label
                {
                    Text = $"• {zaman} – {akt.Aciklama}{xpMetni}",
                    Font = FontOnbellegi.Al("Segoe UI", 8.5f),
                    ForeColor = tema.Yazi,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoEllipsis = true,
                    Tag = "SabitRenk"
                };
                izgara.Controls.Add(lbl, 0, satir++);
            }

            _pnlAktivitelerKart.Controls.Add(izgara);
            _pnlAktivitelerKart.ResumeLayout(true);
        }

        private void ProfilDuzenlePenceresiAc()
        {
            TemaPaleti tema = Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);

            using var dlg = new Form
            {
                Text = "Profili Düzenle",
                Size = new Size(420, 365),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = tema.ArkaPlan,
                ForeColor = tema.Yazi
            };

            var lblAd = new Label { Text = "Öğrenci Adı:", Location = new Point(20, 18), AutoSize = true };
            var txtAd = new TextBox { Text = _aktifTercih.OgrenciAdi, Location = new Point(20, 40), Size = new Size(360, 26), BackColor = tema.Kart, ForeColor = tema.Yazi };

            var lblHedef = new Label { Text = "Kişisel Hedef / Başlık:", Location = new Point(20, 76), AutoSize = true };
            var txtHedef = new TextBox { Text = _aktifTercih.KisiselHedef, Location = new Point(20, 98), Size = new Size(360, 26), BackColor = tema.Kart, ForeColor = tema.Yazi };

            var lblSoruHedef = new Label { Text = "Soru Çözme Hedefi:", Location = new Point(20, 134), AutoSize = true };
            var numSoru = new NumericUpDown
            {
                Minimum = 10,
                Maximum = 2000,
                Increment = 10,
                Value = Math.Clamp(_aktifTercih.HedefSoruSayisi, 10, 2000),
                Location = new Point(155, 131),
                Size = new Size(90, 26),
                BackColor = tema.Kart,
                ForeColor = tema.Yazi
            };

            var lblAvatarSec = new Label { Text = "Avatar Seç:", Location = new Point(20, 168), AutoSize = true };
            var flpAvatarlar = new FlowLayoutPanel { Location = new Point(20, 190), Size = new Size(365, 76) };

            string secilenAvatar = _aktifTercih.Avatar;
            foreach (string av in Kisisellestirme.AvatarSecenekleri)
            {
                string secenek = av;
                var btnAv = new Button
                {
                    Text = secenek,
                    Size = new Size(38, 32),
                    Font = FontOnbellegi.Al("Segoe UI Emoji", 12f),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = secenek == secilenAvatar ? tema.Vurgu : tema.Kart,
                    ForeColor = tema.Yazi,
                    Margin = new Padding(0, 0, 5, 5),
                    Cursor = Cursors.Hand
                };
                btnAv.Click += (_, _) =>
                {
                    secilenAvatar = secenek;
                    foreach (Button b in flpAvatarlar.Controls)
                        b.BackColor = b.Text == secilenAvatar ? tema.Vurgu : tema.Kart;
                };
                flpAvatarlar.Controls.Add(btnAv);
            }

            var btnKaydet = new Button
            {
                Text = "Kaydet",
                Location = new Point(270, 280),
                Size = new Size(110, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = tema.Vurgu,
                ForeColor = KisisellestirmeServisi.ParlakMi(tema.Vurgu) ? Color.Black : Color.White,
                DialogResult = DialogResult.OK
            };

            dlg.Controls.AddRange(new Control[] { lblAd, txtAd, lblHedef, txtHedef, lblSoruHedef, numSoru, lblAvatarSec, flpAvatarlar, btnKaydet });
            dlg.AcceptButton = btnKaydet;

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                if (!string.IsNullOrWhiteSpace(txtAd.Text)) _aktifTercih.OgrenciAdi = txtAd.Text.Trim();
                if (!string.IsNullOrWhiteSpace(txtHedef.Text)) _aktifTercih.KisiselHedef = txtHedef.Text.Trim();
                _aktifTercih.HedefSoruSayisi = (int)numSoru.Value;
                _aktifTercih.Avatar = secilenAvatar;

                Kisisellestirme.TercihiKaydet(_aktifTercih);
                TumProfilVerileriniYenile();
                BasariBildirimiGoster("✏️ Profil bilgileriniz güncellendi!");
            }
        }

        private void ProfilKartiniPngKaydet()
        {
            string guvenliAd = DosyaAdiTemizle(_aktifTercih.OgrenciAdi);
            string masaustu = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            using var sfd = new SaveFileDialog
            {
                Title = "Profil Kartını PNG Olarak Kaydet",
                Filter = "PNG Görseli (*.png)|*.png",
                DefaultExt = "png",
                AddExtension = true,
                FileName = $"{guvenliAd}_ProfilKarti.png",
                InitialDirectory = Directory.Exists(masaustu) ? masaustu : VeriYolu.VeriKlasoru
            };

            if (sfd.ShowDialog(this) != DialogResult.OK) return;

            _btnProfilDuzenle.Visible = false;
            _btnPngIndir.Visible = false;
            _pnlProfilOnizleme.Refresh();

            try
            {
                using var bmp = new Bitmap(Math.Max(1, _pnlProfilOnizleme.Width), Math.Max(1, _pnlProfilOnizleme.Height));
                _pnlProfilOnizleme.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                bmp.Save(sfd.FileName, ImageFormat.Png);
                BasariBildirimiGoster("📸 Profil kartı PNG olarak kaydedildi!");
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(
                    "Seçilen klasöre yazma izni yok. Lütfen Masaüstü veya Belgelerim klasörünü seçin.\n\n" + ex.Message,
                    "Erişim Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Profil kartı PNG olarak kaydedilemedi.\n\n" + ex.Message,
                    "Kaydetme Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _btnProfilDuzenle.Visible = true;
                _btnPngIndir.Visible = true;
            }
        }

        private static string DosyaAdiTemizle(string hamAd)
        {
            char[] gecersizler = Path.GetInvalidFileNameChars();
            string temiz = new string((hamAd ?? "Ogrenci")
                .Trim()
                .Select(c => c == ' ' || gecersizler.Contains(c) ? '_' : c)
                .ToArray());

            return string.IsNullOrWhiteSpace(temiz) ? "Ogrenci" : temiz;
        }

        private void TemaButonlariniYenile()
        {
            _flpTemaButonlari.SuspendLayout();
            PanelTemizle(_flpTemaButonlari);
            List<TemaPaleti> temalar = Kisisellestirme.TumTemalariGetir(_aktifTercih);
            TemaPaleti aktifTema = Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);
            _btnOzelTemaSil.Visible = aktifTema.OzelMi;

            foreach (TemaPaleti tema in temalar)
            {
                TemaPaleti secenek = tema;
                bool secili = secenek.Ad.Equals(_aktifTercih.TemaAdi, StringComparison.OrdinalIgnoreCase);
                var btn = new Button
                {
                    Size = new Size(162, 34),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = secenek.Kart,
                    ForeColor = secenek.Yazi,
                    Text = $"● {secenek.Ad}",
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(8, 0, 4, 0),
                    Margin = new Padding(0, 0, 6, 6),
                    Cursor = Cursors.Hand,
                    Tag = "SabitRenk"
                };

                btn.FlatAppearance.BorderColor = secili ? secenek.Vurgu : Color.FromArgb(95, 95, 105);
                btn.FlatAppearance.BorderSize = secili ? 2 : 1;

                btn.Paint += (_, e) =>
                {
                    using var firca = new SolidBrush(secenek.Vurgu);
                    e.Graphics.FillEllipse(firca, 10, 11, 11, 11);
                };

                btn.Click += (_, _) =>
                {
                    _aktifTercih.TemaAdi = secenek.Ad;
                    _aktifTercih.VurguHex = null;
                    CanliUygula();
                };

                _flpTemaButonlari.Controls.Add(btn);
            }

            var btnYeniRenk = new Button
            {
                Size = new Size(162, 34),
                FlatStyle = FlatStyle.Flat,
                Text = "➕ Yeni Renk Ekle",
                Margin = new Padding(0, 0, 6, 6),
                Cursor = Cursors.Hand,
                Tag = "VurguButon"
            };
            btnYeniRenk.Click += (_, _) => YeniRenkPaletiEkleDialog();
            _flpTemaButonlari.Controls.Add(btnYeniRenk);

            _flpTemaButonlari.ResumeLayout(true);
        }

        private void YeniRenkPaletiEkleDialog()
        {
            using var renkSecici = new ColorDialog { FullOpen = true, Color = Color.FromArgb(0, 150, 220) };
            if (renkSecici.ShowDialog() != DialogResult.OK) return;

            using var pencere = new Form
            {
                Text = "Yeni Renk Teması",
                Size = new Size(340, 195),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(32, 34, 38),
                ForeColor = Color.White
            };

            var lblAd = new Label { Text = "Tema Adı:", Location = new Point(18, 20), AutoSize = true };
            var txtAd = new TextBox { Text = $"Özel Tema {_aktifTercih.OzelPaletler.Count + 1}", Location = new Point(105, 17), Size = new Size(200, 26), BackColor = Color.FromArgb(48, 51, 56), ForeColor = Color.White };
            var rbKoyu = new RadioButton { Text = "Koyu Zemin", Location = new Point(20, 62), AutoSize = true, Checked = true };
            var rbAcik = new RadioButton { Text = "Açık Zemin", Location = new Point(140, 62), AutoSize = true };
            var btnOlustur = new Button
            {
                Text = "Ekle ve Uygula",
                Location = new Point(185, 108),
                Size = new Size(120, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = renkSecici.Color,
                ForeColor = KisisellestirmeServisi.ParlakMi(renkSecici.Color) ? Color.Black : Color.White,
                DialogResult = DialogResult.OK
            };

            pencere.Controls.AddRange(new Control[] { lblAd, txtAd, rbKoyu, rbAcik, btnOlustur });
            pencere.AcceptButton = btnOlustur;

            if (pencere.ShowDialog(this) == DialogResult.OK)
            {
                string ad = string.IsNullOrWhiteSpace(txtAd.Text) ? $"Özel Tema {_aktifTercih.OzelPaletler.Count + 1}" : txtAd.Text.Trim();
                OzelRenkPaleti yeniPalet = KisisellestirmeServisi.TekRenktenPaletUret(ad, renkSecici.Color, rbKoyu.Checked);
                _aktifTercih.OzelPaletler.RemoveAll(p => p.Ad.Equals(ad, StringComparison.OrdinalIgnoreCase));
                _aktifTercih.OzelPaletler.Add(yeniPalet);
                _aktifTercih.TemaAdi = ad;
                _aktifTercih.VurguHex = null;

                Kisisellestirme.TercihiKaydet(_aktifTercih);
                CanliUygula();
            }
        }

        private void CanliUygula()
        {
            Kisisellestirme.TemayiFormUzerineUygula(this, _pnlSidebar, _pnlIcerikAlani, _pnlKisisellestirme, _aktifTercih);
            SidebarAktifButonlariniGuncelle(_aktifEkranKodu);
            TumProfilVerileriniYenile();
        }
    }
}
