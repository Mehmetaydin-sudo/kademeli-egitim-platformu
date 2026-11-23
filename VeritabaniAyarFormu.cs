using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using KademeliEgitimPlatformu.Services;
using Npgsql;

namespace KademeliEgitimPlatformu
{
    /// <summary>
    /// Son kullanıcıların JSON dosyası düzenlemeden yerel PostgreSQL 18 bağlantı bilgilerini
    /// girip test edebileceği ve güvenli biçimde Belgelerim klasörüne kaydedebileceği kurulum ekranı.
    /// </summary>
    public sealed class VeritabaniAyarFormu : Form
    {
        private TextBox _txtHost = null!;
        private NumericUpDown _numPort = null!;
        private TextBox _txtVeritabani = null!;
        private TextBox _txtKullanici = null!;
        private TextBox _txtSifre = null!;
        private CheckBox _chkSifreGoster = null!;
        private Label _lblDurum = null!;
        private Button _btnTestEt = null!;
        private Button _btnKaydet = null!;
        private Button _btnIptal = null!;

        public VeritabaniAyarFormu()
        {
            ArayuzuOlustur();
            MevcutAyarlariYukle();
        }

        private void ArayuzuOlustur()
        {
            Text = "PostgreSQL 18 Bağlantı Kurulumu — Kademeli Eğitim Platformu";
            AutoScaleMode = AutoScaleMode.Dpi;
            Size = new Size(560, 485);
            MinimumSize = new Size(500, 440);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(15, 23, 42);
            ForeColor = Color.White;
            Padding = new Padding(20);

            var anaIzgara = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 74F));
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));

            // 1) BAŞLIK VE YÖNERGE
            var pnlBaslik = new Panel { Dock = DockStyle.Fill };
            var lblBaslik = new Label
            {
                Text = "🗄️ Yerel PostgreSQL 18 Bağlantı Sihirbazı",
                Font = FontOnbellegi.Al("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Dock = DockStyle.Top,
                Height = 30
            };
            var lblAciklama = new Label
            {
                Text = "Yerel PostgreSQL sunucu bilgilerinizi girin. Ayarlar güvenli biçimde Belgelerim\\KademeliEgitimPlatformu\\appsettings.json konumuna kaydedilir.",
                Font = FontOnbellegi.Al("Segoe UI", 9f),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Fill
            };
            pnlBaslik.Controls.Add(lblAciklama);
            pnlBaslik.Controls.Add(lblBaslik);

            // 2) FORM ALANLARI
            var formPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(22, 32, 50),
                Padding = new Padding(16, 12, 16, 12),
                Margin = new Padding(0, 6, 0, 10)
            };

            var alanIzgara = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6
            };
            alanIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145F));
            alanIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 6; i++)
                alanIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 16.66F));

            _txtHost = MetinKutusu();
            _numPort = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 65535,
                Value = 5432,
                Font = FontOnbellegi.Al("Segoe UI", 10f),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                Width = 110,
                Anchor = AnchorStyles.Left
            };
            _txtVeritabani = MetinKutusu();
            _txtKullanici = MetinKutusu();
            _txtSifre = MetinKutusu();
            _txtSifre.UseSystemPasswordChar = true;

            _chkSifreGoster = new CheckBox
            {
                Text = "Şifreyi göster",
                Font = FontOnbellegi.Al("Segoe UI", 9f),
                ForeColor = Color.FromArgb(203, 213, 225),
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };
            _chkSifreGoster.CheckedChanged += (_, _) => _txtSifre.UseSystemPasswordChar = !_chkSifreGoster.Checked;

            alanIzgara.Controls.Add(AlanEtiketi("Sunucu (Host):"), 0, 0);
            alanIzgara.Controls.Add(_txtHost, 1, 0);
            alanIzgara.Controls.Add(AlanEtiketi("Port:"), 0, 1);
            alanIzgara.Controls.Add(_numPort, 1, 1);
            alanIzgara.Controls.Add(AlanEtiketi("Veritabanı Adı:"), 0, 2);
            alanIzgara.Controls.Add(_txtVeritabani, 1, 2);
            alanIzgara.Controls.Add(AlanEtiketi("Kullanıcı Adı:"), 0, 3);
            alanIzgara.Controls.Add(_txtKullanici, 1, 3);
            alanIzgara.Controls.Add(AlanEtiketi("PostgreSQL Şifresi:"), 0, 4);
            alanIzgara.Controls.Add(_txtSifre, 1, 4);
            alanIzgara.Controls.Add(_chkSifreGoster, 1, 5);

            formPanel.Controls.Add(alanIzgara);

            // 3) DURUM MESAJI
            _lblDurum = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(22, 32, 50),
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = FontOnbellegi.Al("Segoe UI", 9f),
                Padding = new Padding(12, 8, 12, 8),
                Text = "İpucu: Önce 'Bağlantıyı Test Et' ile yerel PostgreSQL servisini doğrulayabilirsiniz."
            };

            // 4) BUTONLAR
            var pnlButon = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0) };

            _btnTestEt = new Button
            {
                Text = "🔌 Bağlantıyı Test Et",
                Font = FontOnbellegi.Al("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(165, 40),
                Dock = DockStyle.Left,
                Cursor = Cursors.Hand
            };
            _btnTestEt.FlatAppearance.BorderSize = 0;
            _btnTestEt.Click += BtnTestEt_Click;

            _btnKaydet = new Button
            {
                Text = "✓ Test Et ve Kaydet",
                Font = FontOnbellegi.Al("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(175, 40),
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };
            _btnKaydet.FlatAppearance.BorderSize = 0;
            _btnKaydet.Click += BtnKaydet_Click;

            _btnIptal = new Button
            {
                Text = "Kapat",
                Font = FontOnbellegi.Al("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(92, 40),
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };
            _btnIptal.FlatAppearance.BorderSize = 0;
            _btnIptal.Click += (_, _) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            pnlButon.Controls.Add(_btnTestEt);
            pnlButon.Controls.Add(_btnIptal);
            pnlButon.Controls.Add(new Panel { Width = 10, Dock = DockStyle.Right });
            pnlButon.Controls.Add(_btnKaydet);

            anaIzgara.Controls.Add(pnlBaslik, 0, 0);
            anaIzgara.Controls.Add(formPanel, 0, 1);
            anaIzgara.Controls.Add(_lblDurum, 0, 2);
            anaIzgara.Controls.Add(pnlButon, 0, 3);

            Controls.Add(anaIzgara);
            CancelButton = _btnIptal;
        }

        private static Label AlanEtiketi(string metin) => new()
        {
            Text = metin,
            Font = FontOnbellegi.Al("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(203, 213, 225),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        private static TextBox MetinKutusu() => new()
        {
            Dock = DockStyle.Fill,
            Font = FontOnbellegi.Al("Segoe UI", 10f),
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 4, 0, 4)
        };

        private void MevcutAyarlariYukle()
        {
            NpgsqlConnectionStringBuilder mevcut = VeritabaniBaglantisi.MevcutAyarlariOku();
            _txtHost.Text = string.IsNullOrWhiteSpace(mevcut.Host) ? "localhost" : mevcut.Host;
            _numPort.Value = Math.Clamp(mevcut.Port > 0 ? mevcut.Port : 5432, 1, 65535);
            _txtVeritabani.Text = string.IsNullOrWhiteSpace(mevcut.Database) ? "kademeli_egitim" : mevcut.Database;
            _txtKullanici.Text = string.IsNullOrWhiteSpace(mevcut.Username) ? "postgres" : mevcut.Username;
            _txtSifre.Text = mevcut.Password ?? string.Empty;
        }

        private async void BtnTestEt_Click(object? sender, EventArgs e)
        {
            await BaglantiyiCalistirAsync(kaydet: false);
        }

        private async void BtnKaydet_Click(object? sender, EventArgs e)
        {
            await BaglantiyiCalistirAsync(kaydet: true);
        }

        private async Task BaglantiyiCalistirAsync(bool kaydet)
        {
            string host = _txtHost.Text.Trim();
            int port = (int)_numPort.Value;
            string veritabani = _txtVeritabani.Text.Trim();
            string kullanici = _txtKullanici.Text.Trim();
            string sifre = _txtSifre.Text;

            Cursor onceki = Cursor;
            try
            {
                Cursor = Cursors.WaitCursor;
                _btnTestEt.Enabled = false;
                _btnKaydet.Enabled = false;
                _lblDurum.BackColor = Color.FromArgb(30, 41, 59);
                _lblDurum.ForeColor = Color.FromArgb(125, 211, 252);
                _lblDurum.Text = "⏳ Yerel PostgreSQL 18 bağlantısı test ediliyor...";

                (bool basarili, string mesaj) = await Task.Run(
                    () => VeritabaniBaglantisi.BaglantiyiTestEt(host, port, veritabani, kullanici, sifre));

                if (IsDisposed)
                    return;

                if (!basarili)
                {
                    _lblDurum.BackColor = Color.FromArgb(69, 26, 26);
                    _lblDurum.ForeColor = Color.FromArgb(254, 202, 202);
                    _lblDurum.Text = "✕ " + mesaj.Replace("\r\n", " ").Replace("\n", " ");
                    return;
                }

                if (kaydet)
                {
                    string kayitYolu = VeritabaniBaglantisi.BaglantiAyariniKaydet(host, port, veritabani, kullanici, sifre);
                    MessageBox.Show(
                        $"PostgreSQL bağlantı ayarları kaydedildi ve doğrulandı!\n\nAyar dosyası:\n{kayitYolu}",
                        "Kurulum Tamamlandı",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                    return;
                }

                _lblDurum.BackColor = Color.FromArgb(20, 65, 40);
                _lblDurum.ForeColor = Color.FromArgb(134, 239, 172);
                _lblDurum.Text = "✓ " + mesaj;
            }
            finally
            {
                if (!IsDisposed)
                {
                    Cursor = onceki;
                    _btnTestEt.Enabled = true;
                    _btnKaydet.Enabled = true;
                }
            }
        }
    }
}
