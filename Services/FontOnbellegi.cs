using System.Collections.Concurrent;
using System.Drawing;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>
    /// Arayüzde kullanılan Font nesnelerini önbelleğe alır.
    /// Her ekran çiziminde yeni Font oluşturulup GDI+ tutamacı sızdırılmasını engeller.
    /// Önbellek süreç ömrü boyunca yaşar; kullanılan font çeşidi sınırlıdır.
    /// </summary>
    public static class FontOnbellegi
    {
        private static readonly ConcurrentDictionary<(string Aile, float Boyut, FontStyle Stil), Font> Onbellek = new();

        public static Font Al(string aile, float boyut, FontStyle stil = FontStyle.Regular) =>
            Onbellek.GetOrAdd((aile, boyut, stil), k => new Font(k.Aile, k.Boyut, k.Stil));

        /// <summary>Önbellekte tutulan farklı font sayısı (tanılama/test amaçlı).</summary>
        public static int KayitliFontSayisi => Onbellek.Count;
    }
}
