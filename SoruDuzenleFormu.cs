using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KademeliEgitimPlatformu.Models;
using KademeliEgitimPlatformu.Services;

namespace KademeliEgitimPlatformu
{
    /// <summary>
    /// Belge önizleme veya soru havuzu yönetiminde tek bir sorunun başlık, şık, doğru cevap,
    /// modül, zorluk ve pedagojik açıklamasını uygulama içinden hızlıca düzenlemeye yarayan pencere.
    /// </summary>
    public sealed class SoruDuzenleFormu : Form
    {
        private readonly Soru _hedefSoru;

        private ComboBox _cmbModul = null!;
        private ComboBox _cmbZorluk = null!;
        private ComboBox _cmbEtiket = null!;
        private TextBox _txtBaslik = null!;
        private TextBox _txtKodBlogu = null!;
        private readonly TextBox[] _txtSecenekler = new TextBox[4];
        private readonly RadioButton[] _rbDogruSecenek = new RadioButton[4];
        private TextBox _txtAciklama = null!;
        private Label _lblDogrulama = null!;
        private Button _btnKaydet = null!;
        private Button _btnIptal = null!;

        public SoruDuzenleFormu(Soru soru, int? soruNo = null)
        {
            _hedefSoru = soru ?? throw new ArgumentNullException(nameof(soru));
            ArayuzuOlustur(soruNo);
            VerileriDoldur();
            CanliDogrula();
        }

