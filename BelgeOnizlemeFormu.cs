using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using KademeliEgitimPlatformu.Models;
using KademeliEgitimPlatformu.Services;

namespace KademeliEgitimPlatformu
{
    /// <summary>
    /// Word (.docx) ve PDF (.pdf) belgelerinden ayrıştırılan soruları veritabanına
    /// kaydetmeden önce listeleyen, SoruDogrulayici ile hatalı/eksik soruları vurgulayan
    /// ve kullanıcı onayı alan kontrol/önizleme penceresi.
    /// </summary>
    public sealed class BelgeOnizlemeFormu : Form
    {
        private readonly string _dosyaYolu;
        private BelgeDonusturmeSonucu _sonuc;

        private ComboBox _cmbVarsayilanModul = null!;
        private Label _lblOzet = null!;
        private DataGridView _gridSorular = null!;
        private RichTextBox _txtSeciliDetay = null!;
        private RichTextBox _txtUyarilar = null!;
        private Button _btnDuzenle = null!;
        private Button _btnKaydet = null!;
        private Button _btnIptal = null!;

        /// <summary>Kullanıcının seçip onayladığı, SoruDogrulayici'dan geçmiş sorular.</summary>
        public List<Soru> OnaylananSorular { get; private set; } = new();

        public BelgeOnizlemeFormu(
            string dosyaYolu,
            DisiplinTuru varsayilanDisiplin = DisiplinTuru.GorselProgramlama,
            BelgeDonusturmeSonucu? hazirSonuc = null)
        {
            _dosyaYolu = dosyaYolu ?? throw new ArgumentNullException(nameof(dosyaYolu));
            _sonuc = hazirSonuc ?? BelgeSoruDonusturucu.DosyadanDonustur(_dosyaYolu, varsayilanDisiplin);

            ArayuzuOlustur(varsayilanDisiplin);
            SonuclariTabloyaYukle();
        }

