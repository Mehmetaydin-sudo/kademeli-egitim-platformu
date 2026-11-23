# 🎓 Kademeli Eğitim Platformu

Öğretmenlerin ve öğrencilerin **herhangi bir ders, branş veya uzmanlık alanı** için kendi soru paketlerini kolayca yükleyebildiği; **Kolay, Orta ve Zor** kademeli zorluk seviyeleri ile başarı barajı sistemini birleştiren, **%100 çevrimdışı** çalışan masaüstü eğitim ve sınav platformu.

---

## 🎯 Projenin Amacı

Kademeli Eğitim Platformu; öğrencilerin konuları ezberlemek yerine sıfırdan uzmanlığa doğru adım adım kavramasını hedefleyen genel amaçlı bir öğrenme ortamıdır. Matematikten yabancı dile, fen bilimlerinden hukuka veya mesleki derslere kadar **her alana uyarlanabilir**:

- Öğretmenler ve eğitmenler mevcut ders notlarını veya testlerini ekstra teknik formatla uğraşmadan (`.docx`, `.pdf`, `.xlsx`, `.csv`) platforma aktarabilir.
- Öğrenciler internet bağlantısına ihtiyaç duymadan kendi bilgisayarlarında kademeli sınavlara girer, yanlış yaptıkları sorularda hatanın sebebini anında öğrenir ve performans karneleriyle eksik konularını takip eder.
- İlk kurulumda veritabanı boşsa platformun akışını hemen deneyimleyebilmek için örnek bir başlangıç soru havuzu otomatik yüklenir.

---

## 🚀 Öne Çıkan Özellikler

* **Her Alan İçin Kendi Soru Paketini Yükleme (`.docx`, `.pdf`, `.xlsx`, `.csv`):**
  * **Word (`.docx`) ve PDF (`.pdf`):** `DocumentFormat.OpenXml` ve `UglyToad.PdfPig` ile tamamen çevrimdışı olarak klasik test belgelerini, toplu cevap anahtarlarını (`1-B, 2-A` / `1. B`) ve Word tablolarını (`Soru | A | B | C | D | Cevap | Zorluk | Açıklama`) otomatik ayrıştırır.
  * **Önizleme ve Doğrulama Ekranı:** `.docx` veya `.pdf` seçildiğinde sorular veritabanına yazılmadan önce önizleme ekranında listelenir; `SoruDogrulayici` ile eksik şıklı veya cevapsız sorular vurgulanır ve yalnızca kullanıcının onayladığı geçerli sorular aktarılır.
  * **Excel (`.xlsx`) ve `.csv`:** `ClosedXML` ile toplu soru içe/dışa aktarım ve USB soru paketi üretimi (`tools/SoruPaketleyici`) desteklenir. Tüm içe aktarımlar mevcut ilerlemeyi ezmeden `NpgsqlTransaction` içinde çalışır.
* **Pedagojik "Neden Yanlış?" Açıklaması:** Yanlış bir şık seçildiğinde yalnızca doğru cevabı göstermekle kalmaz; hatanın kaynağını sade ve öğretici bir dille anında açıklar.
* **Kademeli Zorluk Geçişi ve Seviye Barajı Kuralları:**
  * **Kolay Seviye:** Her zaman açıktır; temel kavramların pekiştirilmesi için başlangıç adımıdır.
  * **Kolay → Orta Geçişi (%70 Başarı Barajı):** Katı engel yerine **Seviye Geçiş Ara Sınavı** uygulanır; baraj aşıldığında Orta seviye kilidi kalıcı olarak açılır.
  * **Orta → Zor Geçişi (%75 Başarı Barajı):** Orta → Zor Seviye Geçiş Ara Sınavı başarıyla tamamlandığında ileri seviye (Zor mod) kilidi kalıcı olarak açılır.
* **İstatistik, Zayıf Konu Tespiti ve Performans Karnesi Dışa Aktarma:**
  * Modül/konu bazlı başarı yüzdeleri, sınav geçmişi ve yanlış yapılan sorular kalıcı olarak kaydedilir.
  * Yanlış cevaplar (soru metni, verilen cevap, doğru cevap ve pedagojik açıklama) **Hata ve Performans Karnesi** olarak izlenir ve `.txt` raporu halinde dışa aktarılabilir.
  * Her sınavda sorular ve şıklar karıştırılır (`SoruGorunumu`), klavye kısayolları (`1`–`4` ve `Enter`) ve ipucu jetonu sistemi öğrenme deneyimini destekler.
* **%100 Çevrimdışı Yerel Masaüstü Mimarisi:**
  * Harici sunucu veya internet bağlantısı gerektirmez; Windows Forms (.NET 8) üzerinde yerel **PostgreSQL 18 (`Npgsql`)** veritabanıyla çalışır.
  * Parola ve hassas özetleme işlemlerinde `BCrypt.Net-Next` (tuzlu özetleme), tüm SQL sorgularında ise SQL Injection'a karşı parametreli sorgular kullanılır.

---

## 🛠️ Kurulum ve Geliştirici Başlangıç Adımları

