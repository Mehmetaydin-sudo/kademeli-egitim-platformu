using System;
using KademeliEgitimPlatformu.Services;

if (args.Length < 1 || args.Length > 2)
{
    Console.WriteLine("Kademeli Eğitim Platformu — ClosedXML (.xlsx / .csv) Soru Paketleyici");
    Console.WriteLine("Kullanım:");
    Console.WriteLine("  1) Kaynak dosyayı doğrulayıp USB soru paketi (.xlsx / .csv) üretmek:");
    Console.WriteLine("     dotnet run --project tools/SoruPaketleyici -- <kaynak.xlsx|kaynak.csv> <cikti.xlsx|cikti.csv>");
    Console.WriteLine();
    Console.WriteLine("  2) PostgreSQL soru havuzunu .xlsx veya .csv olarak dışa aktarmak:");
    Console.WriteLine("     dotnet run --project tools/SoruPaketleyici -- disari-aktar <cikti.xlsx|cikti.csv>");
    Console.WriteLine("     dotnet run --project tools/SoruPaketleyici -- <cikti.xlsx|cikti.csv>");
    Console.WriteLine();
    Console.WriteLine("  3) .xlsx veya .csv paketini PostgreSQL veritabanına içe aktarmak:");
    Console.WriteLine("     dotnet run --project tools/SoruPaketleyici -- ice-aktar <paket.xlsx|paket.csv>");
    return 1;
}

try
{
    if (args.Length == 1)
    {
        string hedefYol = VeriYolu.GuvenliCiktiYolu(args[0]);
        var servis = new SoruServisi();
        int adet = servis.DisariAktar(hedefYol);
        Console.WriteLine($"PostgreSQL soru havuzu dışa aktarıldı: {hedefYol} ({adet} soru)");
        return 0;
    }

    string ilkArg = args[0].ToLowerInvariant();
    if (ilkArg == "disari-aktar")
    {
        string hedefYol = VeriYolu.GuvenliCiktiYolu(args[1]);
        var servis = new SoruServisi();
        int adet = servis.DisariAktar(hedefYol);
        Console.WriteLine($"PostgreSQL soru havuzu dışa aktarıldı: {hedefYol} ({adet} soru)");
        return 0;
    }

    if (ilkArg == "ice-aktar")
    {
        var servis = new SoruServisi();
        int adet = servis.DisaridanPaketYukle(args[1]);
        Console.WriteLine($"PostgreSQL soru havuzuna içe aktarıldı: {args[1]} ({adet} yeni soru)");
        return 0;
    }

    string guvenliPaketYolu = VeriYolu.GuvenliCiktiYolu(args[1]);
    int paketAdet = SoruServisi.PaketOlustur(args[0], guvenliPaketYolu);
    Console.WriteLine($"USB soru paketi oluşturuldu: {guvenliPaketYolu}");
    Console.WriteLine($"Doğrulanan ve paketlenen soru sayısı: {paketAdet}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("Paket işlemi başarısız: " + VeritabaniBaglantisi.KullaniciDostuHataMesaji(ex));
    return 1;
}
