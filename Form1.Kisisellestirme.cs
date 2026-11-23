using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
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

        // Sol Kolon Bileşenleri
        private FlowLayoutPanel _flpDersIstatistikleri = new();
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

        private void KisisellestirmeModulunuBagla()
        {
            _aktifTercih = Kisisellestirme.TercihiYukle();

            _btnMenuKisisellestirme.Text = "🎨 Profil && Rozetler";
            _btnMenuKisisellestirme.FlatStyle = _btnMenuSimulator.FlatStyle;
            _btnMenuKisisellestirme.FlatAppearance.BorderSize = _btnMenuSimulator.FlatAppearance.BorderSize;
            _btnMenuKisisellestirme.BackColor = _btnMenuSimulator.BackColor;
            _btnMenuKisisellestirme.ForeColor = _btnMenuSimulator.ForeColor;
            _btnMenuKisisellestirme.Font = _btnMenuSimulator.Font;
            _btnMenuKisisellestirme.TextAlign = _btnMenuSimulator.TextAlign;
            _btnMenuKisisellestirme.Padding = _btnMenuSimulator.Padding;
            _btnMenuKisisellestirme.Cursor = Cursors.Hand;

            if (_btnMenuSimulator.Parent is FlowLayoutPanel flp)
            {
                _btnMenuKisisellestirme.Size = _btnMenuSimulator.Size;
                _btnMenuKisisellestirme.Margin = _btnMenuSimulator.Margin;
                flp.Controls.Add(_btnMenuKisisellestirme);
            }
            else if (_btnMenuSimulator.Parent != null)
            {
                _btnMenuKisisellestirme.SetBounds(
                    _btnMenuSimulator.Left,
                    _btnMenuSimulator.Bottom + 8,
                    _btnMenuSimulator.Width,
                    _btnMenuSimulator.Height);
                _btnMenuSimulator.Parent.Controls.Add(_btnMenuKisisellestirme);
            }

            _btnMenuAna.Click += (_, _) => _pnlKisisellestirme.Visible = false;
            _btnMenuSinav.Click += (_, _) => _pnlKisisellestirme.Visible = false;
            _btnMenuSimulator.Click += (_, _) => _pnlKisisellestirme.Visible = false;
            _btnMenuKisisellestirme.Click += (_, _) => KisisellestirmeEkraniniAc();

            _pnlKisisellestirme.Dock = DockStyle.Fill;
            _pnlKisisellestirme.AutoScroll = true;
            _pnlKisisellestirme.AutoScrollMargin = new Size(0, 28);
            _pnlKisisellestirme.Visible = false;
            _pnlKisisellestirme.Padding = new Padding(0);
            _pnlKisisellestirme.Tag = "AnaBolum";
            _pnlIcerikAlani.Controls.Add(_pnlKisisellestirme);

            BasariBildirimiHazirla();
            CalismaSuresiSayaciniBaslat();
            KisisellestirmeArayuzunuInsaaEt();
            Kisisellestirme.TemayiFormUzerineUygula(this, _pnlSidebar, _pnlIcerikAlani, _aktifTercih);
        }

        /// <summary>
        /// Sınav veya Simülatör modüllerinden çağrıldığında gerçek XP, görev, seri ve aktivite kaydı işler.
        /// </summary>
        public void OyunEtkinligiKaydet(string olayAnahtari, string aciklama, int kazanilanXp, int cozulenSoruSayisi = 0, string? dersKodu = null)
        {
            Kisisellestirme.GunlukDurumuSenkronizeEt(_aktifTercih);

            if (!string.IsNullOrWhiteSpace(olayAnahtari) && !_aktifTercih.OdullendirilenOlaylar.Add(olayAnahtari))
                kazanilanXp = 0;

            _aktifTercih.KazanilanEkstraXp += kazanilanXp;
            _aktifTercih.ToplamCozulenSoru += cozulenSoruSayisi;

            string bugun = DateTime.Today.ToString("yyyy-MM-dd");
            var gunKaydi = _aktifTercih.HaftalikGecmis.FirstOrDefault(g => g.TarihAnahtari == bugun);
            if (gunKaydi != null) gunKaydi.CozulenSoru += cozulenSoruSayisi;

            foreach (var gorev in _aktifTercih.GunlukGorevler)
            {
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
                string bugun = DateTime.Today.ToString("yyyy-MM-dd");
                var kayit = _aktifTercih.HaftalikGecmis.FirstOrDefault(g => g.TarihAnahtari == bugun);
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
            _pnlBasariBildirimi.Size = new Size(370, 46);
            _pnlBasariBildirimi.Location = new Point(Math.Max(20, _pnlIcerikAlani.ClientSize.Width - 395), 16);
            _pnlBasariBildirimi.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _pnlBasariBildirimi.Visible = false;
            _pnlBasariBildirimi.Tag = "SabitRenk";
            _pnlBasariBildirimi.BackColor = Color.FromArgb(24, 28, 36);
            _pnlBasariBildirimi.Paint += (_, e) =>
            {
                var tema = Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);
                KisisellestirmeServisi.DuzKartCiz(e.Graphics, _pnlBasariBildirimi.ClientRectangle, tema.Vurgu, 2f);
            };

            _lblBildirimMetni.Dock = DockStyle.Fill;
            _lblBildirimMetni.ForeColor = Color.White;
            _lblBildirimMetni.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
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
            _lblBildirimMetni.Text = mesaj;
            _pnlBasariBildirimi.Location = new Point(Math.Max(20, _pnlIcerikAlani.ClientSize.Width - _pnlBasariBildirimi.Width - 24), 16);
            _pnlBasariBildirimi.Visible = true;
            _pnlBasariBildirimi.BringToFront();
            _tmrBildirim.Stop();
            _tmrBildirim.Start();
        }

        private void KisisellestirmeEkraniniAc()
        {
            foreach (Control ctrl in _pnlIcerikAlani.Controls)
            {
                ctrl.Visible = ctrl == _pnlKisisellestirme;
            }
            _pnlKisisellestirme.AutoScrollPosition = new Point(0, 0);
            TumProfilVerileriniYenile();
            _pnlKisisellestirme.BringToFront();
        }

        private void KisisellestirmeArayuzunuInsaaEt()
        {
            _pnlKisisellestirme.Controls.Clear();

            var anaAkis = new FlowLayoutPanel
            {
                Location = new Point(26, 18),
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Tag = "AnaBolum"
            };

            // Üst Başlık
            var lblBaslik = new Label
            {
                Text = "Profil, Rozetler && Görünüm",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12)
            };
            anaAkis.Controls.Add(lblBaslik);

            // 1. ÜST PROFİL KARTI
            _pnlProfilOnizleme.Size = new Size(940, 146);
            _pnlProfilOnizleme.Margin = new Padding(0, 0, 0, 18);
            _pnlProfilOnizleme.Tag = "OnizlemeKarti";
            _pnlProfilOnizleme.Paint += (_, e) =>
            {
                var tema = Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);
                _pnlProfilOnizleme.BackColor = tema.Kart;
                Color kenar = _aktifTercih.CerceveGoster ? tema.Vurgu : Color.FromArgb(65, tema.Yazi);
                KisisellestirmeServisi.DuzKartCiz(e.Graphics, _pnlProfilOnizleme.ClientRectangle, kenar, _aktifTercih.CerceveGoster ? 2f : 1f);
            };

            _lblAvatar.Size = new Size(64, 64);
            _lblAvatar.Location = new Point(20, 18);
            _lblAvatar.Font = new Font("Segoe UI Emoji", 28f);
            _lblAvatar.TextAlign = ContentAlignment.MiddleCenter;
            _lblAvatar.BackColor = Color.Transparent;
            _lblAvatar.Cursor = Cursors.Hand;
            _lblAvatar.Click += (_, _) => ProfilDuzenlePenceresiAc();

            _lblOgrenciAdi.Location = new Point(94, 18);
            _lblOgrenciAdi.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            _lblOgrenciAdi.AutoSize = true;
            _lblOgrenciAdi.BackColor = Color.Transparent;

            _lblUnvanVeHedef.Location = new Point(96, 46);
            _lblUnvanVeHedef.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            _lblUnvanVeHedef.AutoSize = true;
            _lblUnvanVeHedef.BackColor = Color.Transparent;
            _lblUnvanVeHedef.Tag = "SabitRenk";

            _lblSeriVeJeton.Location = new Point(96, 69);
            _lblSeriVeJeton.Font = new Font("Segoe UI", 9f);
            _lblSeriVeJeton.AutoSize = true;
            _lblSeriVeJeton.BackColor = Color.Transparent;

            _lblSeviyeBaslik.Location = new Point(22, 96);
            _lblSeviyeBaslik.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            _lblSeviyeBaslik.AutoSize = true;
            _lblSeviyeBaslik.BackColor = Color.Transparent;

            _lblXpDetay.Location = new Point(620, 96);
            _lblXpDetay.Size = new Size(298, 20);
            _lblXpDetay.Font = new Font("Segoe UI", 8.5f);
            _lblXpDetay.TextAlign = ContentAlignment.MiddleRight;
            _lblXpDetay.BackColor = Color.Transparent;

            _pnlXpBarArka.Location = new Point(22, 120);
            _pnlXpBarArka.Size = new Size(896, 10);
            _pnlXpBarArka.BackColor = Color.FromArgb(60, 65, 75);
            _pnlXpBarArka.Tag = "SabitRenk";

            _pnlXpBarDolu.Location = new Point(0, 0);
            _pnlXpBarDolu.Size = new Size(120, 10);
            _pnlXpBarDolu.Tag = "SabitRenk";
            _pnlXpBarArka.Controls.Add(_pnlXpBarDolu);

            _btnProfilDuzenle.Text = "✎ Profili Düzenle";
            _btnProfilDuzenle.SetBounds(668, 18, 132, 32);
            _btnProfilDuzenle.FlatStyle = FlatStyle.Flat;
            _btnProfilDuzenle.Cursor = Cursors.Hand;
            _btnProfilDuzenle.Click += (_, _) => ProfilDuzenlePenceresiAc();

            _btnPngIndir.Text = "📸 PNG İndir";
            _btnPngIndir.SetBounds(808, 18, 110, 32);
            _btnPngIndir.FlatStyle = FlatStyle.Flat;
            _btnPngIndir.Cursor = Cursors.Hand;
            _btnPngIndir.Tag = "VurguButon";
            _btnPngIndir.Click += (_, _) => ProfilKartiniPngKaydet();

            _pnlProfilOnizleme.Controls.Clear();
            _pnlProfilOnizleme.Controls.AddRange(new Control[]
            {
                _lblAvatar, _lblOgrenciAdi, _lblUnvanVeHedef, _lblSeriVeJeton,
                _lblSeviyeBaslik, _lblXpDetay, _pnlXpBarArka,
                _btnProfilDuzenle, _btnPngIndir
            });
            anaAkis.Controls.Add(_pnlProfilOnizleme);

            // 2. İKİ KOLONLU ANA DASHBOARD ALANI
            var ikiKolon = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                Size = new Size(940, 820),
                Margin = new Padding(0),
                Tag = "AnaBolum"
            };
            ikiKolon.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 575f));
            ikiKolon.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 365f));

            var solKolon = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 0, 15, 0),
                Tag = "AnaBolum"
            };

            var sagKolon = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(5, 0, 0, 0),
                Tag = "AnaBolum"
            };

            // --- SOL KOLON 1: Başarı İstatistikleri (3 Ders Tek Satırda Tam Hizalı) ---
            solKolon.Controls.Add(BolumBasligiOlustur("Başarı İstatistikleri"));
            _flpDersIstatistikleri.Size = new Size(560, 76);
            _flpDersIstatistikleri.WrapContents = false;
            _flpDersIstatistikleri.Margin = new Padding(0, 0, 0, 14);
            _flpDersIstatistikleri.Tag = "AnaBolum";
            solKolon.Controls.Add(_flpDersIstatistikleri);

            // --- SOL KOLON 2: Rozet Koleksiyonum & Ünvan Vitrini ---
            solKolon.Controls.Add(BolumBasligiOlustur("Rozet Koleksiyonum (Ünvan takmak için tıklayın)"));
            _flpRozetVitrini.Size = new Size(565, 250);
            _flpRozetVitrini.Margin = new Padding(0, 0, 0, 14);
            _flpRozetVitrini.Tag = "AnaBolum";
            solKolon.Controls.Add(_flpRozetVitrini);

            // --- SOL KOLON 3: Görünüm & 50 Renk Teması ---
            solKolon.Controls.Add(BolumBasligiOlustur("Görünüm && Renk Temaları (50 Seçenek)"));
            var pnlAyarKarti = new Panel
            {
                Size = new Size(558, 340),
                Padding = new Padding(14)
            };

            _flpTemaButonlari.Location = new Point(14, 14);
            _flpTemaButonlari.Size = new Size(530, 126);
            _flpTemaButonlari.AutoScroll = true;
            _flpTemaButonlari.Tag = "AnaBolum";

            var lblVurgu = new Label { Text = "Vurgu:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(14, 154), AutoSize = true };
            var flpVurgu = new FlowLayoutPanel { Location = new Point(72, 149), Size = new Size(470, 32), Tag = "AnaBolum" };
            Color[] vurguRenkleri =
            {
                Color.FromArgb(0, 122, 204), Color.FromArgb(16, 185, 129), Color.FromArgb(56, 189, 248),
                Color.FromArgb(168, 85, 247), Color.FromArgb(245, 158, 11), Color.FromArgb(244, 63, 94),
                Color.FromArgb(236, 72, 153), Color.FromArgb(100, 116, 139)
            };
            foreach (var r in vurguRenkleri)
            {
                var btnR = new Button { Size = new Size(25, 25), BackColor = r, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 7, 0), Cursor = Cursors.Hand, Tag = "SabitRenk" };
                btnR.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
                btnR.Click += (_, _) => { _aktifTercih.VurguHex = KisisellestirmeServisi.ColorToHex(r); CanliUygula(); };
                flpVurgu.Controls.Add(btnR);
            }

            var lblFont = new Label { Text = "Font:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(14, 194), AutoSize = true };
            _cmbYaziTipi.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbYaziTipi.SetBounds(72, 190, 165, 28);
            _cmbYaziTipi.Items.Clear();
            foreach (var f in Kisisellestirme.YaziTipleri) _cmbYaziTipi.Items.Add(f);
            _cmbYaziTipi.SelectedIndex = Math.Max(0, Kisisellestirme.YaziTipleri.IndexOf(_aktifTercih.YaziTipi));
            _cmbYaziTipi.SelectedIndexChanged += (_, _) =>
            {
                _aktifTercih.YaziTipi = _cmbYaziTipi.SelectedItem?.ToString() ?? "Segoe UI";
                CanliUygula();
            };

            var lblBoyut = new Label { Text = "Boyut:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(252, 194), AutoSize = true };
            _cmbYaziBoyutu.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbYaziBoyutu.SetBounds(305, 190, 115, 28);
            _cmbYaziBoyutu.Items.Clear();
            _cmbYaziBoyutu.Items.AddRange(new object[] { "Küçük", "Normal", "Büyük" });
            _cmbYaziBoyutu.SelectedItem = _aktifTercih.YaziBoyutuModu;
            if (_cmbYaziBoyutu.SelectedIndex < 0) _cmbYaziBoyutu.SelectedIndex = 1;
            _cmbYaziBoyutu.SelectedIndexChanged += (_, _) =>
            {
                _aktifTercih.YaziBoyutuModu = _cmbYaziBoyutu.SelectedItem?.ToString() ?? "Normal";
                CanliUygula();
            };

            _chkCerceve.Text = "Profil kartında vurgu çerçevesi göster";
            _chkCerceve.Checked = _aktifTercih.CerceveGoster;
            _chkCerceve.AutoSize = true;
            _chkCerceve.Location = new Point(14, 232);
            _chkCerceve.CheckedChanged += (_, _) => { _aktifTercih.CerceveGoster = _chkCerceve.Checked; _pnlProfilOnizleme.Invalidate(); };

            var btnKaydet = new Button { Text = "💾 Kaydet", Location = new Point(14, 274), Size = new Size(135, 34), FlatStyle = FlatStyle.Flat, Tag = "VurguButon", Cursor = Cursors.Hand };
            btnKaydet.Click += (_, _) =>
            {
                Kisisellestirme.TercihiKaydet(_aktifTercih);
                BasariBildirimiGoster("💾 Tüm profil ve görünüm tercihleri kaydedildi!");
            };

            var btnSifirla = new Button { Text = "↺ Sıfırla", Location = new Point(158, 274), Size = new Size(115, 34), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
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

            _btnOzelTemaSil.Text = "🗑️ Temayı Sil";
            _btnOzelTemaSil.Location = new Point(282, 274);
            _btnOzelTemaSil.Size = new Size(125, 34);
            _btnOzelTemaSil.FlatStyle = FlatStyle.Flat;
            _btnOzelTemaSil.Cursor = Cursors.Hand;
            _btnOzelTemaSil.Visible = false;
            _btnOzelTemaSil.Click += (_, _) =>
            {
                _aktifTercih.OzelPaletler.RemoveAll(p => p.Ad.Equals(_aktifTercih.TemaAdi, StringComparison.OrdinalIgnoreCase));
                _aktifTercih.TemaAdi = "Koyu Klasik";
                _aktifTercih.VurguHex = null;
                Kisisellestirme.TercihiKaydet(_aktifTercih);
                CanliUygula();
            };

            pnlAyarKarti.Controls.AddRange(new Control[]
            {
                _flpTemaButonlari, lblVurgu, flpVurgu, lblFont, _cmbYaziTipi,
                lblBoyut, _cmbYaziBoyutu, _chkCerceve, btnKaydet, btnSifirla, _btnOzelTemaSil
            });
            solKolon.Controls.Add(pnlAyarKarti);

            // --- SAĞ KOLON 1: Gelişimim ---
            sagKolon.Controls.Add(BolumBasligiOlustur("Gelişimim"));
            _pnlGelisimKart.Size = new Size(355, 168);
            _pnlGelisimKart.Margin = new Padding(0, 0, 0, 14);
            sagKolon.Controls.Add(_pnlGelisimKart);

            // --- SAĞ KOLON 2: Bugünün Görevleri ---
            sagKolon.Controls.Add(BolumBasligiOlustur("🎯 Bugünün Görevleri"));
            _pnlGorevlerKart.Size = new Size(355, 148);
            _pnlGorevlerKart.Margin = new Padding(0, 0, 0, 14);
            sagKolon.Controls.Add(_pnlGorevlerKart);

            // --- SAĞ KOLON 3: Haftalık Çalışma Performansı ---
            sagKolon.Controls.Add(BolumBasligiOlustur("📊 Haftalık Performans (Pzt - Paz)"));
            _pnlHaftalikGrafik.Size = new Size(355, 145);
            _pnlHaftalikGrafik.Margin = new Padding(0, 0, 0, 14);
            _pnlHaftalikGrafik.Paint += (_, e) => HaftalikGrafikCiz(e.Graphics, _pnlHaftalikGrafik.ClientRectangle);
            sagKolon.Controls.Add(_pnlHaftalikGrafik);

            // --- SAĞ KOLON 4: Son Aktiviteler ---
            sagKolon.Controls.Add(BolumBasligiOlustur("🕒 Son Aktiviteler"));
            _pnlAktivitelerKart.Size = new Size(355, 168);
            sagKolon.Controls.Add(_pnlAktivitelerKart);

            ikiKolon.Controls.Add(solKolon, 0, 0);
            ikiKolon.Controls.Add(sagKolon, 1, 0);
            anaAkis.Controls.Add(ikiKolon);

            _pnlKisisellestirme.Controls.Add(anaAkis);
            TumProfilVerileriniYenile();
        }

        private static Label BolumBasligiOlustur(string metin) => new()
        {
            Text = metin,
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6)
        };

        private void TumProfilVerileriniYenile()
        {
            var rozetler = Kisisellestirme.RozetleriHesapla(
                _aktifTercih, _gorselBasari, _veriTabaniBasari, _siberBasari, _ortaModGecti, _simulatorOduluAlindi);

            foreach (var r in rozetler.Where(x => x.KazanildiMi))
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
            var tema = Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);
            var xpBilgi = Kisisellestirme.XpVeSeviyeHesapla(
                _aktifTercih, _gorselBasari, _veriTabaniBasari, _siberBasari, _ortaModGecti, _simulatorOduluAlindi);

            var kazanilmislar = rozetler.Where(r => r.KazanildiMi).ToList();
            string aktifUnvan = "Yeni Başlayan";
            Color unvanRengi = tema.Vurgu;

            if (!string.IsNullOrWhiteSpace(_aktifTercih.SeciliUnvan) && kazanilmislar.Any(r => r.Unvan == _aktifTercih.SeciliUnvan))
            {
                var seciliRozet = kazanilmislar.First(r => r.Unvan == _aktifTercih.SeciliUnvan);
                aktifUnvan = $"{seciliRozet.Ikon} {seciliRozet.Unvan}";
                unvanRengi = seciliRozet.KademeRengi;
            }
            else if (kazanilmislar.Count > 0)
            {
                var enYuksek = kazanilmislar.OrderByDescending(r => r.Kademe).First();
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

            int doluluk = Math.Clamp(xpBilgi.MevcutSeviyeXp * 896 / xpBilgi.SonrakiSeviyeGereken, 8, 896);
            _pnlXpBarDolu.Size = new Size(doluluk, 10);
            _pnlXpBarDolu.BackColor = tema.Vurgu;

            _pnlProfilOnizleme.Invalidate();
        }

        private void DersIstatistikleriniGuncelle()
        {
            _flpDersIstatistikleri.Controls.Clear();
            var tema = Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);

            (string Ad, int Yuzde, Color Renk)[] dersler =
            {
                ("Görsel Prog.", _gorselBasari, Color.FromArgb(0, 175, 160)),
                ("Veri Tabanı", _veriTabaniBasari, Color.FromArgb(245, 165, 25)),
                ("Siber Güvenlik", _siberBasari, Color.FromArgb(215, 70, 165))
            };

            for (int i = 0; i < dersler.Length; i++)
            {
                var d = dersler[i];
                int sagBosluk = (i < dersler.Length - 1) ? 9 : 0;

                var kart = new Panel
                {
                    Size = new Size(179, 72),
                    BackColor = tema.Kart,
                    Margin = new Padding(0, 0, sagBosluk, 0),
                    Tag = "SabitRenk"
                };
                kart.Paint += (_, e) => KisisellestirmeServisi.DuzKartCiz(e.Graphics, kart.ClientRectangle, Color.FromArgb(65, tema.Yazi), 1f);

                var lblYuzde = new Label { Text = $"%{d.Yuzde}", Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = d.Renk, Location = new Point(12, 8), AutoSize = true, Tag = "SabitRenk" };
                var lblAd = new Label { Text = d.Ad, Font = new Font("Segoe UI", 8.5f), ForeColor = tema.Yazi, Location = new Point(12, 36), AutoSize = true, Tag = "SabitRenk" };

                var barArka = new Panel { Location = new Point(12, 58), Size = new Size(155, 5), BackColor = Color.FromArgb(55, tema.Yazi), Tag = "SabitRenk" };
                var barDolu = new Panel { Location = new Point(0, 0), Size = new Size(Math.Clamp(d.Yuzde, 0, 100) * 155 / 100, 5), BackColor = d.Renk, Tag = "SabitRenk" };
                barArka.Controls.Add(barDolu);

                kart.Controls.AddRange(new Control[] { lblYuzde, lblAd, barArka });
                _flpDersIstatistikleri.Controls.Add(kart);
            }
        }

        private void RozetVitrininiGuncelle(List<RozetBilgisi> rozetler)
        {
            _flpRozetVitrini.Controls.Clear();
            var tema = Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);

            foreach (var rozet in rozetler)
            {
                bool takiliMi = rozet.KazanildiMi && _aktifTercih.SeciliUnvan == rozet.Unvan;
                var kart = new Panel
                {
                    Size = new Size(132, 118),
                    BackColor = tema.Kart,
                    Margin = new Padding(0, 0, 9, 8),
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
                    Font = new Font("Segoe UI Emoji", 18f),
                    Location = new Point(8, 8),
                    Size = new Size(42, 34),
                    ForeColor = rozet.KazanildiMi ? tema.Yazi : Color.FromArgb(130, tema.Yazi),
                    Tag = "SabitRenk"
                };

                var lblKademe = new Label
                {
                    Text = takiliMi ? "★ TAKILI" : rozet.KademeAdi,
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    ForeColor = takiliMi ? tema.Vurgu : rozet.KademeRengi,
                    Location = new Point(52, 16),
                    AutoSize = true,
                    Tag = "SabitRenk"
                };

                var lblSeri = new Label
                {
                    Text = rozet.SeriAdi,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = rozet.KazanildiMi ? tema.Yazi : Color.FromArgb(150, tema.Yazi),
                    Location = new Point(10, 46),
                    AutoSize = true,
                    Tag = "SabitRenk"
                };

                var lblUnvan = new Label
                {
                    Text = rozet.Unvan,
                    Font = new Font("Segoe UI", 8f),
                    ForeColor = rozet.KazanildiMi ? rozet.KademeRengi : Color.FromArgb(125, tema.Yazi),
                    Location = new Point(10, 67),
                    Size = new Size(114, 18),
                    Tag = "SabitRenk"
                };

                var barArka = new Panel { Location = new Point(10, 92), Size = new Size(112, 4), BackColor = Color.FromArgb(55, tema.Yazi), Tag = "SabitRenk" };
                var barDolu = new Panel { Location = new Point(0, 0), Size = new Size(Math.Clamp(rozet.IlerlemeYuzdesi, 0, 100) * 112 / 100, 4), BackColor = rozet.KazanildiMi ? rozet.KademeRengi : tema.Vurgu, Tag = "SabitRenk" };
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
                lblSeri.Click += RozetTiklandi;
                lblUnvan.Click += RozetTiklandi;

                kart.Controls.AddRange(new Control[] { lblIkon, lblKademe, lblSeri, lblUnvan, barArka });
                _flpRozetVitrini.Controls.Add(kart);
            }
        }

        private void GelisimKartiniGuncelle()
        {
            _pnlGelisimKart.Controls.Clear();
            var tema = Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);

            int gercekSoru = Math.Max(_aktifTercih.ToplamCozulenSoru, (_gorselBasari + _veriTabaniBasari + _siberBasari) / 5);
            string sureMetni = _aktifTercih.ToplamCalismaDakikasi >= 60
                ? $"{_aktifTercih.ToplamCalismaDakikasi / 60} sa {_aktifTercih.ToplamCalismaDakikasi % 60} dk"
                : $"{_aktifTercih.ToplamCalismaDakikasi} dk";

            string enGuclu = "Henüz Yok";
            int maxPuan = Math.Max(_gorselBasari, Math.Max(_veriTabaniBasari, _siberBasari));
            if (maxPuan > 0)
            {
                if (_gorselBasari == maxPuan) enGuclu = $"Görsel Prog. (%{maxPuan})";
                else if (_veriTabaniBasari == maxPuan) enGuclu = $"Veri Tabanı (%{maxPuan})";
                else enGuclu = $"Siber Güv. (%{maxPuan})";
            }

            int hedefYuzde = Math.Min(100, gercekSoru * 100 / Math.Max(1, _aktifTercih.HedefSoruSayisi));

            (string Etiket, string Deger)[] kutular =
            {
                ("Çözülen Soru", $"{gercekSoru} / {_aktifTercih.HedefSoruSayisi}"),
                ("Çalışma Süresi", sureMetni),
                ("En Güçlü Ders", enGuclu),
                ("Hedef İlerlemesi", $"%{hedefYuzde} Tamamlandı")
            };

            for (int i = 0; i < kutular.Length; i++)
            {
                int col = i % 2;
                int row = i / 2;
                var mini = new Panel
                {
                    Location = new Point(12 + col * 168, 12 + row * 74),
                    Size = new Size(160, 66),
                    BackColor = tema.ArkaPlan,
                    Tag = "SabitRenk"
                };

                var lblDeger = new Label { Text = kutular[i].Deger, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = tema.Vurgu, Location = new Point(10, 10), AutoSize = true, Tag = "SabitRenk" };
                var lblEtiket = new Label { Text = kutular[i].Etiket, Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(180, tema.Yazi), Location = new Point(10, 36), AutoSize = true, Tag = "SabitRenk" };

                mini.Controls.AddRange(new Control[] { lblDeger, lblEtiket });
                _pnlGelisimKart.Controls.Add(mini);
            }
        }

        private void GorevlerKartiniGuncelle()
        {
            _pnlGorevlerKart.Controls.Clear();
            var tema = Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);

            int y = 12;
            foreach (var g in _aktifTercih.GunlukGorevler)
            {
                var lblDurum = new Label
                {
                    Text = g.TamamlandiMi ? $"✅ {g.Baslik}" : $"⬜ {g.Baslik} ({g.Mevcut}/{g.Hedef})",
                    Font = new Font("Segoe UI", 9f, g.TamamlandiMi ? FontStyle.Strikeout : FontStyle.Regular),
                    ForeColor = g.TamamlandiMi ? Color.FromArgb(140, tema.Yazi) : tema.Yazi,
                    Location = new Point(14, y),
                    AutoSize = true,
                    Tag = "SabitRenk"
                };

                var lblXp = new Label
                {
                    Text = $"+{g.XpOdulu} XP",
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = tema.Vurgu,
                    Location = new Point(282, y),
                    AutoSize = true,
                    Tag = "SabitRenk"
                };

                _pnlGorevlerKart.Controls.AddRange(new Control[] { lblDurum, lblXp });
                y += 32;
            }

            int tamamlanan = _aktifTercih.GunlukGorevler.Count(x => x.TamamlandiMi);
            var lblOzet = new Label
            {
                Text = $"Tamamlanan görev: {tamamlanan} / {_aktifTercih.GunlukGorevler.Count}",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, tema.Yazi),
                Location = new Point(14, 116),
                AutoSize = true,
                Tag = "SabitRenk"
            };
            _pnlGorevlerKart.Controls.Add(lblOzet);
        }

        private void HaftalikGrafikCiz(Graphics g, Rectangle alan)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var tema = Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);

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
            int barGenislik = 26;
            int aralik = 46;
            int baslangicX = 22;
            int tabanY = 114;
            int maksYukseklik = 76;

            using var yaziFirca = new SolidBrush(Color.FromArgb(190, tema.Yazi));
            using var barFirca = new SolidBrush(tema.Vurgu);
            using var bosFirca = new SolidBrush(Color.FromArgb(40, tema.Yazi));
            using var font = new Font("Segoe UI", 8f);

            for (int i = 0; i < 7; i++)
            {
                int x = baslangicX + i * aralik;
                g.FillRectangle(bosFirca, x, tabanY - maksYukseklik, barGenislik, maksYukseklik);

                int h = dakikalar[i] > 0 ? Math.Max(6, dakikalar[i] * maksYukseklik / maks) : 0;
                if (h > 0)
                    g.FillRectangle(barFirca, x, tabanY - h, barGenislik, h);

                g.DrawString(gunAdlari[i], font, yaziFirca, x + 2, tabanY + 5);
                if (dakikalar[i] > 0)
                    g.DrawString($"{dakikalar[i]}d", font, yaziFirca, x, tabanY - h - 16);
            }
        }

        private void AktivitelerKartiniGuncelle()
        {
            _pnlAktivitelerKart.Controls.Clear();
            var tema = Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);

            int y = 12;
            foreach (var akt in _aktifTercih.SonAktiviteler.Take(4))
            {
                string zaman = KisisellestirmeServisi.GoreceliTarihFormatla(akt.Tarih);
                string xpMetni = akt.KazanilanXp > 0 ? $" (+{akt.KazanilanXp} XP)" : "";
                var lbl = new Label
                {
                    Text = $"• {zaman} – {akt.Aciklama}{xpMetni}",
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = tema.Yazi,
                    Location = new Point(12, y),
                    Size = new Size(330, 32),
                    Tag = "SabitRenk"
                };
                _pnlAktivitelerKart.Controls.Add(lbl);
                y += 37;
            }
        }

        private void ProfilDuzenlePenceresiAc()
        {
            var tema = Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);

            using var dlg = new Form
            {
                Text = "Profili Düzenle",
                Size = new Size(410, 355),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = tema.ArkaPlan,
                ForeColor = tema.Yazi
            };

            var lblAd = new Label { Text = "Öğrenci Adı:", Location = new Point(20, 18), AutoSize = true };
            var txtAd = new TextBox { Text = _aktifTercih.OgrenciAdi, Location = new Point(20, 40), Size = new Size(350, 26), BackColor = tema.Kart, ForeColor = tema.Yazi };

            var lblHedef = new Label { Text = "Kişisel Hedef / Başlık:", Location = new Point(20, 76), AutoSize = true };
            var txtHedef = new TextBox { Text = _aktifTercih.KisiselHedef, Location = new Point(20, 98), Size = new Size(350, 26), BackColor = tema.Kart, ForeColor = tema.Yazi };

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
            var flpAvatarlar = new FlowLayoutPanel { Location = new Point(20, 190), Size = new Size(355, 72) };

            string secilenAvatar = _aktifTercih.Avatar;
            foreach (var av in Kisisellestirme.AvatarSecenekleri)
            {
                var btnAv = new Button
                {
                    Text = av,
                    Size = new Size(38, 32),
                    Font = new Font("Segoe UI Emoji", 12f),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = av == secilenAvatar ? tema.Vurgu : tema.Kart,
                    ForeColor = tema.Yazi,
                    Margin = new Padding(0, 0, 5, 5),
                    Cursor = Cursors.Hand
                };
                btnAv.Click += (_, _) =>
                {
                    secilenAvatar = av;
                    foreach (Button b in flpAvatarlar.Controls)
                        b.BackColor = b.Text == secilenAvatar ? tema.Vurgu : tema.Kart;
                };
                flpAvatarlar.Controls.Add(btnAv);
            }

            var btnKaydet = new Button
            {
                Text = "Kaydet",
                Location = new Point(260, 274),
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
            using var sfd = new SaveFileDialog
            {
                Title = "Profil Kartını PNG Olarak Kaydet",
                Filter = "PNG Görseli (*.png)|*.png",
                FileName = $"{_aktifTercih.OgrenciAdi.Replace(' ', '_')}_ProfilKarti.png"
            };

            if (sfd.ShowDialog(this) != DialogResult.OK) return;

            _btnProfilDuzenle.Visible = false;
            _btnPngIndir.Visible = false;
            _pnlProfilOnizleme.Refresh();

            try
            {
                using var bmp = new Bitmap(_pnlProfilOnizleme.Width, _pnlProfilOnizleme.Height);
                _pnlProfilOnizleme.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                bmp.Save(sfd.FileName, ImageFormat.Png);
                BasariBildirimiGoster("📸 Profil kartı PNG olarak kaydedildi!");
            }
            finally
            {
                _btnProfilDuzenle.Visible = true;
                _btnPngIndir.Visible = true;
            }
        }

        private void TemaButonlariniYenile()
        {
            _flpTemaButonlari.Controls.Clear();
            var temalar = Kisisellestirme.TumTemalariGetir(_aktifTercih);
            var aktifTema = Kisisellestirme.TemaBul(_aktifTercih.TemaAdi, _aktifTercih);
            _btnOzelTemaSil.Visible = aktifTema.OzelMi;

            foreach (var tema in temalar)
            {
                bool secili = tema.Ad.Equals(_aktifTercih.TemaAdi, StringComparison.OrdinalIgnoreCase);
                var btn = new Button
                {
                    Size = new Size(162, 34),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = tema.Kart,
                    ForeColor = tema.Yazi,
                    Text = $"● {tema.Ad}",
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(8, 0, 4, 0),
                    Margin = new Padding(0, 0, 6, 6),
                    Cursor = Cursors.Hand,
                    Tag = "SabitRenk"
                };

                btn.FlatAppearance.BorderColor = secili ? tema.Vurgu : Color.FromArgb(95, 95, 105);
                btn.FlatAppearance.BorderSize = secili ? 2 : 1;

                btn.Paint += (_, e) =>
                {
                    using var firca = new SolidBrush(tema.Vurgu);
                    e.Graphics.FillEllipse(firca, 10, 11, 11, 11);
                };

                btn.Click += (_, _) =>
                {
                    _aktifTercih.TemaAdi = tema.Ad;
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
                var yeniPalet = KisisellestirmeServisi.TekRenktenPaletUret(ad, renkSecici.Color, rbKoyu.Checked);
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
            Kisisellestirme.TemayiFormUzerineUygula(this, _pnlSidebar, _pnlIcerikAlani, _aktifTercih);
            TumProfilVerileriniYenile();
        }
    }
}