### Gereksinimler
* **İşletim Sistemi:** Windows 10 veya Windows 11
* **SDK:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* **Veritabanı:** Yerel **PostgreSQL 18** (`localhost:5432`)

### 1. Projeyi Klonlama
```bash
git clone https://github.com/Mehmetaydin-sudo/kademeli-egitim-platformu.git
cd kademeli-egitim-platformu
```

### 2. Veritabanı ve Şema Hazırlığı
Yerel PostgreSQL 18 üzerinde `kademeli_egitim` veritabanını oluşturun ve `database/schema.sql` dosyasını çalıştırın (uygulama açılışında eksik tabloları da otomatik doğrular):

```bash
psql -U postgres -h localhost -p 5432 -c "CREATE DATABASE kademeli_egitim WITH ENCODING 'UTF8';"
psql -U postgres -h localhost -p 5432 -d kademeli_egitim -f database/schema.sql
```

### 3. Bağlantı Yapılandırması (`appsettings.example.json` → `appsettings.json`)
Proje kök dizinindeki `appsettings.example.json` dosyasını `appsettings.json` adıyla kopyalayın ve yerel PostgreSQL parolanızı girin (`appsettings.json` güvenlik gereği `.gitignore` içindedir):

```powershell
Copy-Item appsettings.example.json appsettings.json
```

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=kademeli_egitim;Username=postgres;Password=BURAYA_SIFRE_GELECEK"
  }
}
```

### 4. Derleme, Çalıştırma ve Test
```bash
# Çözümü derle
dotnet build KademeliEgitimPlatformu.slnx

# Uygulamayı başlat
dotnet run --project KademeliEgitimPlatformu.csproj

