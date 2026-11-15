using KademeliEgitimPlatformu.Services;

if (args.Length != 2)
{
    Console.WriteLine("Kullanım:");
    Console.WriteLine("  dotnet run --project tools/SoruPaketleyici -- <girdi.json> <cikti.dat>");
    Console.WriteLine();
    Console.WriteLine("Örnek:");
    Console.WriteLine("  dotnet run --project tools/SoruPaketleyici -- Data/Sorular.json Data/Sorular.dat");
    return 1;
}

try
{
    int adet = SoruServisi.PaketOlustur(args[0], args[1]);
    Console.WriteLine($"Şifreli paket oluşturuldu: {args[1]}");
    Console.WriteLine($"Doğrulanan ve paketlenen soru sayısı: {adet}");
    Console.WriteLine("Bu dosya AES-256-GCM ile şifrelenmiştir; düz metin soru içermez.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("Paket oluşturulamadı: " + ex.Message);
    return 1;
}
