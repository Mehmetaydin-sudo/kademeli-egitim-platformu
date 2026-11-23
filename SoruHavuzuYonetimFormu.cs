using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using KademeliEgitimPlatformu.Models;
using KademeliEgitimPlatformu.Services;

namespace KademeliEgitimPlatformu
{
    /// <summary>
    /// PostgreSQL soru havuzundaki tüm soruları listeleme, arama, filtreleme,
    /// ekleme/düzenleme/silme, dışa aktarma (.xlsx/.csv) ve örnek şablon (.docx/.xlsx)
    /// oluşturma işlemlerini sunan yönetim penceresi.
    /// </summary>
    public sealed class SoruHavuzuYonetimFormu : Form
    {
        private readonly ISoruDeposu _soruDeposu;
        private List<Soru> _tumSorular = new();

        private TextBox _txtArama = null!;
        private ComboBox _cmbModulFiltre = null!;
        private ComboBox _cmbZorlukFiltre = null!;
        private Label _lblSayac = null!;
        private DataGridView _gridSorular = null!;
        private Button _btnDuzenle = null!;
        private Button _btnSil = null!;

        /// <summary>Pencere açıkken soru havuzunda ekleme, güncelleme veya silme yapıldı mı?</summary>
        public bool DegisiklikYapildi { get; private set; }

        public SoruHavuzuYonetimFormu(ISoruDeposu soruDeposu)
        {
            _soruDeposu = soruDeposu ?? throw new ArgumentNullException(nameof(soruDeposu));
            ArayuzuOlustur();
            SorulariYukle();
        }

        private void ArayuzuOlustur()
        {
            Text = "Soru Havuzu Yönetimi — Kademeli Eğitim Platformu";
            AutoScaleMode = AutoScaleMode.Dpi;
            Size = new Size(1220, 740);
            MinimumSize = new Size(940, 600);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(15, 23, 42);
            ForeColor = Color.White;
            Padding = new Padding(18);

            var anaIzgara = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            anaIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));

