using System;
using System.Collections.Generic;
using System.Linq;
using KademeliEgitimPlatformu.Models;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>Bir disiplinin o sınavdaki sonucu.</summary>
    public sealed record DisiplinSonucu(DisiplinTuru Disiplin, int Dogru, int Toplam)
    {
        public int Yuzde => PuanlamaServisi.Yuzde(Dogru, Toplam);
    }

    /// <summary>
    /// Sınav puanlamasını arayüzden bağımsız, test edilebilir biçimde hesaplar.
    /// Karma sınavda her disiplin kendi sorularından ayrı değerlendirilir.
    /// </summary>
    public static class PuanlamaServisi
    {
        public static int Yuzde(int dogru, int toplam) =>
            toplam <= 0 ? 0 : (int)Math.Round(dogru * 100.0 / toplam, MidpointRounding.AwayFromZero);

        public static IReadOnlyList<DisiplinSonucu> DisiplinBazindaHesapla(
            IReadOnlyList<Soru> sorular,
            IReadOnlyList<bool> cevaplar)
        {
            ArgumentNullException.ThrowIfNull(sorular);
            ArgumentNullException.ThrowIfNull(cevaplar);

            return sorular
                .Select((soru, index) => new
                {
                    soru.Disiplin,
                    Dogru = index < cevaplar.Count && cevaplar[index]
                })
                .GroupBy(x => x.Disiplin)
                .Select(g => new DisiplinSonucu(g.Key, g.Count(x => x.Dogru), g.Count()))
                .OrderBy(s => s.Disiplin)
                .ToList();
        }

        /// <summary>
        /// Sınav sonucunu mevcut ilerlemeyle birleştirir. Başarı yüzdeleri asla
        /// düşürülmez (en iyi sonuç korunur), kazanılan jeton eklenir.
        /// </summary>
        public static IlerlemeDurumu Birlestir(
            IlerlemeDurumu mevcut,
            IEnumerable<DisiplinSonucu> sonuclar,
            int kazanilanJeton = 0)
        {
            ArgumentNullException.ThrowIfNull(mevcut);
            ArgumentNullException.ThrowIfNull(sonuclar);

            int gorsel = mevcut.GorselBasari;
            int veriTabani = mevcut.VeriTabaniBasari;
            int siber = mevcut.SiberBasari;

            foreach (DisiplinSonucu sonuc in sonuclar)
            {
                int yuzde = Math.Clamp(sonuc.Yuzde, 0, 100);
                switch (sonuc.Disiplin)
                {
                    case DisiplinTuru.GorselProgramlama:
                        gorsel = Math.Max(gorsel, yuzde);
                        break;
                    case DisiplinTuru.VeriTabanlari:
                        veriTabani = Math.Max(veriTabani, yuzde);
                        break;
                    case DisiplinTuru.SiberGuvenlik:
                        siber = Math.Max(siber, yuzde);
                        break;
                }
            }

            return mevcut with
            {
                GorselBasari = gorsel,
                VeriTabaniBasari = veriTabani,
                SiberBasari = siber,
                IpucuJetonu = mevcut.IpucuJetonu + Math.Max(0, kazanilanJeton)
            };
        }
    }
}