        private void ArayuzuOlustur(int? soruNo)
        {
            Text = soruNo.HasValue
                ? $"Soru {soruNo.Value} — Hızlı Düzenle"
                : "Soru Düzenle";
            AutoScaleMode = AutoScaleMode.Dpi;
            Size = new Size(860, 740);
            MinimumSize = new Size(720, 620);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(15, 23, 42);
            ForeColor = Color.White;
            Padding = new Padding(18);

            var anaIzgara = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 7
            };
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));  // Modül, Zorluk, Etiket
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 102F)); // Soru Metni
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));  // Kod Bloğu (Opsiyonel)
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // 4 Şık + Doğru Cevap seçimi
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));  // Açıklama / Neden Yanlış
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));  // Canlı Doğrulama Durumu
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));  // Alt Butonlar

            // 1) ÜST META SATIRI (MODÜL, ZORLUK, ETİKET)
            var ustIzgara = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 8)
            };
            ustIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
            ustIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
            ustIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));

            _cmbModul = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Bottom,
                Font = FontOnbellegi.Al("Segoe UI", 9.5f),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White
            };
            _cmbModul.Items.AddRange(new object[] { "Görsel Programlama", "Veri Tabanları", "Siber Güvenlik" });

            _cmbZorluk = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Bottom,
                Font = FontOnbellegi.Al("Segoe UI", 9.5f),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White
            };
            _cmbZorluk.Items.AddRange(new object[] { "Kolay", "Orta", "Zor" });

            _cmbEtiket = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDown,
                Dock = DockStyle.Bottom,
                Font = FontOnbellegi.Al("Segoe UI", 9.5f),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White
            };
            _cmbEtiket.Items.AddRange(new object[] { "", "AraSinav", "KolayOrtaAraSinav", "OrtaZorAraSinav" });
            _cmbEtiket.TextChanged += (_, _) => CanliDogrula();

            ustIzgara.Controls.Add(AlanKapsayici("Modül:", _cmbModul, new Padding(0, 0, 10, 0)), 0, 0);
            ustIzgara.Controls.Add(AlanKapsayici("Zorluk Seviyesi:", _cmbZorluk, new Padding(0, 0, 10, 0)), 1, 0);
            ustIzgara.Controls.Add(AlanKapsayici("Etiket (Opsiyonel):", _cmbEtiket, new Padding(0)), 2, 0);

            // 2) SORU METNİ
            _txtBaslik = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = FontOnbellegi.Al("Segoe UI", 10f),
                BackColor = Color.FromArgb(22, 32, 50),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtBaslik.TextChanged += (_, _) => CanliDogrula();

            // 3) KOD BLOĞU (OPSİYONEL)
            _txtKodBlogu = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                AcceptsReturn = true,
                AcceptsTab = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = FontOnbellegi.Al("Consolas", 9.5f),
                BackColor = Color.FromArgb(15, 23, 38),
                ForeColor = Color.FromArgb(125, 211, 252),
                BorderStyle = BorderStyle.FixedSingle
            };

            // 4) ŞIKLAR VE DOĞRU CEVAP RADYO BUTONLARI
            var pnlSiklar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(22, 32, 50),
                Padding = new Padding(12, 8, 12, 10),
                Margin = new Padding(0, 4, 0, 8)
            };

            var lblSikBaslik = new Label
            {
                Text = "Şıklar (Doğru cevabı soldaki daireden işaretleyin):",
                Font = FontOnbellegi.Al("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(125, 211, 252),
                Dock = DockStyle.Top,
                Height = 24
            };

            var sikIzgara = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4
            };
            sikIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86F));
            sikIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            string[] harfler = { "A", "B", "C", "D" };
            for (int i = 0; i < 4; i++)
            {
                sikIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));

                var rb = new RadioButton
                {
                    Text = $"{harfler[i]} Doğru",
                    Font = FontOnbellegi.Al("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(203, 213, 225),
                    Dock = DockStyle.Fill,
                    Cursor = Cursors.Hand
                };
                rb.CheckedChanged += (_, _) => CanliDogrula();
                _rbDogruSecenek[i] = rb;

                var txt = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Font = FontOnbellegi.Al("Segoe UI", 9.5f),
                    BackColor = Color.FromArgb(15, 23, 42),
                    ForeColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    Margin = new Padding(4, 5, 0, 5)
                };
                txt.TextChanged += (_, _) => CanliDogrula();
                _txtSecenekler[i] = txt;

                sikIzgara.Controls.Add(rb, 0, i);
                sikIzgara.Controls.Add(txt, 1, i);
            }

            pnlSiklar.Controls.Add(sikIzgara);
            pnlSiklar.Controls.Add(lblSikBaslik);

            // 5) AÇIKLAMA / NEDEN YANLIŞ
            _txtAciklama = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = FontOnbellegi.Al("Segoe UI", 9.5f),
                BackColor = Color.FromArgb(22, 32, 50),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // 6) CANLI DOĞRULAMA ŞERİDİ
            _lblDogrulama = new Label
            {
                Dock = DockStyle.Fill,
                Font = FontOnbellegi.Al("Segoe UI", 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 10, 0)
            };

            // 7) ALT BUTONLAR
            var pnlButonlar = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) };

            _btnKaydet = new Button
            {
                Text = "✓ Değişiklikleri Uygula",
                Font = FontOnbellegi.Al("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(210, 40),
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };
            _btnKaydet.FlatAppearance.BorderSize = 0;
            _btnKaydet.Click += BtnKaydet_Click;

            var bosluk = new Panel { Width = 10, Dock = DockStyle.Right };

            _btnIptal = new Button
            {
                Text = "İptal",
                Font = FontOnbellegi.Al("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(105, 40),
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };
            _btnIptal.FlatAppearance.BorderSize = 0;
            _btnIptal.Click += (_, _) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            pnlButonlar.Controls.Add(_btnIptal);
            pnlButonlar.Controls.Add(bosluk);
            pnlButonlar.Controls.Add(_btnKaydet);

            anaIzgara.Controls.Add(ustIzgara, 0, 0);
            anaIzgara.Controls.Add(AlanKapsayici("Soru Metni:", _txtBaslik, new Padding(0, 0, 0, 6)), 0, 1);
            anaIzgara.Controls.Add(AlanKapsayici("Kod Bloğu (İsteğe Bağlı):", _txtKodBlogu, new Padding(0, 0, 0, 6)), 0, 2);
            anaIzgara.Controls.Add(pnlSiklar, 0, 3);
            anaIzgara.Controls.Add(AlanKapsayici("Pedagojik Açıklama / Neden Yanlış (İsteğe Bağlı):", _txtAciklama, new Padding(0, 0, 0, 6)), 0, 4);
            anaIzgara.Controls.Add(_lblDogrulama, 0, 5);
            anaIzgara.Controls.Add(pnlButonlar, 0, 6);

            Controls.Add(anaIzgara);
            CancelButton = _btnIptal;
        }

        private static Panel AlanKapsayici(string etiket, Control kontrol, Padding margin)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Margin = margin };
            var lbl = new Label
            {
                Text = etiket,
                Font = FontOnbellegi.Al("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Top,
                Height = 20
            };
            panel.Controls.Add(kontrol);
            panel.Controls.Add(lbl);
            return panel;
        }

        private void VerileriDoldur()
        {
            _cmbModul.SelectedIndex = Enum.IsDefined(typeof(DisiplinTuru), _hedefSoru.Disiplin)
                ? (int)_hedefSoru.Disiplin
                : 0;
            _cmbZorluk.SelectedIndex = Enum.IsDefined(typeof(ZorlukSeviyesi), _hedefSoru.Zorluk)
                ? (int)_hedefSoru.Zorluk
                : 1;
            _cmbEtiket.Text = _hedefSoru.Etiket ?? string.Empty;
            _txtBaslik.Text = _hedefSoru.Baslik ?? string.Empty;
            _txtKodBlogu.Text = _hedefSoru.KodBlogu ?? string.Empty;

            for (int i = 0; i < 4; i++)
            {
                _txtSecenekler[i].Text = _hedefSoru.Secenekler?.ElementAtOrDefault(i) ?? string.Empty;
                _rbDogruSecenek[i].Checked = _hedefSoru.DogruSecenekIndex == i;
            }

            if (_hedefSoru.YanlisCevapAciklamalari != null && _hedefSoru.YanlisCevapAciklamalari.Count > 0)
            {
                _txtAciklama.Text = _hedefSoru.YanlisCevapAciklamalari.Values.FirstOrDefault() ?? string.Empty;
            }
        }

        private Soru TaslakSoruOlustur()
        {
            int dogruIndex = -1;
            for (int i = 0; i < 4; i++)
            {
                if (_rbDogruSecenek[i].Checked)
                {
                    dogruIndex = i;
                    break;
                }
            }

            var aciklamalar = new Dictionary<int, string>();
            string temizAciklama = _txtAciklama.Text.Trim();
            if (!string.IsNullOrEmpty(temizAciklama))
            {
                for (int i = 0; i < 4; i++)
                {
                    if (i != dogruIndex)
                        aciklamalar[i] = temizAciklama;
                }
            }

            return new Soru
            {
                Id = _hedefSoru.Id,
                Disiplin = (DisiplinTuru)Math.Clamp(_cmbModul.SelectedIndex, 0, 2),
                Zorluk = (ZorlukSeviyesi)Math.Clamp(_cmbZorluk.SelectedIndex, 0, 2),
                Etiket = string.IsNullOrWhiteSpace(_cmbEtiket.Text) ? null : _cmbEtiket.Text.Trim(),
                Baslik = _txtBaslik.Text.Trim(),
                KodBlogu = string.IsNullOrWhiteSpace(_txtKodBlogu.Text) ? null : _txtKodBlogu.Text.Replace("\r\n", "\n"),
                Secenekler = _txtSecenekler.Select(t => t.Text.Trim()).ToList(),
                DogruSecenekIndex = dogruIndex,
                YanlisCevapAciklamalari = aciklamalar
            };
        }

        private void CanliDogrula()
        {
            Soru taslak = TaslakSoruOlustur();
            SoruDogrulamaSonucu sonuc = SoruDogrulayici.Dogrula(taslak);

            for (int i = 0; i < 4; i++)
            {
                bool bos = string.IsNullOrWhiteSpace(_txtSecenekler[i].Text);
                _txtSecenekler[i].BackColor = bos
                    ? Color.FromArgb(69, 26, 26)
                    : Color.FromArgb(15, 23, 42);
                _rbDogruSecenek[i].ForeColor = _rbDogruSecenek[i].Checked
                    ? Color.FromArgb(74, 222, 128)
                    : Color.FromArgb(203, 213, 225);
            }

            _txtBaslik.BackColor = string.IsNullOrWhiteSpace(_txtBaslik.Text)
                ? Color.FromArgb(69, 26, 26)
                : Color.FromArgb(22, 32, 50);

            if (sonuc.GecerliMi)
            {
                _lblDogrulama.BackColor = Color.FromArgb(20, 65, 40);
                _lblDogrulama.ForeColor = Color.FromArgb(134, 239, 172);
                _lblDogrulama.Text = "✓ Soru geçerli — değişiklikleri uygulayıp kaydedebilirsiniz.";
            }
            else
            {
                _lblDogrulama.BackColor = Color.FromArgb(69, 26, 26);
                _lblDogrulama.ForeColor = Color.FromArgb(254, 202, 202);
                _lblDogrulama.Text = "✕ " + sonuc.HataOzeti;
            }
        }

        private void BtnKaydet_Click(object? sender, EventArgs e)
        {
            Soru guncel = TaslakSoruOlustur();
            SoruDogrulamaSonucu sonuc = SoruDogrulayici.Dogrula(guncel);

            if (!sonuc.GecerliMi)
            {
                DialogResult onay = MessageBox.Show(
                    "Soru hâlâ eksik veya hatalı alanlar içeriyor:\n\n• " +
                    string.Join("\n• ", sonuc.Hatalar) +
                    "\n\nYine de taslak olarak kaydedilsin mi? (Veritabanına aktarılabilmesi için tüm hataların giderilmesi gerekir.)",
                    "Eksik Alan Uyarısı",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (onay != DialogResult.Yes)
                    return;
            }

            _hedefSoru.Disiplin = guncel.Disiplin;
            _hedefSoru.Zorluk = guncel.Zorluk;
            _hedefSoru.Etiket = guncel.Etiket;
            _hedefSoru.Baslik = guncel.Baslik;
            _hedefSoru.KodBlogu = guncel.KodBlogu;
            _hedefSoru.Secenekler = guncel.Secenekler;
            _hedefSoru.DogruSecenekIndex = guncel.DogruSecenekIndex;

            // Kullanıcı genel açıklamayı değiştirdiyse güncelle; boş bıraktıysa temizle.
            string mevcutIlk = _hedefSoru.YanlisCevapAciklamalari?.Values.FirstOrDefault() ?? string.Empty;
            string yeniAciklama = _txtAciklama.Text.Trim();
            if (string.IsNullOrEmpty(yeniAciklama))
            {
                _hedefSoru.YanlisCevapAciklamalari = new Dictionary<int, string>();
            }
            else if (!string.Equals(mevcutIlk, yeniAciklama, StringComparison.Ordinal) ||
                     _hedefSoru.YanlisCevapAciklamalari == null ||
                     _hedefSoru.YanlisCevapAciklamalari.Count == 0)
            {
                _hedefSoru.YanlisCevapAciklamalari = guncel.YanlisCevapAciklamalari;
            }
            else
            {
                // Doğru şık değiştiyse yeni doğru şıkkın yanlış açıklamasını kaldır.
                if (guncel.DogruSecenekIndex is >= 0 and <= 3)
                    _hedefSoru.YanlisCevapAciklamalari.Remove(guncel.DogruSecenekIndex);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
