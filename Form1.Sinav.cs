using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using KademeliEgitimPlatformu.Models;
using KademeliEgitimPlatformu.Services;

namespace KademeliEgitimPlatformu
{
    public partial class Form1
    {
        // ==================== 2. SINAV EKRANI ====================
        private void SinavEkraniniCiz()
        {
            _pnlSinavEkrani.SuspendLayout();
            PanelTemizle(_pnlSinavEkrani);
            _pnlSinavEkrani.Dock = DockStyle.Fill;
            _pnlSinavEkrani.Padding = new Padding(45, 25, 45, 25);
            _pnlSinavEkrani.BackColor = Color.FromArgb(15, 23, 42);

            TableLayoutPanel sinavIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6
            };
            sinavIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            sinavIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 68F));
            sinavIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));
            sinavIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));
            sinavIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            sinavIzgara.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));

            Panel pnlUst = new() { Dock = DockStyle.Fill };
            _lblDisiplinBaslik = new()
            {
                Text = "Görsel Programlama — Orta Mod",
                Font = FontOnbellegi.Al("Segoe UI", 17, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                AutoSize = true
            };

            _lblSoruSayaci = new()
            {
                Text = "Soru 1 / 3",
                Font = FontOnbellegi.Al("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Dock = DockStyle.Bottom,
                AutoSize = true
            };

            Button btnAnaMenuyeDon = new()
            {
                Text = "✕ Kapat",
                Font = FontOnbellegi.Al("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 40),
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };
            btnAnaMenuyeDon.FlatAppearance.BorderSize = 0;
            btnAnaMenuyeDon.Click += (s, e) => EkranDegistir("ANA");

            _lblSureSayaci = new()
            {
                Text = "⏱️ 00:00   ",
                Font = FontOnbellegi.Al("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = true
            };

            _lblJetonSayaci = new()
            {
                Text = $"🔑 İpucu Jetonu: {_ipucuJetonu}   ",
                Font = FontOnbellegi.Al("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = true
            };

            pnlUst.Controls.Add(_lblDisiplinBaslik);
            pnlUst.Controls.Add(_lblSoruSayaci);
            pnlUst.Controls.Add(_lblJetonSayaci);
            pnlUst.Controls.Add(_lblSureSayaci);
            pnlUst.Controls.Add(btnAnaMenuyeDon);

            _lblSoruMetni = new()
            {
                Font = FontOnbellegi.Al("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(241, 245, 249),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            Panel pnlKodKapsayici = CerceveliPanelOlustur(Color.FromArgb(51, 65, 85), Color.FromArgb(9, 13, 22), 1);
            pnlKodKapsayici.Margin = new Padding(0, 4, 0, 10);

            Panel pnlKodUstBar = new()
            {
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = Color.FromArgb(19, 27, 46),
                Padding = new Padding(12, 4, 0, 0)
            };
            Label lblKodDosyaAdi = new()
            {
                Text = "🔴 🟡 🟢    OrnekKodBlogu.cs — Salt Okunur İnceleme Modu",
                Font = FontOnbellegi.Al("Consolas", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true
            };
            pnlKodUstBar.Controls.Add(lblKodDosyaAdi);

            _lblKodSatirNo = new RichTextBox
            {
                Text = "1\n2\n3\n4\n5\n6\n7\n8",
                Font = FontOnbellegi.Al("Consolas", 12.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                BackColor = Color.FromArgb(13, 18, 30),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.None,
                Width = 46,
                Dock = DockStyle.Left,
                TabStop = false
            };

            _txtKodBlogu = new()
            {
                Font = FontOnbellegi.Al("Consolas", 12.5f),
                BackColor = Color.FromArgb(9, 13, 22),
                ForeColor = Color.FromArgb(125, 211, 252),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                Dock = DockStyle.Fill
            };
            _txtKodBlogu.VScroll += (s, e) => KodSatirlariniHizala();

            pnlKodKapsayici.Controls.Add(_txtKodBlogu);
            pnlKodKapsayici.Controls.Add(_lblKodSatirNo);
            pnlKodKapsayici.Controls.Add(pnlKodUstBar);

            TableLayoutPanel siklarIzgara = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Margin = new Padding(0, 4, 0, 8)
            };
            for (int i = 0; i < 4; i++)
            {
                siklarIzgara.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
                int secenekIndex = i;
                Button btn = new()
                {
                    Font = FontOnbellegi.Al("Segoe UI", 11.5f),
                    BackColor = Color.FromArgb(24, 34, 52),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(18, 0, 0, 0),
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0, 4, 0, 4),
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(38, 52, 77);
                btn.Click += (s, e) => SecenekSecildi(secenekIndex);

                _secenekButonlari[i] = btn;
                siklarIzgara.Controls.Add(btn, 0, i);
            }

            _pnlNedenYanlis = CerceveliPanelOlustur(Color.FromArgb(234, 88, 12), Color.FromArgb(43, 27, 23), 1);
            _pnlNedenYanlis.Padding = new Padding(18, 10, 18, 10);
            _pnlNedenYanlis.Visible = false;

            _lblNedenYanlisAciklama = new()
            {
                Font = FontOnbellegi.Al("Segoe UI", 11f),
                ForeColor = Color.FromArgb(254, 215, 170),
                Dock = DockStyle.Fill
            };

            _lblNedenYanlisBaslik = new()
            {
                Text = "Neden Yanlış Açıklaması",
                Font = FontOnbellegi.Al("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(251, 146, 60),
                Dock = DockStyle.Top,
                Height = 28
            };

            _pnlNedenYanlis.Controls.Add(_lblNedenYanlisAciklama);
            _pnlNedenYanlis.Controls.Add(_lblNedenYanlisBaslik);

            Panel pnlAlt = new() { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) };

            _btnIpucuKullan = new()
            {
                Text = "💡 İpucu Kullan (2 Yanlış Şıkkı Ele)",
                Font = FontOnbellegi.Al("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.FromArgb(250, 204, 21),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(300, 46),
                Dock = DockStyle.Left,
                Cursor = Cursors.Hand
            };
            _btnIpucuKullan.FlatAppearance.BorderColor = Color.FromArgb(250, 204, 21);
            _btnIpucuKullan.Click += IpucuKullan_Click;

            _btnSonrakiSoru = new()
            {
                Text = "Sonraki Soru →",
                Font = FontOnbellegi.Al("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(210, 46),
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
            sinavIzgara.Controls.Add(pnlKodKapsayici, 0, 2);
            sinavIzgara.Controls.Add(siklarIzgara, 0, 3);
            sinavIzgara.Controls.Add(_pnlNedenYanlis, 0, 4);
            sinavIzgara.Controls.Add(pnlAlt, 0, 5);

            _pnlSinavEkrani.Controls.Add(sinavIzgara);
            _pnlSinavEkrani.ResumeLayout(true);
        }

        private void SinaviBaslat(DisiplinTuru? disiplin, ZorlukSeviyesi? zorluk, string baslik)
        {
            _aktifSinavTuru = SinavTuru.Normal;
            _aktifSinavDisiplini = disiplin;
            _aktifZorluk = zorluk;

            List<Soru> sorular = disiplin.HasValue
                ? SoruDeposu.SorulariFiltrele(disiplin.Value, zorluk)
                : SoruDeposu.TumSorulariGetir(zorluk);

            SinaviBaslatInternal(sorular, baslik, disiplin, zorluk);
        }

        /// <summary>
        /// Modül ara sınavını başlatır. Sorular "AraSinav" etiketli havuzdan gelir;
        /// geçme barajı %70'tir ve sonuç kalıcı olarak ilerlemeye yazılır.
        /// </summary>
        private void AraSinaviBaslat(DisiplinTuru disiplin)
        {
            List<Soru> sorular = SoruDeposu.AraSinavSorulariniGetir(disiplin);

            if (sorular.Count == 0)
            {
                MessageBox.Show(
                    "Bu modül için henüz ara sınav sorusu bulunamadı.\n\n" +
                    "Soru havuzunu Data/Sorular.json içinde genişletebilir veya USB paket yükleyebilirsiniz.",
                    "Ara Sınav Bulunamadı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            _aktifSinavTuru = SinavTuru.AraSinav;
            _aktifSinavDisiplini = disiplin;
            _aktifZorluk = null;

            SinaviBaslatInternal(sorular, $"{disiplin} — Ara Sınav", disiplin, null);
        }

        private void SinaviBaslatInternal(
            List<Soru> sorular,
            string baslik,
            DisiplinTuru? disiplin,
            ZorlukSeviyesi? zorluk)
        {
            _aktifSorular = sorular;

            if (_aktifSorular.Count == 0)
            {
                MessageBox.Show(
                    "Bu seçimde henüz soru bulunamadı.\n\n" +
                    $"Disiplin: {(disiplin?.ToString() ?? "Tümü")}\n" +
                    $"Zorluk: {(zorluk?.ToString() ?? "Tümü")}\n\n" +
                    "Soru havuzunu Data/Sorular.json ile genişletebilir veya USB paket yükleyebilirsiniz.",
                    "Soru Bulunamadı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Aynı sorular hep aynı sırada gelmesin (ezberleme önlemi).
            Karistir(_aktifSorular);

            // Şıklar da karıştırılır; doğru cevap ve açıklamalar yeni sıraya taşınır.
            _gorunumler = _aktifSorular
                .Select(soru => SoruGorunumu.Olustur(soru, karistir: true))
                .ToList();

            _cevaplar = new List<bool>();
            _oturumYanlislari = new List<YanlisKaydi>();
            _mevcutSoruIndex = 0;
            _gecenSaniye = 0;
            _sinavBaslangic = DateTime.Now;

            // Ekran ilk kez gösteriliyorsa kontroller burada oluşturulur;
            // ardından başlık ve süre etiketleri güvenle güncellenebilir.
            EkranDegistir("SINAV");

            _lblDisiplinBaslik.Text = baslik;
            _lblSureSayaci.Text = "⏱️ 00:00   ";
            _sinavTimer.Start();
            SoruyuEkranaYukle();
        }

        private static void Karistir<T>(IList<T> liste)
        {
            for (int i = liste.Count - 1; i > 0; i--)
            {
                int j = Random.Shared.Next(i + 1);
                (liste[i], liste[j]) = (liste[j], liste[i]);
            }
        }

        // ==================== KOD GÖRÜNTÜLEYİCİ SATIR NUMARALARI ====================
        private const int EM_GETFIRSTVISIBLELINE = 0xCE;
        private const int EM_LINESCROLL = 0xB6;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Kod kutusu kaydırıldığında satır numarası sütununu aynı konuma getirir.
        /// İki kutu da aynı fontu kullandığı için satır yükseklikleri birebir eşleşir.
        /// </summary>
        private void KodSatirlariniHizala()
        {
            if (!_txtKodBlogu.IsHandleCreated || !_lblKodSatirNo.IsHandleCreated)
                return;

            int hedef = (int)SendMessage(_txtKodBlogu.Handle, EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero);
            int mevcut = (int)SendMessage(_lblKodSatirNo.Handle, EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero);

            if (hedef != mevcut)
                SendMessage(_lblKodSatirNo.Handle, EM_LINESCROLL, IntPtr.Zero, (IntPtr)(hedef - mevcut));
        }

        private void SoruyuEkranaYukle()
        {
            Soru soru = _aktifSorular[_mevcutSoruIndex];
            SoruGorunumu gorunum = _gorunumler[_mevcutSoruIndex];

            _lblSoruSayaci.Text =
                $"Soru {_mevcutSoruIndex + 1} / {_aktifSorular.Count}  •  Zorluk: {soru.Zorluk}  •  1-4 tuşlarıyla seç";
            _lblJetonSayaci.Text = $"🔑 İpucu Jetonu: {_ipucuJetonu}   ";
            _lblSoruMetni.Text = soru.Baslik;
            _txtKodBlogu.Text = soru.KodBlogu ?? "// Bu soru için kod bloğu bulunmamaktadır.";

            int satirSayisi = Math.Max(1, _txtKodBlogu.Text.Split('\n').Length);
            var satirlar = new string[satirSayisi];
            for (int s = 0; s < satirSayisi; s++) satirlar[s] = (s + 1).ToString();
            _lblKodSatirNo.Text = string.Join("\n", satirlar);

            // Yeni soruda her iki görünümü de başa sar ve satır numaralarını hizala.
            _txtKodBlogu.SelectionStart = 0;
            _txtKodBlogu.ScrollToCaret();
            _lblKodSatirNo.SelectionStart = 0;
            KodSatirlariniHizala();

            string[] harfler = { "A", "B", "C", "D" };
            for (int i = 0; i < 4; i++)
            {
                _secenekButonlari[i].Text = $"{harfler[i]})  {gorunum.Secenekler[i]}";
                _secenekButonlari[i].BackColor = Color.FromArgb(24, 34, 52);
                _secenekButonlari[i].FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
                _secenekButonlari[i].Enabled = true;
            }

            _btnIpucuKullan.Enabled = true;
            _pnlNedenYanlis.Visible = false;
            _btnSonrakiSoru.Visible = false;
            _btnSonrakiSoru.Text = _mevcutSoruIndex == _aktifSorular.Count - 1
                ? "Sınavı Bitir ✓"
                : "Sonraki Soru →";
        }

        private void SecenekSecildi(int secilenIndex)
        {
            Soru soru = _aktifSorular[_mevcutSoruIndex];
            SoruGorunumu gorunum = _gorunumler[_mevcutSoruIndex];
            bool dogruMu = secilenIndex == gorunum.DogruSecenekIndex;

            // Disiplin bazlı değerlendirme için cevabı kaydet.
            if (_cevaplar.Count > _mevcutSoruIndex)
                _cevaplar[_mevcutSoruIndex] = dogruMu;
            else
                _cevaplar.Add(dogruMu);

            foreach (var btn in _secenekButonlari)
                btn.Enabled = false;
            _btnIpucuKullan.Enabled = false;

            _secenekButonlari[gorunum.DogruSecenekIndex].BackColor = Color.FromArgb(20, 83, 45);
            _secenekButonlari[gorunum.DogruSecenekIndex].FlatAppearance.BorderColor = Color.FromArgb(34, 197, 94);

            if (dogruMu)
            {
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

                string aciklama = gorunum.Aciklama(secilenIndex)
                    ?? "Seçtiğiniz şık bu senaryo için uygun değildir. Yeşil işaretli doğru cevabı inceleyiniz.";
                _lblNedenYanlisAciklama.Text = aciklama;

                // Hata karnesi için soru bazlı kayıt tut.
                _oturumYanlislari.Add(new YanlisKaydi(
                    DateTime.Now,
                    soru.Disiplin,
                    soru.Zorluk,
                    soru.Baslik,
                    gorunum.Secenekler[secilenIndex],
                    gorunum.DogruCevap,
                    aciklama));
            }

            _pnlNedenYanlis.Visible = true;
            _btnSonrakiSoru.Visible = true;
        }

        private void IpucuKullan_Click(object? sender, EventArgs e)
        {
            if (_ipucuJetonu <= 0)
            {
                MessageBox.Show("İpucu jetonunuz kalmadı! Doğru cevaplar vererek yeni jeton kazanabilirsiniz.", "Jeton Bitti", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_aktifSorular.Count == 0 || _mevcutSoruIndex < 0 || _mevcutSoruIndex >= _aktifSorular.Count)
                return;

            int dogruIndex = _gorunumler[_mevcutSoruIndex].DogruSecenekIndex;
            int elenen = 0;
            for (int i = 0; i < 4; i++)
            {
                if (i != dogruIndex && _secenekButonlari[i].Enabled && elenen < 2)
                {
                    _secenekButonlari[i].Enabled = false;
                    _secenekButonlari[i].Text += "  [İpucu ile Elendi]";
                    elenen++;
                }
            }

            _ipucuJetonu--;
            _lblJetonSayaci.Text = $"🔑 İpucu Jetonu: {_ipucuJetonu}   ";
            _btnIpucuKullan.Enabled = false;
            KaliciIlerlemeyiKaydet(); // Jeton harcandığında SQLite'a kaydet
        }

        private void SonrakiSoru_Click(object? sender, EventArgs e)
        {
            _mevcutSoruIndex++;
            if (_mevcutSoruIndex < _aktifSorular.Count)
            {
                SoruyuEkranaYukle();
                return;
            }

            _sinavTimer.Stop();

            int toplam = _aktifSorular.Count;
            int dogru = _cevaplar.Count(c => c);
            int basariYuzdesi = PuanlamaServisi.Yuzde(dogru, toplam);
            TimeSpan sure = TimeSpan.FromSeconds(Math.Max(0, _gecenSaniye));

            // Yanlış cevapları hata karnesi için kalıcı kaydet (rapor hatası sınavı engellemez).
            YanlislariKarneyeKaydet();

            if (_aktifSinavTuru == SinavTuru.AraSinav)
            {
                bool araSinavGecti = KademeServisi.AraSinavGectiMi(dogru, toplam);
                if (araSinavGecti)
                    AraSinaviGectiOlarakIsaretle(_aktifSinavDisiplini);

                KaliciIlerlemeyiKaydet();

                IlerlemeDurumu durum = MevcutIlerleme();
                int gecilen = KademeServisi.GecilenAraSinavSayisi(durum);

                string sonucMesaji = araSinavGecti
                    ? $"🎉 Ara sınavı geçtiniz! ({gecilen}/3 tamamlandı)\n" +
                      (KademeServisi.OrtaModAcikMi(durum)
                          ? "Orta Mod kilidi açıldı."
                          : KademeServisi.EksikAraSinavMetni(durum))
                    : $"⚠️ Ara sınav barajı %{KademeServisi.AraSinavBaraji}. Tekrar deneyebilirsiniz.\n" +
                      KademeServisi.EksikAraSinavMetni(durum);

                MessageBox.Show(
                    $"Ara Sınav Tamamlandı!\n\n" +
                    $"Doğru Sayısı: {dogru} / {toplam}\n" +
                    $"Geçen Süre: {sure:mm\\:ss}\n" +
                    $"Başarı Yüzdesi: %{basariYuzdesi}\n\n{sonucMesaji}",
                    "Ara Sınav Sonucu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                EkranDegistir("ANA");
                return;
            }

            bool barajGecti = basariYuzdesi >= KademeServisi.OrtaModBaraji;

            var sonuclar = PuanlamaServisi.DisiplinBazindaHesapla(_aktifSorular, _cevaplar);
            IlerlemeDurumu mevcutDurum = MevcutIlerleme();
            IlerlemeDurumu guncelDurum = PuanlamaServisi.Birlestir(mevcutDurum, sonuclar, barajGecti ? 1 : 0);

            _gorselBasari = guncelDurum.GorselBasari;
            _veriTabaniBasari = guncelDurum.VeriTabaniBasari;
            _siberBasari = guncelDurum.SiberBasari;
            _ipucuJetonu = guncelDurum.IpucuJetonu;

            // Orta Mod sınavı %75 ile geçilirse Zor Mod kilidi açılır.
            if (_aktifZorluk == ZorlukSeviyesi.Orta && barajGecti)
                _ortaModGecti = true;

            KaliciIlerlemeyiKaydet();

            string barajMesaji = _aktifZorluk == ZorlukSeviyesi.Orta
                ? (barajGecti
                    ? "🎉 TEBRİKLER! Orta Mod sınavını geçtiniz; Zor Mod (kod simülatörü) kilidi açıldı ve +1 İpucu Jetonu kaydedildi!"
                    : "⚠️ Orta Mod barajının altında kaldınız. Zor Mod için sınavı tekrar deneyin.")
                : (barajGecti
                    ? "🎉 TEBRİKLER! %75 başarı barajını geçtiniz ve +1 İpucu Jetonu kaydedildi!"
                    : "⚠️ %75 başarı barajının altında kaldınız. Yanlış açıklamalarını tekrar gözden geçirin.");

            string yanlisBilgi = _oturumYanlislari.Count > 0
                ? $"\nYanlış Sayısı: {_oturumYanlislari.Count} (hata karnesine kaydedildi)"
                : "\nYanlış cevap yok — tebrikler!";

            MessageBox.Show(
                $"Sınav Tamamlandı!\n\nDoğru Sayısı: {dogru} / {toplam}\nGeçen Süre: {sure:mm\\:ss}\nBaşarı Yüzdesi: %{basariYuzdesi}{yanlisBilgi}\n\n{barajMesaji}",
                "Çevrimdışı Hata Karnesi ve Sonuç",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            EkranDegistir("ANA");
        }

        /// <summary>Oturumdaki yanlış cevapları kalıcı hata karnesine yazar.</summary>
        private void YanlislariKarneyeKaydet()
        {
            if (_oturumYanlislari.Count == 0)
                return;

            try
            {
                KarneDeposu.Kaydet(_oturumYanlislari);
            }
            catch (Exception)
            {
                // Karne yazılamasa bile ilerleme ve sınav sonucu korunur.
            }
        }

        /// <summary>Ara sınavı geçilen modülün kalıcı bayrağını işaretler.</summary>
        private void AraSinaviGectiOlarakIsaretle(DisiplinTuru? disiplin)
        {
            switch (disiplin)
            {
                case DisiplinTuru.GorselProgramlama:
                    _gorselAraSinavGecti = true;
                    break;
                case DisiplinTuru.VeriTabanlari:
                    _veriTabaniAraSinavGecti = true;
                    break;
                case DisiplinTuru.SiberGuvenlik:
                    _siberAraSinavGecti = true;
                    break;
            }
        }
    }
}