# Tüm xUnit birim ve entegrasyon testlerini çalıştır
dotnet test tests/KademeliEgitimPlatformu.Tests/KademeliEgitimPlatformu.Tests.csproj
```

---

## 📄 Desteklenen Soru Yükleme Formatları

### Word (`.docx`) ve PDF (`.pdf`)
* **Soru Başlangıcı:** `1.`, `1)`, `Soru 1:` vb.
* **Şıklar:** `A)`, `A.`, `a)` … `D)` / `D.` (alt alta veya aynı satırda).
* **Doğru Cevap:** `Cevap: B`, `Doğru Cevap: B`, `Yanıt: B` satırları veya belge sonunda toplu cevap anahtarı (`Cevap Anahtarı: 1-B, 2-A` ya da `1. B` / `2. A`).
* **Opsiyonel Alanlar:** `Zorluk:` (`Kolay`/`Orta`/`Zor` — varsayılan `Orta`), `Modül:` (yoksa önizleme ekranında seçilen modül), `Açıklama:` / `Neden Yanlış:`, `Kod:`, `Etiket:`.
* **Word Tablosu:** `Soru | A | B | C | D | Cevap | Zorluk | Açıklama` sütunları otomatik algılanır.

### Excel (`.xlsx`) ve `.csv`
Sütun sırası: `Disiplin`, `Zorluk`, `Etiket`, `Baslik`, `KodBlogu`, `SecenekA`, `SecenekB`, `SecenekC`, `SecenekD`, `DogruSecenekIndex`, `AciklamaA`, `AciklamaB`, `AciklamaC`, `AciklamaD`.

---

## 👥 Ekip ve Katkı Rehberi

### Proje Ekibi
* **Mehmet Aydın** ([@Mehmetaydin-sudo](https://github.com/Mehmetaydin-sudo)) — Proje Lideri, Yazılım Mimarisi & Veritabanı
* **Yağız Domaç** — Arayüz (UI/UX) Tasarımı, Oyunlaştırma & Testler
* **Ünal Küçük** — İçerik Mimarisi, Soru Havuzu & Pedagojik Açıklamalar
* **İsmail Can Şal** — Modül Geliştirme, Senaryolar & Kalite Kontrol

### Branch ve Pull Request İş Akışı
`main` dalı korumalıdır; doğrudan `main` dalına push veya merge yapılmaz. Tüm ekip üyeleri aşağıdaki akışı izler:

1. **Güncel daldan yeni görev dalı açın:**
   ```bash
   git checkout main
   git pull origin main
   git checkout -b gorev/kisa-gorev-adi
   ```
   *(Örnek: `gorev/pdf-onizleme`, `gorev/karne-filtreleme`, `gorev/yeni-modul-ekleme`)*
2. **Kurallara uygun geliştirme yapın:**
   * `*.Designer.cs` dosyalarını açık onay olmadan düzenlemeyin; arayüz değişikliklerini ilgili formun `.cs` code-behind dosyasında yapın.
   * Veritabanı işlemlerini `Services/` katmanı üzerinden, parametreli Npgsql sorguları ve `using` bloklarıyla yürütün.
   * Proje kurallarının tamamı için [GEMINI.md](GEMINI.md) dosyasına bakın.
3. **Derleme ve test doğrulaması yapın:**
   ```bash
   dotnet build KademeliEgitimPlatformu.slnx
   dotnet test tests/KademeliEgitimPlatformu.Tests/KademeliEgitimPlatformu.Tests.csproj
   ```
4. **Dalınızı pushlayın ve Pull Request açın:**
   ```bash
   git push -u origin gorev/kisa-gorev-adi
   ```
   * GitHub üzerinden `gorev/...` dalından `main` (veya aktif geliştirme dalına) Pull Request açın.
   * CI (`Derle ve Test Et`) kontrollerinin yeşil geçmesini bekleyin ve `CODEOWNERS` kuralı gereği proje lideri **@Mehmetaydin-sudo** onayından sonra merge işlemini tamamlayın.

---

## 🗂️ Proje Yapısı

```text
KademeliEgitimPlatformu/
├── database/
│   └── schema.sql                       # PostgreSQL 18 tablo ve indeks şeması
├── Models/                              # Soru, Kullanici, SinavTuru, SoruGorunumu, YanlisKaydi
├── Services/
│   ├── VeritabaniBaglantisi.cs          # appsettings.json + Npgsql merkezi bağlantı yönetimi
│   ├── SoruServisi.cs                   # PostgreSQL soru havuzu + ClosedXML (.xlsx/.csv)
│   ├── BelgeSoruDonusturucu.cs          # Çevrimdışı Word (.docx) ve PDF (.pdf) ayrıştırıcı
│   ├── SoruDogrulayici.cs               # Merkezi soru alan/şık/cevap doğrulama kuralları
│   ├── KademeServisi.cs                 # Kademeli seviye geçiş ve başarı barajı kuralları
│   ├── IlerlemeServisi.cs               # İlerleme ve seviye geçiş ara sınav kayıtları
│   ├── KarneServisi.cs                  # Yanlış cevap takibi ve performans karnesi raporu
│   ├── KullaniciServisi.cs              # BCrypt.Net-Next tuzlu parola yönetimi
│   ├── PuanlamaServisi.cs               # Başarı hesaplama servisi
│   └── SimulatorServisi.cs              # Uygulamalı kod analizi bileşeni
├── tools/SoruPaketleyici/               # CLI tabanlı .xlsx / .csv soru paketleyici
├── tests/KademeliEgitimPlatformu.Tests/ # xUnit birim ve entegrasyon testleri
├── BelgeOnizlemeFormu.cs                # .docx / .pdf önizleme ve doğrulama penceresi
├── Form1.cs / Form1.AnaMenu.cs / Form1.Sinav.cs / Form1.Simulator.cs
├── appsettings.example.json             # Örnek veritabanı yapılandırması
└── KademeliEgitimPlatformu.csproj
```

---

## 📌 Sürüm Özeti (Changelog)

* **v0.8.3 (Güncel):** Öğrenci profili, 8 kategorili (Bronz–Elmas) rozet vitrini ve takılabilir ünvanlar, XP/seviye çubuğu, günlük çalışma serisi, haftalık çalışma performansı grafiği, günlük görevler, 50 hazır tema + özel renk üretici ve PNG profil kartı dışa aktarma (`Services/KisisellestirmeServisi.cs`, `Form1.Kisisellestirme.cs`); tercihler PostgreSQL `meta` tablosunda saklanır, GDI+ font/kontrol sızıntıları giderildi ve 80/80 xUnit testi eklendi.
* **v0.8.2:** Genel sağlık kontrolü ve kararlılık iyileştirmeleri — `.docx`/`.pdf`/`.xlsx`/`.csv` içe aktarma ve önizleme modül değişiminde asenkron (`Task.Run`) işleme ile UI donma koruması, Türkçe Windows Excel ANSI (`Windows-1254`) `.csv` otomatik kodlama tespiti ve açık dosya (`FileShare.ReadWrite`) desteği, taranmış/boş PDF ile veri satırsız Word tablosu uyarıları, karne dışa aktarma hata yönetimi ve 75/75 xUnit regresyon testi.
* **v0.8.1:** Genel amaçlı **Kademeli Eğitim Platformu** konumlandırması; çevrimdışı Word (`.docx`) ve PDF (`.pdf`) soru dönüştürücü (`BelgeSoruDonusturucu`), merkezi `SoruDogrulayici`, kayıt öncesi önizleme ekranı (`BelgeOnizlemeFormu`), otomatik PostgreSQL şema kontrolü ve güvenli dosya yolu yönetimi.
* **v0.8.0:** Yerel PostgreSQL 18 (`Npgsql`), `BCrypt.Net-Next` tuzlu parola güvenliği, `ClosedXML` (`.xlsx`/`.csv`) veri aktarımı ve Seviye Geçiş Ara Sınavı sistemi.
* **v0.1.0 – v0.7.0:** WinForms (.NET 8) çekirdek arayüzü, kademeli zorluk kuralları, "Neden Yanlış?" pedagojik açıklamaları, hata karnesi ve xUnit test altyapısı.

---

## 📄 Lisans

Bu proje [MIT](LICENSE) lisansı ile lisanslanmıştır.
