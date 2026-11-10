using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using KademeliEgitimPlatformu.Models;
using KademeliEgitimPlatformu.Services;

namespace KademeliEgitimPlatformu
{
    public partial class Form1
    {
        // Ekran yeniden çizildiğinde (menü geçişi) editördeki kod ve son analiz
        // sonucu kaybolmasın diye burada tutulur.
        private string _simulatorKodOnbellek = string.Empty;
        private string _simulatorSonucOnbellek = string.Empty;
        private Color _simulatorSonucArka = Color.FromArgb(43, 27, 23);
        private Color _simulatorSonucYazi = Color.FromArgb(251, 146, 60);

        private Label _lblSimBeceriGorsel = new();
        private Label _lblSimBeceriVeri = new();
        private Label _lblSimBeceriSiber = new();

        private const string VarsayilanSimulatorKodu =
            "// GÖREV: Aşağıdaki 2 gizli syntax hatasını düzeltin:\n" +
            "// 1. if bloğunun sonundaki noktalı virgül (;) tuzağını kaldırın.\n" +
            "// 2. Atama operatörü (=) yerine karşılaştırma (==) kullanın.\n\n" +
            "public void YetkiKontrol(int kullaniciRol)\n" +
            "{\n" +
            "    int adminRol = 1;\n\n" +
            "    if (kullaniciRol = adminRol);\n" +
            "    {\n" +
            "        Console.WriteLine(\"Yönetici girişi başarılı.\");\n" +
            "    }\n" +
            "}";