        private void ArayuzuOlustur(DisiplinTuru varsayilanDisiplin)
        {
            Text = $"Belge Soru Önizleme ve Doğrulama — {Path.GetFileName(_dosyaYolu)}";
            AutoScaleMode = AutoScaleMode.Dpi;
            Size = new Size(1240, 760);
            MinimumSize = new Size(960, 620);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(15, 23, 42);
            ForeColor = Color.White;
            Padding = new Padding(18);

            var anaIzgara = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 84F));
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 64F));
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 36F));
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));

            // 1) ÜST BİLGİ VE VARSAYILAN MODÜL SEÇİMİ
            var pnlUst = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(22, 32, 50),
                Padding = new Padding(16, 10, 16, 10),
                Margin = new Padding(0, 0, 0, 10)
            };

            var lblBaslik = new Label
            {
                Text = $"📄 {Path.GetFileName(_dosyaYolu)}",
                Font = FontOnbellegi.Al("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(125, 211, 252),
                Dock = DockStyle.Top,
                Height = 28,
                AutoEllipsis = true
            };

            _lblOzet = new Label
            {
                Font = FontOnbellegi.Al("Segoe UI", 10f),
                ForeColor = Color.FromArgb(203, 213, 225),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var pnlModul = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 340,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 12, 0, 0),
                BackColor = Color.Transparent
            };

            _cmbVarsayilanModul = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = FontOnbellegi.Al("Segoe UI", 10f),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                Width = 195
            };
            _cmbVarsayilanModul.Items.Add(new ModulSecenegi(DisiplinTuru.GorselProgramlama, "Görsel Programlama"));
            _cmbVarsayilanModul.Items.Add(new ModulSecenegi(DisiplinTuru.VeriTabanlari, "Veri Tabanları"));
            _cmbVarsayilanModul.Items.Add(new ModulSecenegi(DisiplinTuru.SiberGuvenlik, "Siber Güvenlik"));
            _cmbVarsayilanModul.SelectedIndex = (int)varsayilanDisiplin;
            _cmbVarsayilanModul.SelectedIndexChanged += VarsayilanModul_Degisti;

            var lblModul = new Label
            {
                Text = "Varsayılan Modül:",
                Font = FontOnbellegi.Al("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                Padding = new Padding(0, 5, 8, 0)
            };

            pnlModul.Controls.Add(_cmbVarsayilanModul);
            pnlModul.Controls.Add(lblModul);

            pnlUst.Controls.Add(_lblOzet);
            pnlUst.Controls.Add(lblBaslik);
            pnlUst.Controls.Add(pnlModul);

            // 2) ORTA SORU LİSTESİ (DATAGRIDVIEW)
            _gridSorular = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(22, 32, 50),
                BorderStyle = BorderStyle.FixedSingle,
                GridColor = Color.FromArgb(51, 65, 85),
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 36,
                RowTemplate = { Height = 34 },
                EnableHeadersVisualStyles = false,
                Margin = new Padding(0, 0, 0, 10)
            };

            _gridSorular.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.FromArgb(226, 232, 240),
                Font = FontOnbellegi.Al("Segoe UI", 9.5f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };

            _gridSorular.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(22, 32, 50),
                ForeColor = Color.White,
                SelectionBackColor = Color.FromArgb(37, 99, 235),
                SelectionForeColor = Color.White,
                Font = FontOnbellegi.Al("Segoe UI", 9.5f)
            };

            _gridSorular.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Aktar",
                HeaderText = "Aktar",
                FillWeight = 28,
                MinimumWidth = 52
            });
            _gridSorular.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "No",
                HeaderText = "No",
                ReadOnly = true,
                FillWeight = 24,
                MinimumWidth = 42
            });
            _gridSorular.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Durum",
                HeaderText = "Durum",
                ReadOnly = true,
                FillWeight = 58,
                MinimumWidth = 96
            });
            _gridSorular.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Modul",
                HeaderText = "Modül",
                ReadOnly = true,
                FillWeight = 75,
                MinimumWidth = 115
            });
            _gridSorular.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Zorluk",
                HeaderText = "Zorluk",
                ReadOnly = true,
                FillWeight = 36,
                MinimumWidth = 60
            });
            _gridSorular.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Baslik",
                HeaderText = "Soru Metni",
                ReadOnly = true,
                FillWeight = 165,
                MinimumWidth = 220
            });
            _gridSorular.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Cevap",
                HeaderText = "Cevap",
                ReadOnly = true,
                FillWeight = 30,
                MinimumWidth = 52
            });
            _gridSorular.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Uyarilar",
                HeaderText = "Doğrulama / Uyarı",
                ReadOnly = true,
                FillWeight = 140,
                MinimumWidth = 180
            });

            _gridSorular.SelectionChanged += GridSorular_SelectionChanged;
            _gridSorular.CellDoubleClick += GridSorular_CellDoubleClick;
            _gridSorular.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (_gridSorular.IsCurrentCellDirty)
                    _gridSorular.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _gridSorular.CellValueChanged += GridSorular_CellValueChanged;

            // 3) ALT DETAY VE UYARI PANELLERİ
            var altIzgara = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10)
            };
            altIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            altIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));

            var pnlDetay = KutuluAltPanel("Seçili Soru Önizlemesi", Color.FromArgb(56, 189, 248), out _txtSeciliDetay);
            pnlDetay.Margin = new Padding(0, 0, 8, 0);

            var pnlUyarilar = KutuluAltPanel("Ayrıştırma ve Doğrulama Uyarıları", Color.FromArgb(251, 146, 60), out _txtUyarilar);
            pnlUyarilar.Margin = new Padding(8, 0, 0, 0);

            altIzgara.Controls.Add(pnlDetay, 0, 0);
            altIzgara.Controls.Add(pnlUyarilar, 1, 0);

            // 4) ALT EYLEM BUTONLARI
            var pnlAltButonlar = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 6, 0, 0)
            };

            var lblBilgi = new Label
            {
                Text = "İpucu: Hatalı/eksik (kırmızı) satıra çift tıklayarak veya 'Seçili Soruyu Düzenle' butonuyla uygulama içinden anında düzeltebilirsiniz.",
                Font = FontOnbellegi.Al("Segoe UI", 9f),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _btnDuzenle = new Button
            {
                Text = "✎ Seçili Soruyu Düzenle",
                Font = FontOnbellegi.Al("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(195, 44),
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };
            _btnDuzenle.FlatAppearance.BorderSize = 0;
            _btnDuzenle.Click += (_, _) => SeciliSoruyuDuzenle();

            var pnlBoslukSol = new Panel { Width = 10, Dock = DockStyle.Right };

            _btnKaydet = new Button
            {
                Text = "✓ Onayla ve Veritabanına Kaydet",
                Font = FontOnbellegi.Al("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(270, 44),
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };
            _btnKaydet.FlatAppearance.BorderSize = 0;
            _btnKaydet.Click += BtnKaydet_Click;

            var pnlBosluk = new Panel { Width = 10, Dock = DockStyle.Right };

            _btnIptal = new Button
            {
                Text = "İptal",
                Font = FontOnbellegi.Al("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 44),
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };
            _btnIptal.FlatAppearance.BorderSize = 0;
            _btnIptal.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            pnlAltButonlar.Controls.Add(lblBilgi);
            pnlAltButonlar.Controls.Add(_btnIptal);
            pnlAltButonlar.Controls.Add(pnlBosluk);
            pnlAltButonlar.Controls.Add(_btnDuzenle);
            pnlAltButonlar.Controls.Add(pnlBoslukSol);
            pnlAltButonlar.Controls.Add(_btnKaydet);

            anaIzgara.Controls.Add(pnlUst, 0, 0);
            anaIzgara.Controls.Add(_gridSorular, 0, 1);
            anaIzgara.Controls.Add(altIzgara, 0, 2);
            anaIzgara.Controls.Add(pnlAltButonlar, 0, 3);

            Controls.Add(anaIzgara);
            CancelButton = _btnIptal;
        }

        private static Panel KutuluAltPanel(string baslik, Color baslikRengi, out RichTextBox metinKutusu)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(22, 32, 50),
                Padding = new Padding(12, 8, 12, 10)
            };

            var lbl = new Label
            {
                Text = baslik,
                Font = FontOnbellegi.Al("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = baslikRengi,
                Dock = DockStyle.Top,
                Height = 24
            };

            metinKutusu = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(226, 232, 240),
                Font = FontOnbellegi.Al("Segoe UI", 9.5f),
                WordWrap = true
            };

            panel.Controls.Add(metinKutusu);
            panel.Controls.Add(lbl);
            return panel;
        }

        private async void VarsayilanModul_Degisti(object? sender, EventArgs e)
        {
            if (_cmbVarsayilanModul.SelectedItem is not ModulSecenegi secim)
                return;

            Cursor oncekiImlec = Cursor;
            try
            {
                Cursor = Cursors.WaitCursor;
                _cmbVarsayilanModul.Enabled = false;
                _btnKaydet.Enabled = false;
                _lblOzet.Text = "Belge yeni varsayılan modülle yeniden ayrıştırılıyor...";

                BelgeDonusturmeSonucu yeniSonuc = await Task.Run(
                    () => BelgeSoruDonusturucu.DosyadanDonustur(_dosyaYolu, secim.Disiplin));

                if (IsDisposed)
                    return;

                _sonuc = yeniSonuc;
                SonuclariTabloyaYukle();
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                {
                    MessageBox.Show(
                        "Belge yeni modül seçimiyle yeniden okunurken hata oluştu.\n\n" + ex.Message,
                        "Yeniden Okuma Hatası",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            finally
            {
                if (!IsDisposed)
                {
                    Cursor = oncekiImlec;
                    _cmbVarsayilanModul.Enabled = true;
                    OzetVeButonGuncelle();
                }
            }
        }

        private void SonuclariTabloyaYukle()
        {
            _gridSorular.Rows.Clear();

            foreach (BelgeSoruKaydi kayit in _sonuc.Kayitlar)
            {
                SoruDogrulamaSonucu dogrulama = SoruDogrulayici.Dogrula(kayit.Soru);
                bool gecerli = kayit.GecerliMi && dogrulama.GecerliMi;
                string durum = gecerli
                    ? (kayit.BilgiUyarilari.Count > 0 ? "✓ Geçerli (Notlu)" : "✓ Geçerli")
                    : "✕ Hatalı / Eksik";

                string cevapHarfi = kayit.Soru.DogruSecenekIndex is >= 0 and <= 3
                    ? ((char)('A' + kayit.Soru.DogruSecenekIndex)).ToString()
                    : "—";

                string uyariMetni = kayit.TumUyarilarMetni;
                if (string.IsNullOrWhiteSpace(uyariMetni) && !dogrulama.GecerliMi)
                    uyariMetni = dogrulama.HataOzeti;

                int satirIndex = _gridSorular.Rows.Add(
                    gecerli,
                    kayit.SoruNo,
                    durum,
                    DisiplinAdi(kayit.Soru.Disiplin),
                    kayit.Soru.Zorluk.ToString(),
                    string.IsNullOrWhiteSpace(kayit.Soru.Baslik) ? "(Soru metni eksik)" : kayit.Soru.Baslik,
                    cevapHarfi,
                    string.IsNullOrWhiteSpace(uyariMetni) ? "Sorun yok" : uyariMetni);

                DataGridViewRow satir = _gridSorular.Rows[satirIndex];
                satir.Tag = kayit;

                if (!gecerli)
                {
                    satir.Cells["Aktar"].ReadOnly = true;
                    satir.DefaultCellStyle.BackColor = Color.FromArgb(69, 26, 26);
                    satir.DefaultCellStyle.ForeColor = Color.FromArgb(254, 202, 202);
                    satir.DefaultCellStyle.SelectionBackColor = Color.FromArgb(127, 29, 29);
                    satir.Cells["Durum"].Style.ForeColor = Color.FromArgb(248, 113, 113);
                }
                else if (kayit.BilgiUyarilari.Count > 0)
                {
                    satir.Cells["Durum"].Style.ForeColor = Color.FromArgb(250, 204, 21);
                    satir.Cells["Uyarilar"].Style.ForeColor = Color.FromArgb(253, 224, 71);
                }
                else
                {
                    satir.Cells["Durum"].Style.ForeColor = Color.FromArgb(74, 222, 128);
                    satir.Cells["Uyarilar"].Style.ForeColor = Color.FromArgb(148, 163, 184);
                }
            }

            UyarilarPaneliniGuncelle();

            if (_gridSorular.Rows.Count > 0)
            {
                _gridSorular.Rows[0].Selected = true;
                _btnDuzenle.Enabled = true;
                SeciliSoruyuGoster(_sonuc.Kayitlar[0]);
            }
            else
            {
                _btnDuzenle.Enabled = false;
                _txtSeciliDetay.Text = "Belgeden ayrıştırılmış soru bulunamadı.";
            }

            OzetVeButonGuncelle();
        }

        private void GridSorular_SelectionChanged(object? sender, EventArgs e)
        {
            bool seciliVar = _gridSorular.SelectedRows.Count > 0;
            _btnDuzenle.Enabled = seciliVar;

            if (!seciliVar)
                return;

            if (_gridSorular.SelectedRows[0].Tag is BelgeSoruKaydi kayit)
                SeciliSoruyuGoster(kayit);
        }

        private void GridSorular_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            // Onay kutusuna (Aktar) çift tıklandığında düzenleme penceresini açma.
            if (e.ColumnIndex == _gridSorular.Columns["Aktar"]!.Index)
                return;

            SeciliSoruyuDuzenle();
        }

        private void SeciliSoruyuDuzenle()
        {
            if (_gridSorular.SelectedRows.Count == 0)
                return;

            DataGridViewRow seciliSatir = _gridSorular.SelectedRows[0];
            if (seciliSatir.Tag is not BelgeSoruKaydi kayit)
                return;

            bool onceGecerliMiydi = kayit.GecerliMi;

            using var duzenle = new SoruDuzenleFormu(kayit.Soru, kayit.SoruNo);
            if (duzenle.ShowDialog(this) != DialogResult.OK)
                return;

            kayit.YenidenDogrula();
            _sonuc.UyarilariYenile();

            SatirGorunumunuGuncelle(seciliSatir, kayit, otomatikIsaretle: !onceGecerliMiydi && kayit.GecerliMi);
            UyarilarPaneliniGuncelle();
            SeciliSoruyuGoster(kayit);
            OzetVeButonGuncelle();
        }

        private void SatirGorunumunuGuncelle(DataGridViewRow satir, BelgeSoruKaydi kayit, bool otomatikIsaretle)
        {
            SoruDogrulamaSonucu dogrulama = SoruDogrulayici.Dogrula(kayit.Soru);
            bool gecerli = kayit.GecerliMi && dogrulama.GecerliMi;

            satir.Cells["Durum"].Value = gecerli
                ? (kayit.BilgiUyarilari.Count > 0 ? "✓ Geçerli (Notlu)" : "✓ Geçerli")
                : "✕ Hatalı / Eksik";
            satir.Cells["Modul"].Value = DisiplinAdi(kayit.Soru.Disiplin);
            satir.Cells["Zorluk"].Value = kayit.Soru.Zorluk.ToString();
            satir.Cells["Baslik"].Value = string.IsNullOrWhiteSpace(kayit.Soru.Baslik)
                ? "(Soru metni eksik)"
                : kayit.Soru.Baslik;
            satir.Cells["Cevap"].Value = kayit.Soru.DogruSecenekIndex is >= 0 and <= 3
                ? ((char)('A' + kayit.Soru.DogruSecenekIndex)).ToString()
                : "—";

            string uyariMetni = kayit.TumUyarilarMetni;
            if (string.IsNullOrWhiteSpace(uyariMetni) && !dogrulama.GecerliMi)
                uyariMetni = dogrulama.HataOzeti;
            satir.Cells["Uyarilar"].Value = string.IsNullOrWhiteSpace(uyariMetni) ? "Sorun yok" : uyariMetni;

            if (!gecerli)
            {
                satir.Cells["Aktar"].Value = false;
                satir.Cells["Aktar"].ReadOnly = true;
                satir.DefaultCellStyle.BackColor = Color.FromArgb(69, 26, 26);
                satir.DefaultCellStyle.ForeColor = Color.FromArgb(254, 202, 202);
                satir.DefaultCellStyle.SelectionBackColor = Color.FromArgb(127, 29, 29);
                satir.Cells["Durum"].Style.ForeColor = Color.FromArgb(248, 113, 113);
                satir.Cells["Uyarilar"].Style.ForeColor = Color.FromArgb(254, 202, 202);
            }
            else
            {
                satir.Cells["Aktar"].ReadOnly = false;
                if (otomatikIsaretle)
                    satir.Cells["Aktar"].Value = true;

                satir.DefaultCellStyle.BackColor = Color.FromArgb(22, 32, 50);
                satir.DefaultCellStyle.ForeColor = Color.White;
                satir.DefaultCellStyle.SelectionBackColor = Color.FromArgb(37, 99, 235);
                satir.Cells["Durum"].Style.ForeColor = kayit.BilgiUyarilari.Count > 0
                    ? Color.FromArgb(250, 204, 21)
                    : Color.FromArgb(74, 222, 128);
                satir.Cells["Uyarilar"].Style.ForeColor = kayit.BilgiUyarilari.Count > 0
                    ? Color.FromArgb(253, 224, 71)
                    : Color.FromArgb(148, 163, 184);
            }
        }

        private void UyarilarPaneliniGuncelle()
        {
            if (_sonuc.Uyarilar.Count == 0)
            {
                _txtUyarilar.ForeColor = Color.FromArgb(134, 239, 172);
                _txtUyarilar.Text = "Tüm sorular sorunsuz ayrıştırıldı ve SoruDogrulayici denetiminden geçti.";
            }
            else
            {
                _txtUyarilar.ForeColor = Color.FromArgb(254, 215, 170);
                var sb = new StringBuilder();
                foreach (BelgeAyristirmaUyarisi uyari in _sonuc.Uyarilar)
                    sb.AppendLine("• " + uyari);
                _txtUyarilar.Text = sb.ToString().TrimEnd();
            }
        }

        private void GridSorular_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == _gridSorular.Columns["Aktar"]!.Index)
                OzetVeButonGuncelle();
        }

        private void SeciliSoruyuGoster(BelgeSoruKaydi kayit)
        {
            Soru s = kayit.Soru;
            var sb = new StringBuilder();
            sb.AppendLine($"Soru {kayit.SoruNo}   •   Modül: {DisiplinAdi(s.Disiplin)}   •   Zorluk: {s.Zorluk}");
            if (!string.IsNullOrWhiteSpace(s.Etiket))
                sb.AppendLine($"Etiket: {s.Etiket}");
            sb.AppendLine();
            sb.AppendLine(string.IsNullOrWhiteSpace(s.Baslik) ? "(Soru metni eksik)" : s.Baslik);

            if (!string.IsNullOrWhiteSpace(s.KodBlogu))
            {
                sb.AppendLine();
                sb.AppendLine("[Kod Bloğu]");
                sb.AppendLine(s.KodBlogu);
            }

            sb.AppendLine();
            string[] harfler = { "A", "B", "C", "D" };
            for (int i = 0; i < 4; i++)
            {
                string metin = s.Secenekler.ElementAtOrDefault(i) ?? string.Empty;
                string isaret = i == s.DogruSecenekIndex ? "  ✓ (Doğru)" : string.Empty;
                sb.AppendLine($"{harfler[i]}) {(string.IsNullOrWhiteSpace(metin) ? "(EKSİK)" : metin)}{isaret}");
            }

            if (s.YanlisCevapAciklamalari.Count > 0)
            {
                string? ilkAciklama = s.YanlisCevapAciklamalari.Values.FirstOrDefault();
                bool hepsiAyni = s.YanlisCevapAciklamalari.Values.Distinct().Count() == 1;
                sb.AppendLine();
                if (hepsiAyni && ilkAciklama != null)
                {
                    sb.AppendLine($"Açıklama / Neden Yanlış: {ilkAciklama}");
                }
                else
                {
                    sb.AppendLine("Neden Yanlış Açıklamaları:");
                    foreach (var kv in s.YanlisCevapAciklamalari.OrderBy(k => k.Key))
                    {
                        if (kv.Key is >= 0 and <= 3)
                            sb.AppendLine($"  - {harfler[kv.Key]}: {kv.Value}");
                    }
                }
            }

            if (kayit.Hatalar.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Doğrulama Hataları:");
                foreach (string hata in kayit.Hatalar)
                    sb.AppendLine($"  ✕ {hata}");
            }

            _txtSeciliDetay.Text = sb.ToString().TrimEnd();
        }

        private void OzetVeButonGuncelle()
        {
            int seciliGecerli = SeciliGecerliSorulariAl().Count;
            _lblOzet.Text =
                $"Toplam Tespit: {_sonuc.Kayitlar.Count}   •   " +
                $"Geçerli: {_sonuc.GecerliSayisi}   •   " +
                $"Hatalı/Eksik: {_sonuc.HataliSayisi}   •   " +
                $"Aktarılacak Seçili: {seciliGecerli}";

            _btnKaydet.Enabled = seciliGecerli > 0;
            _btnKaydet.BackColor = seciliGecerli > 0
                ? Color.FromArgb(22, 163, 74)
                : Color.FromArgb(71, 85, 105);
            _btnKaydet.Text = seciliGecerli > 0
                ? $"✓ Onayla ve Kaydet ({seciliGecerli} Soru)"
                : "Kaydedilecek Geçerli Soru Yok";
        }

        private List<Soru> SeciliGecerliSorulariAl()
        {
            var secilenler = new List<Soru>();
            foreach (DataGridViewRow satir in _gridSorular.Rows)
            {
                bool isaretli = satir.Cells["Aktar"].Value is bool b && b;
                if (isaretli && satir.Tag is BelgeSoruKaydi kayit && kayit.GecerliMi && SoruDogrulayici.GecerliMi(kayit.Soru))
                {
                    secilenler.Add(kayit.Soru);
                }
            }
            return secilenler;
        }

        private void BtnKaydet_Click(object? sender, EventArgs e)
        {
            List<Soru> secilenler = SeciliGecerliSorulariAl();
            if (secilenler.Count == 0)
            {
                MessageBox.Show(
                    "Veritabanına aktarılacak geçerli soru seçilmedi.",
                    "Aktarılacak Soru Yok",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            OnaylananSorular = secilenler;
            DialogResult = DialogResult.OK;
            Close();
        }

        private static string DisiplinAdi(DisiplinTuru disiplin) => disiplin switch
        {
            DisiplinTuru.GorselProgramlama => "Görsel Programlama",
            DisiplinTuru.VeriTabanlari => "Veri Tabanları",
            DisiplinTuru.SiberGuvenlik => "Siber Güvenlik",
            _ => disiplin.ToString()
        };

        private sealed class ModulSecenegi
        {
            public DisiplinTuru Disiplin { get; }
            public string Ad { get; }

            public ModulSecenegi(DisiplinTuru disiplin, string ad)
            {
                Disiplin = disiplin;
                Ad = ad;
            }

            public override string ToString() => Ad;
        }
    }
}