            // 1) ÜST FİLTRE VE ARAMA ÇUBUĞU
            var pnlFiltre = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(22, 32, 50),
                Padding = new Padding(14, 10, 14, 10),
                Margin = new Padding(0, 0, 0, 10)
            };

            var filtreIzgara = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1
            };
            filtreIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44F));
            filtreIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));
            filtreIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16F));
            filtreIzgara.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));

            _txtArama = new TextBox
            {
                Dock = DockStyle.Bottom,
                Font = FontOnbellegi.Al("Segoe UI", 10f),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Soru metni, şık veya etiket içinde ara..."
            };
            _txtArama.TextChanged += (_, _) => TabloyuFiltrele();

            _cmbModulFiltre = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Bottom,
                Font = FontOnbellegi.Al("Segoe UI", 9.5f),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White
            };
            _cmbModulFiltre.Items.AddRange(new object[] { "Tüm Modüller", "Görsel Programlama", "Veri Tabanları", "Siber Güvenlik" });
            _cmbModulFiltre.SelectedIndex = 0;
            _cmbModulFiltre.SelectedIndexChanged += (_, _) => TabloyuFiltrele();

            _cmbZorlukFiltre = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Bottom,
                Font = FontOnbellegi.Al("Segoe UI", 9.5f),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White
            };
            _cmbZorlukFiltre.Items.AddRange(new object[] { "Tüm Zorluklar", "Kolay", "Orta", "Zor" });
            _cmbZorlukFiltre.SelectedIndex = 0;
            _cmbZorlukFiltre.SelectedIndexChanged += (_, _) => TabloyuFiltrele();

            _lblSayac = new Label
            {
                Dock = DockStyle.Fill,
                Font = FontOnbellegi.Al("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(125, 211, 252),
                TextAlign = ContentAlignment.BottomRight,
                Padding = new Padding(0, 0, 0, 4)
            };

            filtreIzgara.Controls.Add(EtiketliKutu("Arama:", _txtArama, new Padding(0, 0, 12, 0)), 0, 0);
            filtreIzgara.Controls.Add(EtiketliKutu("Modül:", _cmbModulFiltre, new Padding(0, 0, 12, 0)), 1, 0);
            filtreIzgara.Controls.Add(EtiketliKutu("Zorluk:", _cmbZorlukFiltre, new Padding(0, 0, 12, 0)), 2, 0);
            filtreIzgara.Controls.Add(_lblSayac, 3, 0);
            pnlFiltre.Controls.Add(filtreIzgara);

            // 2) ORTA TABLO
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
                ReadOnly = true,
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
                Font = FontOnbellegi.Al("Segoe UI", 9.5f, FontStyle.Bold)
            };
            _gridSorular.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(22, 32, 50),
                ForeColor = Color.White,
                SelectionBackColor = Color.FromArgb(37, 99, 235),
                SelectionForeColor = Color.White,
                Font = FontOnbellegi.Al("Segoe UI", 9.5f)
            };

            _gridSorular.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", FillWeight = 22, MinimumWidth = 44 });
            _gridSorular.Columns.Add(new DataGridViewTextBoxColumn { Name = "Modul", HeaderText = "Modül", FillWeight = 64, MinimumWidth = 120 });
            _gridSorular.Columns.Add(new DataGridViewTextBoxColumn { Name = "Zorluk", HeaderText = "Zorluk", FillWeight = 34, MinimumWidth = 60 });
            _gridSorular.Columns.Add(new DataGridViewTextBoxColumn { Name = "Etiket", HeaderText = "Etiket", FillWeight = 46, MinimumWidth = 80 });
            _gridSorular.Columns.Add(new DataGridViewTextBoxColumn { Name = "Baslik", HeaderText = "Soru Metni", FillWeight = 210, MinimumWidth = 260 });
            _gridSorular.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cevap", HeaderText = "Doğru", FillWeight = 26, MinimumWidth = 52 });

            _gridSorular.SelectionChanged += (_, _) => SecimButonlariniGuncelle();
            _gridSorular.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0) SeciliSoruyuDuzenle();
            };

            // 3) ALT İŞLEM ÇUBUĞU
            var pnlAlt = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) };

            var btnYeni = AltButon("➕ Yeni Soru", Color.FromArgb(22, 163, 74), 130, DockStyle.Left);
            btnYeni.Click += (_, _) => YeniSoruEkle();

            _btnDuzenle = AltButon("✎ Düzenle", Color.FromArgb(37, 99, 235), 118, DockStyle.Left);
            _btnDuzenle.Click += (_, _) => SeciliSoruyuDuzenle();

            _btnSil = AltButon("🗑️ Sil", Color.FromArgb(185, 28, 28), 96, DockStyle.Left);
            _btnSil.Click += (_, _) => SeciliSoruyuSil();

            var btnSablon = AltButon("📄 Örnek Şablon Oluştur", Color.FromArgb(124, 58, 237), 205, DockStyle.Right);
            btnSablon.Click += (_, _) => OrnekSablonOlustur_Click();

            var btnDisariAktar = AltButon("📤 Dışa Aktar (.xlsx/.csv)", Color.FromArgb(14, 116, 144), 205, DockStyle.Right);
            btnDisariAktar.Click += DisariAktar_Click;

            var btnKapat = AltButon("Kapat", Color.FromArgb(51, 65, 85), 96, DockStyle.Right);
            btnKapat.Click += (_, _) => Close();

            pnlAlt.Controls.Add(_btnSil);
            pnlAlt.Controls.Add(new Panel { Width = 8, Dock = DockStyle.Left });
            pnlAlt.Controls.Add(_btnDuzenle);
            pnlAlt.Controls.Add(new Panel { Width = 8, Dock = DockStyle.Left });
            pnlAlt.Controls.Add(btnYeni);

            pnlAlt.Controls.Add(btnSablon);
            pnlAlt.Controls.Add(new Panel { Width = 8, Dock = DockStyle.Right });
            pnlAlt.Controls.Add(btnDisariAktar);
            pnlAlt.Controls.Add(new Panel { Width = 8, Dock = DockStyle.Right });
            pnlAlt.Controls.Add(btnKapat);

            anaIzgara.Controls.Add(pnlFiltre, 0, 0);
            anaIzgara.Controls.Add(_gridSorular, 0, 1);
            anaIzgara.Controls.Add(pnlAlt, 0, 2);

            Controls.Add(anaIzgara);
            CancelButton = btnKapat;
        }

        private static Panel EtiketliKutu(string baslik, Control kontrol, Padding margin)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Margin = margin };
            var lbl = new Label
            {
                Text = baslik,
                Font = FontOnbellegi.Al("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Top,
                Height = 18
            };
            panel.Controls.Add(kontrol);
            panel.Controls.Add(lbl);
            return panel;
        }

        private static Button AltButon(string metin, Color arkaPlan, int genislik, DockStyle dock)
        {
            var btn = new Button
            {
                Text = metin,
                Font = FontOnbellegi.Al("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = arkaPlan,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(genislik, 42),
                Dock = dock,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void SorulariYukle()
        {
            try
            {
                _tumSorular = _soruDeposu.TumSorulariGetir();
                TabloyuFiltrele();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Soru havuzu PostgreSQL veritabanından yüklenemedi.\n\n" +
                    VeritabaniBaglantisi.KullaniciDostuHataMesaji(ex),
                    "Veritabanı Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void TabloyuFiltrele()
        {
            string aranan = _txtArama.Text.Trim();
            int modulIndex = _cmbModulFiltre.SelectedIndex;
            int zorlukIndex = _cmbZorlukFiltre.SelectedIndex;

            IEnumerable<Soru> suzulen = _tumSorular;

            if (modulIndex > 0)
                suzulen = suzulen.Where(s => (int)s.Disiplin == modulIndex - 1);

            if (zorlukIndex > 0)
                suzulen = suzulen.Where(s => (int)s.Zorluk == zorlukIndex - 1);

            if (!string.IsNullOrWhiteSpace(aranan))
            {
                suzulen = suzulen.Where(s =>
                    s.Baslik.Contains(aranan, StringComparison.OrdinalIgnoreCase) ||
                    (s.Etiket?.Contains(aranan, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    s.Secenekler.Any(sec => sec.Contains(aranan, StringComparison.OrdinalIgnoreCase)));
            }

            List<Soru> liste = suzulen.ToList();

            _gridSorular.Rows.Clear();
            foreach (Soru s in liste)
            {
                string cevap = s.DogruSecenekIndex is >= 0 and <= 3
                    ? ((char)('A' + s.DogruSecenekIndex)).ToString()
                    : "—";

                int idx = _gridSorular.Rows.Add(
                    s.Id,
                    DisiplinAdi(s.Disiplin),
                    s.Zorluk.ToString(),
                    s.Etiket ?? "—",
                    s.Baslik,
                    cevap);
                _gridSorular.Rows[idx].Tag = s;
            }

            _lblSayac.Text = $"Gösterilen: {liste.Count} / {_tumSorular.Count} Soru";
            SecimButonlariniGuncelle();
        }

        private void SecimButonlariniGuncelle()
        {
            bool secili = _gridSorular.SelectedRows.Count > 0 && _gridSorular.SelectedRows[0].Tag is Soru;
            _btnDuzenle.Enabled = secili;
            _btnSil.Enabled = secili;
        }

        private void YeniSoruEkle()
        {
            var yeni = new Soru
            {
                Disiplin = DisiplinTuru.GorselProgramlama,
                Zorluk = ZorlukSeviyesi.Orta,
                Secenekler = new List<string> { "", "", "", "" },
                DogruSecenekIndex = -1
            };

            using var dlg = new SoruDuzenleFormu(yeni);
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            if (!SoruDogrulayici.GecerliMi(yeni))
            {
                MessageBox.Show(
                    "Soru geçerli değil; veritabanına eklenemedi.\n\n" + SoruDogrulayici.Dogrula(yeni).HataOzeti,
                    "Doğrulama Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int eklenen = _soruDeposu.SorulariVeritabaninaAktar(new[] { yeni });
                if (eklenen > 0)
                {
                    DegisiklikYapildi = true;
                    SorulariYukle();
                }
                else
                {
                    MessageBox.Show(
                        "Aynı başlığa sahip bir soru zaten veritabanında mevcut.",
                        "Mükerrer Soru",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Yeni soru kaydedilemedi.\n\n" + VeritabaniBaglantisi.KullaniciDostuHataMesaji(ex),
                    "Kayıt Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void SeciliSoruyuDuzenle()
        {
            if (_gridSorular.SelectedRows.Count == 0 || _gridSorular.SelectedRows[0].Tag is not Soru secili)
                return;

            var kopya = new Soru
            {
                Id = secili.Id,
                Disiplin = secili.Disiplin,
                Zorluk = secili.Zorluk,
                Etiket = secili.Etiket,
                Baslik = secili.Baslik,
                KodBlogu = secili.KodBlogu,
                Secenekler = new List<string>(secili.Secenekler),
                DogruSecenekIndex = secili.DogruSecenekIndex,
                YanlisCevapAciklamalari = new Dictionary<int, string>(secili.YanlisCevapAciklamalari)
            };

            using var dlg = new SoruDuzenleFormu(kopya, secili.Id);
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                if (_soruDeposu.SoruGuncelle(kopya))
                {
                    DegisiklikYapildi = true;
                    SorulariYukle();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Soru güncellenemedi.\n\n" + VeritabaniBaglantisi.KullaniciDostuHataMesaji(ex),
                    "Güncelleme Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void SeciliSoruyuSil()
        {
            if (_gridSorular.SelectedRows.Count == 0 || _gridSorular.SelectedRows[0].Tag is not Soru secili)
                return;

            DialogResult onay = MessageBox.Show(
                $"#{secili.Id} numaralı soruyu veritabanından kalıcı olarak silmek istediğinize emin misiniz?\n\n\"{secili.Baslik}\"",
                "Soru Silme Onayı",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (onay != DialogResult.Yes)
                return;

            try
            {
                if (_soruDeposu.SoruSil(secili.Id))
                {
                    DegisiklikYapildi = true;
                    SorulariYukle();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Soru silinemedi.\n\n" + VeritabaniBaglantisi.KullaniciDostuHataMesaji(ex),
                    "Silme Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async void DisariAktar_Click(object? sender, EventArgs e)
        {
            string belgelerim = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            using var sfd = new SaveFileDialog
            {
                Title = "Soru Havuzunu Dışa Aktar",
                Filter = "Excel Dosyası (*.xlsx)|*.xlsx|CSV Dosyası (*.csv)|*.csv",
                DefaultExt = "xlsx",
                AddExtension = true,
                FileName = $"SoruHavuzu_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
                InitialDirectory = Directory.Exists(belgelerim) ? belgelerim : VeriYolu.VeriKlasoru
            };

            if (sfd.ShowDialog(this) != DialogResult.OK)
                return;

            Cursor onceki = Cursor;
            try
            {
                Cursor = Cursors.WaitCursor;
                string hedefYol = sfd.FileName;
                int adet = await Task.Run(() => _soruDeposu.DisariAktar(hedefYol));

                if (!IsDisposed)
                {
                    MessageBox.Show(
                        $"Soru havuzu başarıyla dışa aktarıldı!\n\nDosya: {hedefYol}\nAktarılan Soru Sayısı: {adet}",
                        "Dışa Aktarım Başarılı",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                {
                    MessageBox.Show(
                        "Soru havuzu dışa aktarılamadı.\n\n" + VeritabaniBaglantisi.KullaniciDostuHataMesaji(ex),
                        "Dışa Aktarım Hatası",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            finally
            {
                if (!IsDisposed) Cursor = onceki;
            }
        }

        private void OrnekSablonOlustur_Click()
        {
            string masaustu = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            using var sfd = new SaveFileDialog
            {
                Title = "Örnek Soru Şablonunu Kaydet",
                Filter = "Word Soru Şablonu (*.docx)|*.docx|Excel Soru Şablonu (*.xlsx)|*.xlsx",
                DefaultExt = "docx",
                AddExtension = true,
                FileName = "Ornek_Soru_Sablonu.docx",
                InitialDirectory = Directory.Exists(masaustu) ? masaustu : VeriYolu.VeriKlasoru
            };

            if (sfd.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                string uzanti = Path.GetExtension(sfd.FileName).ToLowerInvariant();
                if (uzanti == ".xlsx")
                    SoruServisi.OrnekExcelSablonuOlustur(sfd.FileName);
                else
                    BelgeSoruDonusturucu.OrnekWordSablonuOlustur(sfd.FileName);

                MessageBox.Show(
                    $"Örnek soru şablonu oluşturuldu:\n{sfd.FileName}\n\nDosyayı düzenleyip 'Soru İçe Aktar' butonuyla platforma yükleyebilirsiniz.",
                    "Şablon Oluşturuldu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Örnek şablon oluşturulamadı.\n\n" + ex.Message,
                    "Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static string DisiplinAdi(DisiplinTuru disiplin) => disiplin switch
        {
            DisiplinTuru.GorselProgramlama => "Görsel Programlama",
            DisiplinTuru.VeriTabanlari => "Veri Tabanları",
            DisiplinTuru.SiberGuvenlik => "Siber Güvenlik",
            _ => disiplin.ToString()
        };
    }
}
