using System;
using System.Threading;
using System.Windows.Forms;
using Xunit;

namespace KademeliEgitimPlatformu.Tests
{
    public class Form1SmokeTests
    {
        /// <summary>
        /// Formun tüm ekranlarıyla (ana menü, sınav, simülatör) hatasız
        /// oluşturulabildiğini doğrular. WinForms STA iş parçacığı ister.
        /// </summary>
        [Fact]
        public void Form1_ButunEkranlarla_Olusturulabilir()
        {
            Exception? hata = null;
            string? baslik = null;

            var isParcacigi = new Thread(() =>
            {
                try
                {
                    using var form = new Form1();
                    baslik = form.Text;
                }
                catch (Exception ex)
                {
                    hata = ex;
                }
            })
            {
                IsBackground = true
            };
            isParcacigi.SetApartmentState(ApartmentState.STA);

            isParcacigi.Start();
            bool tamamlandi = isParcacigi.Join(TimeSpan.FromSeconds(60));

            Assert.True(tamamlandi, "Form1 oluşturulması 60 saniyede tamamlanmadı (bekleyen bir ileti kutusu olabilir).");
            Assert.Null(hata);
            Assert.NotNull(baslik);
            Assert.Contains("v0.6.0", baslik);
            Assert.Contains("Roslyn", baslik);
        }

        [Fact]
        public void Form1_KlavyeKisayoluAltyapisi_Aktif()
        {
            Exception? hata = null;
            bool keyPreview = false;

            var isParcacigi = new Thread(() =>
            {
                try
                {
                    using var form = new Form1();
                    keyPreview = form.KeyPreview;
                }
                catch (Exception ex)
                {
                    hata = ex;
                }
            })
            {
                IsBackground = true
            };
            isParcacigi.SetApartmentState(ApartmentState.STA);

            isParcacigi.Start();
            isParcacigi.Join(TimeSpan.FromSeconds(60));

            Assert.Null(hata);
            Assert.True(keyPreview);
        }
    }
}