        // ==================== 3. SİMÜLATÖR + HATA KARNESİ ====================
        private void SimulatorEkraniniCiz()
        {
            // Yeniden çizimden önce kullanıcının yazdığı kodu ve sonucu sakla.
            if (!string.IsNullOrWhiteSpace(_txtSimulatorKod.Text))
                _simulatorKodOnbellek = _txtSimulatorKod.Text;

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
                Font = FontOnbellegi.Al("Segoe UI", 19, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Left,
                AutoSize = true
            };

            Button btnGeri = new()
            {
                Text = "✕ Ana Menüye Dön",
                Font = FontOnbellegi.Al("Segoe UI", 10f, FontStyle.Bold),
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

            // SOL PANEL: Roslyn destekli canlı editör
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
                Text = "Zor Mod — Gizli Syntax Tuzağı Analizi (Roslyn Derleyicisi)",
                Font = FontOnbellegi.Al("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Dock = DockStyle.Fill
            };

            Label lblSimYonerge = new()
            {
                Text = "Aşağıdaki C# kodundaki gizli sözdizimi/mantık tuzaklarını düzeltip 'Kontrolcüyü Çalıştır' butonuna basın. " +
                       "Kod gerçek Roslyn derleyicisiyle, tamamen çevrimdışı denetlenir.",
                Font = FontOnbellegi.Al("Segoe UI", 10f),
                ForeColor = Color.FromArgb(203, 213, 225),
                Dock = DockStyle.Fill
            };

            _txtSimulatorKod = new RichTextBox
            {
                Font = FontOnbellegi.Al("Consolas", 12.5f),
                BackColor = Color.FromArgb(9, 13, 22),
                ForeColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                AcceptsTab = true,
                Text = string.IsNullOrWhiteSpace(_simulatorKodOnbellek)
                    ? VarsayilanSimulatorKodu
                    : _simulatorKodOnbellek
            };

            Button btnAnalizEt = new()
            {
                Text = "▶ Yerel Roslyn Kontrolcüsünü Çalıştır",
                Font = FontOnbellegi.Al("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.FromArgb(15, 23, 42),
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 8, 0, 8),
                Cursor = Cursors.Hand
            };
            btnAnalizEt.FlatAppearance.BorderSize = 0;
            btnAnalizEt.Click += SimulatorAnalizEt_Click;

            _txtSimulatorSonuc = new RichTextBox
            {
                Text = string.IsNullOrWhiteSpace(_simulatorSonucOnbellek)
                    ? "⚠️ Yerel Roslyn Kontrolcüsü Bekliyor...\n\nKodu düzeltip butona bastığınızda hatalar satır ve sütun numarasıyla listelenir."
                    : _simulatorSonucOnbellek,
                Font = FontOnbellegi.Al("Consolas", 10f),
                ForeColor = string.IsNullOrWhiteSpace(_simulatorSonucOnbellek)
                    ? Color.FromArgb(251, 146, 60)
                    : _simulatorSonucYazi,
                BackColor = string.IsNullOrWhiteSpace(_simulatorSonucOnbellek)
                    ? Color.FromArgb(43, 27, 23)
                    : _simulatorSonucArka,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both
            };

            solIzgara.Controls.Add(lblSimBaslik, 0, 0);
            solIzgara.Controls.Add(lblSimYonerge, 0, 1);
            solIzgara.Controls.Add(_txtSimulatorKod, 0, 2);
            solIzgara.Controls.Add(btnAnalizEt, 0, 3);
            solIzgara.Controls.Add(_txtSimulatorSonuc, 0, 4);
            pnlSol.Controls.Add(solIzgara);

            // SAĞ PANEL: Hata karnesi ve rozetler
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

            Label lblKarneBaslik = new()
            {
                Text = "📊 Çevrimdışı Hata Karnesi & Beceri Düzeyleri",
                Font = FontOnbellegi.Al("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill
            };

            _lblSimBeceriGorsel = new Label
            {
                Text = $"• Görsel Programlama Yetkinliği: %{_gorselBasari}",
                Font = FontOnbellegi.Al("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 184, 166),
                Dock = DockStyle.Fill
            };

            _lblSimBeceriVeri = new Label
            {
                Text = $"• Veri Tabanları Yetkinliği: %{_veriTabaniBasari}",
                Font = FontOnbellegi.Al("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 158, 11),
                Dock = DockStyle.Fill
            };

            _lblSimBeceriSiber = new Label
            {
                Text = $"• Siber Güvenlik Yetkinliği: %{_siberBasari}   |   🔑 İpucu Jetonu: {_ipucuJetonu}",
                Font = FontOnbellegi.Al("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(236, 72, 153),
                Dock = DockStyle.Fill
            };

            Label lblRozetBaslik = new()
            {
                Text = "🏆 Kazanılan Yerel Oyunlaştırma Rozetleri",
                Font = FontOnbellegi.Al("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill
            };

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

            rozetIzgara.Controls.Add(RozetKarti(
                "🔍 Hata Avcısı",
                _simulatorOduluAlindi ? "Kazanıldı — gizli tuzakları buldu." : "Gizli syntax tuzaklarını buldu.",
                Color.FromArgb(239, 68, 68)), 0, 0);
            rozetIzgara.Controls.Add(RozetKarti(
                "🛠️ Kararlı Geliştirici",
                "%100 çevrimdışı modülleri tamamladı.",
                Color.FromArgb(245, 158, 11)), 1, 0);
            rozetIzgara.Controls.Add(RozetKarti(
                "🗄️ SQL Ustası",
                "İlişkisel sorgu testlerini geçti.",
                Color.FromArgb(56, 189, 248)), 0, 1);
            rozetIzgara.Controls.Add(RozetKarti(
                "🛡️ Siber Muhafız",
                "Hash ve güvenli kodlama barajını aştı.",
                Color.FromArgb(34, 197, 94)), 1, 1);

            Button btnKarneIndir = new()
            {
                Text = "💾 Çevrimdışı Hata Karnesini (.txt) Kaydet",
                Font = FontOnbellegi.Al("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 6, 0, 0),
                Cursor = Cursors.Hand
            };
            btnKarneIndir.FlatAppearance.BorderSize = 0;
            btnKarneIndir.Click += KarneIndir_Click;

            sagIzgara.Controls.Add(lblKarneBaslik, 0, 0);
            sagIzgara.Controls.Add(_lblSimBeceriGorsel, 0, 1);
            sagIzgara.Controls.Add(_lblSimBeceriVeri, 0, 2);
            sagIzgara.Controls.Add(_lblSimBeceriSiber, 0, 3);
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
            Label l2 = new()
            {
                Text = aciklama,
                Font = FontOnbellegi.Al("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(203, 213, 225),
                Dock = DockStyle.Fill
            };
            Label l1 = new()
            {
                Text = ad,
                Font = FontOnbellegi.Al("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = renk,
                Dock = DockStyle.Top,
                Height = 26
            };
            p.Controls.Add(l2);
            p.Controls.Add(l1);
            return p;
        }

        private void SimulatorAnalizEt_Click(object? sender, EventArgs e)
        {
            SimulatorSonucu sonuc;
            System.Windows.Forms.Cursor oncekiImlec = this.Cursor;
            try
            {
                this.Cursor = Cursors.WaitCursor;
                sonuc = _simulatorServisi.Derle(_txtSimulatorKod.Text);
            }
            finally
            {
                this.Cursor = oncekiImlec;
            }

            _simulatorSonucOnbellek = SimulatorServisi.RaporMetni(sonuc);

            if (sonuc.Basarili)
            {
                _simulatorSonucArka = Color.FromArgb(20, 50, 35);
                _simulatorSonucYazi = Color.FromArgb(74, 222, 128);

                if (!_simulatorOduluAlindi)
                {
                    _simulatorOduluAlindi = true;
                    _siberBasari = Math.Min(100, _siberBasari + 25);
                    KaliciIlerlemeyiKaydet();

                    _simulatorSonucOnbellek +=
                        "\n\n🏆 'Hata Avcısı' rozeti kazanıldı! Siber Güvenlik yetkinliğine +25 puan eklendi.";
                }
            }
            else
            {
                _simulatorSonucArka = Color.FromArgb(69, 26, 26);
                _simulatorSonucYazi = Color.FromArgb(252, 165, 165);
            }

            _txtSimulatorSonuc.Text = _simulatorSonucOnbellek;
            _txtSimulatorSonuc.BackColor = _simulatorSonucArka;
            _txtSimulatorSonuc.ForeColor = _simulatorSonucYazi;
            _txtSimulatorSonuc.SelectionStart = 0;
            _txtSimulatorSonuc.ScrollToCaret();

            BeceriEtiketleriniGuncelle();
        }

        private void BeceriEtiketleriniGuncelle()
        {
            _lblSimBeceriGorsel.Text = $"• Görsel Programlama Yetkinliği: %{_gorselBasari}";
            _lblSimBeceriVeri.Text = $"• Veri Tabanları Yetkinliği: %{_veriTabaniBasari}";
            _lblSimBeceriSiber.Text = $"• Siber Güvenlik Yetkinliği: %{_siberBasari}   |   🔑 İpucu Jetonu: {_ipucuJetonu}";
        }

        private void KarneIndir_Click(object? sender, EventArgs e)
        {
            string masaustu = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            using SaveFileDialog sfd = new()
            {
                Title = "Çevrimdışı Hata Karnesini Kaydet",
                Filter = "Metin Dosyası (*.txt)|*.txt",
                DefaultExt = "txt",
                AddExtension = true,
                FileName = $"Cevrimdisi_Hata_Karnesi_{DateTime.Now:yyyyMMdd_HHmm}.txt",
                InitialDirectory = Directory.Exists(masaustu) ? masaustu : string.Empty
            };

            if (sfd.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                var durum = new IlerlemeDurumu(
                    _gorselBasari, _veriTabaniBasari, _siberBasari, _ipucuJetonu, _simulatorOduluAlindi);

                // Geçmiş tüm yanlış cevaplar (en fazla 300 kayıt) raporda yer alır.
                var yanlislar = _karneServisi.SonKayitlar(KarneServisi.MaksKayitSayisi);
                string rapor = KarneRaporu.Olustur(durum, yanlislar, raporTarihi: DateTime.Now, surum: Surum);

                // UTF-8 BOM: Windows Not Defteri Türkçe karakterleri doğru gösterir.
                File.WriteAllText(sfd.FileName, rapor, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

                MessageBox.Show(
                    $"Hata karneniz kaydedildi:\n{sfd.FileName}\n\nKayıtlı yanlış cevap sayısı: {yanlislar.Count}",
                    "Rapor Kaydedildi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(
                    "Seçilen konuma yazma izni yok. Farklı bir klasör seçin.\n\n" + ex.Message,
                    "Erişim Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Rapor kaydedilemedi.\n\n" + ex.Message,
                    "Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
