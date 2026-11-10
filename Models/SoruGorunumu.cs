using System;
using System.Collections.Generic;
using System.Linq;

namespace KademeliEgitimPlatformu.Models
{
    /// <summary>
    /// Bir sorunun sınav sırasındaki görünümü. Şıklar karıştırıldığında
    /// doğru cevap indeksi ve yanlış cevap açıklamaları da birlikte taşınır.
    /// Böylece doğru cevap her zaman doğru şıkta kalır.
    /// </summary>
    public sealed class SoruGorunumu
    {
        private SoruGorunumu(
            IReadOnlyList<string> secenekler,
            int dogruSecenekIndex,
            IReadOnlyDictionary<int, string> aciklamalar)
        {
            Secenekler = secenekler;
            DogruSecenekIndex = dogruSecenekIndex;
            YanlisCevapAciklamalari = aciklamalar;
        }

        public IReadOnlyList<string> Secenekler { get; }

        public int DogruSecenekIndex { get; }

        public IReadOnlyDictionary<int, string> YanlisCevapAciklamalari { get; }

        public string DogruCevap => Secenekler[DogruSecenekIndex];

        public string? Aciklama(int secilenIndex) =>
            YanlisCevapAciklamalari.TryGetValue(secilenIndex, out string? aciklama) ? aciklama : null;

        /// <summary>
        /// Sorunun gösterim kopyasını üretir. <paramref name="karistir"/> true ise
        /// şıklar rastgele sıralanır; doğru indeks ve açıklamalar yeni sıraya taşınır.
        /// </summary>
        public static SoruGorunumu Olustur(Soru soru, bool karistir, Random? rastgele = null)
        {
            ArgumentNullException.ThrowIfNull(soru);
            rastgele ??= Random.Shared;

            int adet = soru.Secenekler.Count;
            var sira = Enumerable.Range(0, adet).ToArray();

            if (karistir)
            {
                for (int i = adet - 1; i > 0; i--)
                {
                    int j = rastgele.Next(i + 1);
                    (sira[i], sira[j]) = (sira[j], sira[i]);
                }
            }

            string[] secenekler = sira.Select(eski => soru.Secenekler[eski]).ToArray();
            int yeniDogru = Array.IndexOf(sira, soru.DogruSecenekIndex);

            var kaynakAciklamalar = soru.YanlisCevapAciklamalari
                                    ?? new Dictionary<int, string>();
            var aciklamalar = new Dictionary<int, string>();

            for (int yeni = 0; yeni < adet; yeni++)
            {
                int eski = sira[yeni];
                if (eski == soru.DogruSecenekIndex)
                    continue;

                if (kaynakAciklamalar.TryGetValue(eski, out string? aciklama) &&
                    !string.IsNullOrWhiteSpace(aciklama))
                {
                    aciklamalar[yeni] = aciklama;
                }
            }

            return new SoruGorunumu(secenekler, yeniDogru, aciklamalar);
        }
    }
}
