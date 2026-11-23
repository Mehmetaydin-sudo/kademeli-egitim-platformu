using System;
using System.Collections.Generic;
using System.Linq;
using KademeliEgitimPlatformu.Models;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>
    /// Bir soru nesnesinin doğrulama sonucunu ve varsa Türkçe hata gerekçelerini taşır.
    /// </summary>
    public sealed class SoruDogrulamaSonucu
    {
        public IReadOnlyList<string> Hatalar { get; }

        public bool GecerliMi => Hatalar.Count == 0;

        public string HataOzeti => Hatalar.Count == 0 ? string.Empty : string.Join(" | ", Hatalar);

        public SoruDogrulamaSonucu(IReadOnlyList<string> hatalar)
        {
            Hatalar = hatalar;
        }
    }

    /// <summary>
    /// Soru nesnelerinin alan, şık, doğru cevap ve açıklama bütünlüğünü doğrular.
    /// Hem veritabanı aktarımında hem de belge önizleme ekranında tek doğrulama kaynağıdır.
    /// </summary>
    public static class SoruDogrulayici
    {
        private static readonly string[] SikHarfleri = { "A", "B", "C", "D" };

        /// <summary>Sorunun veritabanına kaydedilmeye uygun olup olmadığını döndürür.</summary>
        public static bool GecerliMi(Soru? soru) => Dogrula(soru).GecerliMi;

        /// <summary>Soruyu tüm kurallara göre denetler ve eksik/hatalı alanları listeler.</summary>
        public static SoruDogrulamaSonucu Dogrula(Soru? soru)
        {
            var hatalar = new List<string>();

            if (soru == null)
            {
                hatalar.Add("Soru nesnesi boş.");
                return new SoruDogrulamaSonucu(hatalar);
            }

            if (string.IsNullOrWhiteSpace(soru.Baslik))
                hatalar.Add("Soru metni (başlık) boş olamaz.");

            if (soru.Secenekler == null || soru.Secenekler.Count == 0)
            {
                hatalar.Add("Şıklar (A, B, C, D) bulunamadı.");
            }
            else if (soru.Secenekler.Count != 4)
            {
                hatalar.Add($"Tam 4 şık (A–D) gereklidir; {soru.Secenekler.Count} şık bulundu.");
            }
            else
            {
                var eksikHarfler = new List<string>();
                for (int i = 0; i < 4; i++)
                {
                    if (string.IsNullOrWhiteSpace(soru.Secenekler[i]))
                        eksikHarfler.Add(SikHarfleri[i]);
                }

                if (eksikHarfler.Count == 1)
                    hatalar.Add($"{eksikHarfler[0]} şıkkı eksik veya boş.");
                else if (eksikHarfler.Count > 1)
                    hatalar.Add($"{string.Join(", ", eksikHarfler)} şıkları eksik veya boş.");
            }

            if (soru.DogruSecenekIndex < 0 || soru.DogruSecenekIndex > 3)
                hatalar.Add("Doğru cevap (A–D) belirtilmemiş veya geçersiz.");

            if (!Enum.IsDefined(typeof(DisiplinTuru), soru.Disiplin))
                hatalar.Add("Geçersiz modül (disiplin) değeri.");

            if (!Enum.IsDefined(typeof(ZorlukSeviyesi), soru.Zorluk))
                hatalar.Add("Geçersiz zorluk seviyesi değeri.");

            if (soru.YanlisCevapAciklamalari != null && soru.YanlisCevapAciklamalari.Keys.Any(k => k < 0 || k > 3))
                hatalar.Add("Yanlış cevap açıklamaları geçersiz şık indeksi içeriyor.");

            if (soru.Etiket != null && soru.Etiket.Length > 50)
                hatalar.Add("Soru etiketi en fazla 50 karakter olabilir.");

            return new SoruDogrulamaSonucu(hatalar);
        }
    }
}
