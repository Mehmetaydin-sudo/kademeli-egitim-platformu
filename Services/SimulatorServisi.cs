using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace KademeliEgitimPlatformu.Services
{
    public enum TaniOnem
    {
        Hata,
        Uyari
    }

    /// <summary>Derleyicinin ürettiği tek bir tanı (hata veya uyarı).</summary>
    public sealed record SimulatorTanisi(
        string Kod,
        string Mesaj,
        string TurkceIpucu,
        int Satir,
        int Sutun,
        TaniOnem Onem);

    public sealed record SimulatorSonucu(bool Basarili, IReadOnlyList<SimulatorTanisi> Tanilar)
    {
        public int HataSayisi => Tanilar.Count(t => t.Onem == TaniOnem.Hata);
        public int UyariSayisi => Tanilar.Count(t => t.Onem == TaniOnem.Uyari);
    }

    /// <summary>
    /// Gömülü kod simülatörü. Regex tahmini yerine gerçek Roslyn derleyicisini
    /// kullanır: sözdizimi ve tip hatalarını satır/sütun bilgisiyle bildirir.
    /// Tamamen çevrimdışıdır; hiçbir ağ çağrısı yapmaz.
    /// </summary>
    public sealed class SimulatorServisi
    {
        public const int MaksKodUzunlugu = 20_000;
        private static readonly TimeSpan AnalizSuresi = TimeSpan.FromSeconds(5);
        private const int SarmalamaSatiri = 2;

        private static readonly Lazy<IReadOnlyList<MetadataReference>> Referanslar =
            new(ReferanslariYukle, LazyThreadSafetyMode.ExecutionAndPublication);

        private static readonly Regex TipBildirimi =
            new(@"\b(class|struct|record|interface|enum)\s+[A-Za-z_]", RegexOptions.Compiled);

        // Kod "using System;" gibi yönergeyle başlıyorsa sınıf kabına alınmamalı.
        private static readonly Regex KullanimBildirimi =
            new(@"(?m)^\s*using\s+[A-Za-z_][\w.]*\s*;", RegexOptions.Compiled);

        public SimulatorSonucu Derle(string? kod)
        {
            if (string.IsNullOrWhiteSpace(kod))
            {
                return TekHata("BOŞ", "Kod boş. Lütfen analiz edilecek C# kodunu girin.",
                    "Editöre bir kod bloğu yazın.", 1, 1);
            }

            if (kod.Length > MaksKodUzunlugu)
            {
                return TekHata("UZUN", $"Kod {MaksKodUzunlugu} karakter sınırını aşıyor.",
                    "Daha kısa bir kod bloğu deneyin.", 1, 1);
            }

            bool sarmalandi = !TipBildirimi.IsMatch(kod) && !KullanimBildirimi.IsMatch(kod);
            string kaynak = sarmalandi
                ? "public class SimulatorKabi\n{\n" + kod + "\n}"
                : kod;

            try
            {
                var agac = CSharpSyntaxTree.ParseText(
                    kaynak,
                    new CSharpParseOptions(LanguageVersion.Latest),
                    path: "Simulator.cs");

                var derleme = CSharpCompilation.Create(
                    "SimulatorKontrol",
                    new[] { agac },
                    Referanslar.Value,
                    new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        optimizationLevel: OptimizationLevel.Debug,
                        allowUnsafe: false));

                using var iptal = new CancellationTokenSource(AnalizSuresi);
                var tanilar = derleme.GetDiagnostics(iptal.Token)
                    .Where(d => d.Severity == DiagnosticSeverity.Error ||
                                d.Severity == DiagnosticSeverity.Warning)
                    .Where(d => d.Id != "CS8019") // gereksiz using uyarısı gürültü yapmasın
                    .OrderByDescending(d => d.Severity)
                    .ThenBy(d => d.Location.GetLineSpan().StartLinePosition.Line)
                    .Take(12)
                    .Select(d => TanityaCevir(d, sarmalandi))
                    .ToList();

                bool basarili = tanilar.All(t => t.Onem != TaniOnem.Hata);
                return new SimulatorSonucu(basarili, tanilar);
            }
            catch (OperationCanceledException)
            {
                return TekHata("ZAMAN AŞIMI", "Kod analizi zaman aşımına uğradı.",
                    "Kodu sadeleştirip yeniden deneyin.", 1, 1);
            }
            catch (Exception ex)
            {
                return TekHata("ANALİZ", "Kod analiz edilemedi: " + ex.Message,
                    "Kodun geçerli bir C# bloğu olduğundan emin olun.", 1, 1);
            }
        }

        /// <summary>Sonucu arayüzde gösterilecek Türkçe metne çevirir.</summary>
        public static string RaporMetni(SimulatorSonucu sonuc)
        {
            ArgumentNullException.ThrowIfNull(sonuc);
            var sb = new StringBuilder();

            if (sonuc.Basarili)
            {
                sb.AppendLine($"✓ DERLEME BAŞARILI ({sonuc.HataSayisi} hata, {sonuc.UyariSayisi} uyarı)");
                sb.AppendLine();
                sb.AppendLine("Kod Roslyn derleyicisi tarafından doğrulandı. Syntax tuzağı kalmadı.");
                if (sonuc.UyariSayisi > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Uyarılar:");
                    foreach (SimulatorTanisi t in sonuc.Tanilar.Where(t => t.Onem == TaniOnem.Uyari))
                        sb.AppendLine($"• Satır {t.Satir}: {t.Kod} — {t.Mesaj}");
                }
                return sb.ToString().TrimEnd();
            }

            sb.AppendLine($"✕ DERLEME BAŞARISIZ ({sonuc.HataSayisi} hata, {sonuc.UyariSayisi} uyarı)");
            sb.AppendLine();
            foreach (SimulatorTanisi t in sonuc.Tanilar)
            {
                string onem = t.Onem == TaniOnem.Hata ? "HATA" : "UYARI";
                sb.AppendLine($"• {onem} — Satır {t.Satir}, Sütun {t.Sutun} — {t.Kod}");
                sb.AppendLine($"  {t.Mesaj}");
                if (!string.IsNullOrWhiteSpace(t.TurkceIpucu))
                    sb.AppendLine($"  → {t.TurkceIpucu}");
            }
            return sb.ToString().TrimEnd();
        }

        private static SimulatorSonucu TekHata(string kod, string mesaj, string ipucu, int satir, int sutun) =>
            new(false, new[] { new SimulatorTanisi(kod, mesaj, ipucu, satir, sutun, TaniOnem.Hata) });

        private static SimulatorTanisi TanityaCevir(Diagnostic tani, bool sarmalandi)
        {
            FileLinePositionSpan konum = tani.Location.GetLineSpan();
            int satir = konum.StartLinePosition.Line + 1 - (sarmalandi ? SarmalamaSatiri : 0);
            int sutun = konum.StartLinePosition.Character + 1;

            return new SimulatorTanisi(
                tani.Id,
                tani.GetMessage(),
                IpucuBul(tani),
                Math.Max(1, satir),
                Math.Max(1, sutun),
                tani.Severity == DiagnosticSeverity.Error ? TaniOnem.Hata : TaniOnem.Uyari);
        }

        private static string IpucuBul(Diagnostic tani)
        {
            // Not: Roslyn tanı mesajları sistem diline göre yerelleşir.
            // Bu yüzden ipucu eşlemesi mesaj metnine değil, kararlı tanı koduna göre yapılır.
            return tani.Id switch
            {
                "CS1002" => "Satır sonunda noktalı virgül (;) eksik.",
                "CS1026" => "Kapanmayan parantez var: ')' ekleyin.",
                "CS1022" => "Fazladan kapanan parantez/süslü parantez var.",
                "CS1513" => "Kapanmayan süslü parantez var: '}' ekleyin.",
                "CS1519" => "Geçersiz belirteç; genellikle fazladan noktalı virgül veya eksik parantez.",
                "CS1525" => "Geçersiz ifade; 'if (...)' sonrasındaki fazladan ';' bu hataya yol açar.",
                "CS1003" => "Sözdizimi hatası: beklenmeyen karakter veya eksik ayraç.",
                "CS1031" => "Tür bekleniyordu; bildirimi kontrol edin.",
                "CS0029" => "Tür dönüştürme hatası: değer hedef türe atanamıyor. Koşul içinde atama (=) yerine karşılaştırma (==) kullanmayı deneyin.",
                "CS0019" => "Operatör bu türlerle kullanılamıyor; '=' yerine '==' olabilir.",
                "CS0103" => "Tanımsız isim: değişken veya metot adını kontrol edin.",
                "CS0117" => "Bu türde böyle bir üye yok; adı kontrol edin.",
                "CS0161" => "Metot her koşulda bir değer döndürmeli.",
                "CS0165" => "Değişken kullanılmadan önce atanmalı.",
                "CS7036" => "Metot çağrısında eksik argüman var.",
                "CS1501" => "Bu argüman sayısıyla eşleşen metot yok.",
                "CS0127" => "Değer döndüren metot 'void' olarak tanımlanmış.",
                "CS0201" => "Yalnızca atama, çağrı veya artırma ifadeleri geçerli.",
                "CS1955" => "Çağrılamayan üye; parantezleri kontrol edin.",
                "CS0021" => "Bu türe köşeli parantezle erişilemez.",
                _ => string.Empty
            };
        }

        private static IReadOnlyList<MetadataReference> ReferanslariYukle()
        {
            var liste = new List<MetadataReference>();

            if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is not string tpa)
                return liste;

            foreach (string yol in tpa.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!File.Exists(yol))
                    continue;

                try
                {
                    liste.Add(MetadataReference.CreateFromFile(yol));
                }
                catch (IOException)
                {
                    // Kilitli/bulunamayan derleme atlanır; kalan referanslarla devam edilir.
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            return liste;
        }
    }
}
