# 🛡️ Kademeli Bilişim ve Siber Güvenlik Eğitim Platformu

Ortaöğretim ve ön lisans öğrencileri için **PostgreSQL 18 (`Npgsql`)**, **`BCrypt.Net-Next`**, **Word/PDF (`.docx` / `.pdf`)**, **`ClosedXML` (`.xlsx` / `.csv`)** ve **gömülü Roslyn derleyicisi** mimarili masaüstü öğrenme ve sözdizimi (syntax) simülasyon platformu.

---

## 👥 Proje Geliştirme Ekibi
* **Mehmet Aydın** — Yazılım Mimarisi, PostgreSQL Veri Tabanı Motoru ve Gömülü Kod Simülatörü
* **Ünal Küçük** — Görsel Programlama & Veri Tabanları Soru Havuzu ve Pedagojik Açıklamalar
* **İsmail Can Şal** — Siber Güvenlik Modülü ve Gizli Syntax Tuzak Senaryoları
* **Yağız Domaç** — Arayüz (UI/UX) Tasarımı, Oyunlaştırma Rozetleri ve Birim Testler

---

## 🚀 Temel Özellikler

* **Yerel PostgreSQL 18 (`Npgsql`) Mimarisi:** Soru havuzu (`sorular`), kullanıcı hesapları (`kullanicilar`), ilerleme metrikleri (`ilerleme`, `kullanici_ilerleme`), seviye geçiş ara sınav geçmişi (`seviye_gecis_sinavlari`) ve hata karnesi (`yanlis_cevaplar`) yerel PostgreSQL 18 (`localhost:5432`, veritabanı: `kademeli_egitim`) üzerinde tutulur. Bağlantı bilgileri kod içine gömülmez; `appsettings.json` dosyasından `Microsoft.Extensions.Configuration.Json` ile okunur.
* **`BCrypt.Net-Next` Tuzlu Parola Güvenliği:** Kullanıcı kayıt, giriş ve şifre güncelleme işlemlerinde SHA-256 yerine `BCrypt.Net-Next` (`BCrypt.HashPassword` / `BCrypt.Verify`) ile rastgele tuzlu (salted) parola özetleme uygulanır.
* **Seviye Geçiş Ara Sınavı Sistemi:** Katı engel yerine kademeler arasında (`Kolay -> Orta` ve `Orta -> Zor`) **"Seviye Geçiş Ara Sınavı"** uygulanır:
  * **Kolay Mod** — her zaman açıktır.
  * **Kolay -> Orta Seviye Geçiş Ara Sınavı ve Modül Ara Sınavları** — geçme barajı **%70**'tir; geçildiğinde **Orta Mod** kilidi kalıcı olarak açılır ve sonuç `seviye_gecis_sinavlari` tablosuna kaydedilir.
  * **Orta -> Zor Seviye Geçiş Ara Sınavı** — geçme barajı **%75**'tir; geçildiğinde **Zor Mod (Gömülü Kod Simülatörü)** kilidi kalıcı olarak açılır.
* **Word (`.docx`) ve PDF (`.pdf`) Belgelerinden Çevrimdışı Soru İçe Aktarma:** `DocumentFormat.OpenXml` ve `UglyToad.PdfPig` ile tamamen çevrimdışı olarak Word (paragraf ve tablo) ve PDF belgelerinden soru ayrıştırılır. Kayıt öncesinde açılan **Önizleme / Kontrol Penceresi** (`BelgeOnizlemeFormu`), `SoruDogrulayici` üzerinden hatalı veya eksik şıklı soruları kırmızı vurgular, varsayılan modül seçimine ve onaylanan geçerli soruların `NpgsqlTransaction` ile veritabanına aktarılmasına izin verir.
* **`ClosedXML` ile Excel (`.xlsx`) ve `.csv` Veri Aktarımı:** Soru içe ve dışa aktarım işlemlerinde JSON formatı kaldırılmış olup `ClosedXML` kütüphanesiyle `.xlsx` ve `.csv` formatları kullanılır. İçe aktarım işlemleri mevcut kullanıcı ilerlemesini ezmeden `NpgsqlTransaction` güvencesiyle yürütülür.
* **Pedagojik "Neden Yanlış" Paneli:** Yanlış şık seçildiğinde hatanın kaynağını sade bir dille anında açıklar.
* **Gömülü Kod Simülatörü (Roslyn):** Editördeki C# kodunu gerçek Roslyn derleyicisiyle denetler. Hatalar satır/sütun numarası ve Türkçe pedagojik ipuçlarıyla listelenir.
* **Soru Bazlı Hata Karnesi:** Yanlış cevaplar (soru, seçilen cevap, doğru cevap ve açıklama) PostgreSQL `yanlis_cevaplar` tablosunda saklanır (en yeni 300 kayıt) ve `.txt` raporu olarak indirilebilir.
* **Disiplin Bazlı Puanlama & Şık Karıştırma:** Her sınavda soru sırası ve şıklar rastgelelenir; doğru cevap ve yanlış açıklamaları yeni sıraya birlikte taşınır.
* **Klavye Kısayolları:** Sınav sırasında `1`–`4` tuşlarıyla şık seçilir, `Enter` ile sonraki soruya geçilir.

---

## 🖥️ Kurulum ve Çalıştırma

### Gereksinimler
* Windows 10/11 (64-bit veya 32-bit)
* **.NET 8 SDK**
* **PostgreSQL 18** (`localhost:5432`)

### 1. Veritabanı ve Şema Kurulumu
Yerel PostgreSQL 18 sunucusunda `kademeli_egitim` veritabanını oluşturun ve `database/schema.sql` dosyasını çalıştırın:

```bash
psql -U postgres -h localhost -p 5432 -c "CREATE DATABASE kademeli_egitim WITH ENCODING 'UTF8';"
psql -U postgres -h localhost -p 5432 -d kademeli_egitim -f database/schema.sql
```

### 2. Yapılandırma (`appsettings.json`)
Proje kök dizinindeki `appsettings.example.json` dosyasını `appsettings.json` olarak kopyalayın ve kendi yerel PostgreSQL parolanızı girin (`appsettings.json` güvenlik nedeniyle `.gitignore` içindedir):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=kademeli_egitim;Username=postgres;Password=BURAYA_SIFRE_GELECEK"
  }
}
```

### 3. Kaynaktan Derleme ve Çalıştırma
```bash
git clone https://github.com/Mehmetaydin-sudo/kademeli-egitim-platformu.git
cd kademeli-egitim-platformu
dotnet build
dotnet run
```

İlk açılışta `sorular` tablosu boşsa uygulama `Kolay`, `Orta` ve `Zor` seviyelerini kapsayan 23 soruluk varsayılan eğitim ve ara sınav havuzunu otomatik olarak PostgreSQL veritabanına tohumlar.

---

## 📊 Soru Havuzu ve İçe/Dışa Aktarım Biçimleri (`.docx` / `.pdf` / `.xlsx` / `.csv`)

### Word (`.docx`) ve PDF (`.pdf`) Belge İçe Aktarma Biçimleri

Ana menüdeki **Soru İçe Aktar** penceresi `.docx` ve `.pdf` belgelerini öncelikli olarak destekler ve veritabanına yazmadan önce önizleme ekranında doğrular:

* **Klasik Test Formatı:** `1.`, `1)`, `Soru 1:` başlangıçları; `A)`, `A.`, `a)` … `D)` şıkları (alt alta veya aynı satırda); `Cevap: B`, `Doğru Cevap: B`, `Yanıt: B` satırları.
* **Toplu Cevap Anahtarı:** Belge sonunda `Cevap Anahtarı: 1-B, 2-A` veya `1. B` / `2. A` biçimindeki cevap anahtarları soru numaralarıyla otomatik eşleştirilir.
* **Opsiyonel Alanlar:** `Zorluk:` (`Kolay`/`Orta`/`Zor` — belirtilmezse varsayılan `Orta`), `Modül:` (belirtilmezse önizleme ekranında seçilen modül), `Açıklama:` / `Neden Yanlış:`, `Kod:` ve `Etiket:`.
* **Word (`.docx`) Tablo Formatı:** `Soru | A | B | C | D | Cevap | Zorluk | Açıklama` sütunlarıyla hazırlanmış tablolar otomatik algılanır. Eksik şıklı veya cevapsız sorular sessizce atlanmaz; soru numarası ve sebebiyle uyarı listesinde gösterilir.

### Excel (`.xlsx`) ve `.csv` Paket Biçimi

Soru içe ve dışa aktarım işlemlerinde **Excel (`.xlsx`)** ve **`.csv`** formatları da desteklenir. Tablo sütun sırası aşağıdaki gibidir:

| Sütun | Açıklama |
| :--- | :--- |
| `Disiplin` | `GorselProgramlama`, `VeriTabanlari`, `SiberGuvenlik` (veya `0`, `1`, `2`) |
| `Zorluk` | `Kolay`, `Orta`, `Zor` (veya `0`, `1`, `2`) |
| `Etiket` | Boş (normal sınav), `AraSinav`, `KolayOrtaAraSinav`, `OrtaZorAraSinav` |
| `Baslik` | Soru metni (benzersizdir; aynı başlıklı soru tekrar eklenmez) |
| `KodBlogu` | İsteğe bağlı örnek C# / SQL kod bloğu |
| `SecenekA`..`SecenekD` | 4 adet şık metni (boş olamaz) |
| `DogruSecenekIndex` | Doğru şık indeksi (`0` = A, `1` = B, `2` = C, `3` = D) |
| `AciklamaA`..`AciklamaD` | Yanlış şıklar seçildiğinde gösterilecek pedagojik açıklamalar |

### `SoruPaketleyici` Aracı Kullanımı

PostgreSQL soru havuzunu `.xlsx` veya `.csv` olarak dışa aktarmak, doğrulayarak USB soru paketi üretmek veya içe aktarmak için:

```bash
# PostgreSQL soru havuzunu Excel (.xlsx) veya .csv olarak dışa aktar
dotnet run --project tools/SoruPaketleyici -- disari-aktar Data/OrnekSorular.xlsx

# Kaynak .xlsx / .csv dosyasını doğrulayıp USB soru paketi (.xlsx / .csv) üret
dotnet run --project tools/SoruPaketleyici -- Data/OrnekSorular.xlsx Data/UsbSoruPaketi.xlsx

# .xlsx veya .csv soru paketini PostgreSQL veritabanına içe aktar
dotnet run --project tools/SoruPaketleyici -- ice-aktar Data/UsbSoruPaketi.xlsx
```

---

## 🗂️ Proje Yapısı

```
KademeliEgitimPlatformu/
├── database/
│   └── schema.sql                # PostgreSQL 18 veritabanı tablo ve indeks şeması
├── Models/
│   ├── Kullanici.cs              # Kullanıcı modeli (BCrypt sifre_hash ve rol)
│   ├── Soru.cs                   # Soru, DisiplinTuru, ZorlukSeviyesi, Etiket
│   ├── SinavTuru.cs              # Normal, AraSinav, KolayOrtaGecisSinavi, OrtaZorGecisSinavi
│   ├── SoruGorunumu.cs           # Şık karıştırma + doğru cevap/açıklama eşlemesi
│   └── YanlisKaydi.cs            # Hata karnesi kaydı
├── Services/
│   ├── VeritabaniBaglantisi.cs   # appsettings.json + Npgsql merkezi bağlantı yönetimi
│   ├── KullaniciServisi.cs       # BCrypt.Net-Next ile kullanıcı kayıt/giriş/şifre güncelleme
│   ├── SoruServisi.cs            # PostgreSQL soru havuzu + ClosedXML (.xlsx/.csv) aktarım
│   ├── SoruDogrulayici.cs        # Soru alan, şık ve doğru cevap doğrulama kuralları
│   ├── BelgeSoruDonusturucu.cs   # Çevrimdışı Word (.docx) ve PDF (.pdf) soru ayrıştırıcı
│   ├── SoruKoruyucu.cs           # BCrypt.Net-Next parola/metin özetleme ve doğrulama yardımcısı
│   ├── KademeServisi.cs          # Seviye Geçiş Ara Sınavı / Orta Mod / Zor Mod geçiş kuralları
│   ├── ISoruDeposu.cs            # Soru veri erişim arayüzü
│   ├── IIlerlemeDeposu.cs        # İlerleme ve seviye geçiş ara sınavı veri erişim arayüzü
│   ├── IlerlemeServisi.cs        # PostgreSQL ilerleme ve seviye_gecis_sinavlari yönetimi
│   ├── KarneServisi.cs           # PostgreSQL yanlış cevap kayıtları + karne raporu
│   ├── PuanlamaServisi.cs        # Disiplin bazlı puanlama (saf fonksiyonlar)
│   ├── SimulatorServisi.cs       # Roslyn tabanlı kod analizi
│   ├── FontOnbellegi.cs          # GDI+ font önbelleği
│   └── VeriYolu.cs               # Dışa aktarım ve yapılandırma dosya yolları
├── tools/
│   └── SoruPaketleyici/          # ClosedXML tabanlı .xlsx / .csv soru paketleme aracı
├── BelgeOnizlemeFormu.cs         # .docx/.pdf soru önizleme, doğrulama ve onay ekranı
├── BufferedPanel.cs              # Çift tamponlu panel (titremesiz çizim)
├── Form1.cs                      # Çekirdek form, sidebar, ekran geçişi, kısayollar
├── Form1.AnaMenu.cs              # Kontrol paneli, modül/zorluk kartları, .docx/.pdf/.xlsx/.csv içe aktarma
├── Form1.Sinav.cs                # Sınav ve Seviye Geçiş Ara Sınavı akışı, puanlama
├── Form1.Simulator.cs            # Roslyn simülatörü ve hata karnesi
├── Form1.Designer.cs
├── Program.cs                    # Giriş noktası + genel hata yakalama
├── appsettings.example.json      # Örnek PostgreSQL bağlantı yapılandırması
├── GEMINI.md                     # Proje ve mimari kuralları
├── tests/
│   └── KademeliEgitimPlatformu.Tests/   # xUnit birim ve entegrasyon testleri
└── KademeliEgitimPlatformu.csproj
```

### Testleri Çalıştırma
```bash
dotnet test tests/KademeliEgitimPlatformu.Tests/KademeliEgitimPlatformu.Tests.csproj
```

---

## 📌 Sürüm Günlüğü ve Yama Notları (Changelog)

### [v0.8.1 — Belge İçe Aktarma Geliştirmesi] — 2026-11-23
* **[Word & PDF Soru Dönüştürücü (`DocumentFormat.OpenXml` + `UglyToad.PdfPig`)]** Son kullanıcıların `.xlsx` hazırlamak zorunda kalmadan `.docx` (paragraf + tablo) ve `.pdf` (sayfa sayfa Türkçe UTF-8) belgelerinden tamamen çevrimdışı soru yükleyebilmesi için `Services/BelgeSoruDonusturucu.cs` eklendi. Klasik test formatları, toplu cevap anahtarı (`1-B, 2-A` / `1. B`), opsiyonel `Zorluk:` / `Modül:` / `Açıklama:` alanları ve Word soru tabloları desteklenir; eksik şıklı veya cevapsız sorular soru numarası ve sebebiyle uyarı olarak raporlanır.
* **[Merkezi Soru Doğrulama (`SoruDogrulayici`)]** `Services/SoruDogrulayici.cs` ile soru bütünlüğü kuralları tek merkezde toplandı ve eksik/hatalı alanlar için Türkçe gerekçe listesi üretildi.
* **[Önizleme ve Kontrol Ekranı (`BelgeOnizlemeFormu`)]** `OpenFileDialog` filtresi `.docx`, `.pdf`, `.xlsx` ve `.csv` uzantılarının tamamını destekleyecek şekilde (`.docx` ve `.pdf` öncelikli) güncellendi. `.docx`/`.pdf` seçiminde veritabanına yazmadan önce önizleme ekranı açılır; hatalı sorular vurgulanır, varsayılan modül seçilebilir ve yalnızca onaylanan geçerli sorular PostgreSQL transaction içinde kaydedilir.
* **[Birim Testleri]** `tests/KademeliEgitimPlatformu.Tests/BelgeSoruDonusturucuTests.cs` altına klasik test formatı, toplu cevap anahtarı, eksik şık/cevap uyarıları, Türkçe karakterler, gerçek `.docx` (tablo + paragraf), gerçek `.pdf` ve önizleme formu smoke testleri eklendi.

### [v0.8.1] — 2026-11-15
* **[Dağıtım & İzin Güvenliği (`Program Files`)]** `Directory.GetCurrentDirectory()` kullanımları tamamen kaldırıldı; `appsettings.json`, `appsettings.example.json` ve `database/schema.sql` gibi salt-okunur dosyalar `AppContext.BaseDirectory` üzerinden okunacak şekilde sabitlendi. Çalışma zamanı log (`hata.log`) ve dışa aktarılan Excel/CSV (`.xlsx`, `.csv`) çıktıları `Environment.SpecialFolder.MyDocuments` (`Belgelerim\KademeliEgitimPlatformu`) klasörüne yönlendirildi.
* **[Otomatik Şema Kontrolü (`Auto-Migration`)]** `VeritabaniBaglantisi.TablolariHazirla()` ve `EksikTablolariGetir()` ile uygulama açılışında `kademeli_egitim` veritabanının ve 7 zorunlu tablonun varlığı kontrol edilerek eksik tabloların `database/schema.sql` (`CREATE TABLE IF NOT EXISTS`) ile otomatik kurulması sağlandı.
* **[Zenginleştirilmiş Başlangıç Verisi (`Seed Data`)]** İlk kurulumda `sorular` tablosu boş olduğunda `GorselProgramlama`, `VeriTabanlari` ve `SiberGuvenlik` disiplinlerinin tamamında `Kolay`, `Orta` ve `Zor` seviyelerini kapsayan 23 soruluk başlangıç havuzunun otomatik tohumlanması sağlandı.
* **[Hata Dayanıklılığı]** PostgreSQL servisinin kapalı olması, hatalı şifre (`28P01`) veya eksik/bozuk `appsettings.json` durumlarında uygulamanın çökmesi engellenerek kullanıcıya çözüm adımlarını içeren Türkçe `MessageBox` uyarıları (`KullaniciDostuHataMesaji`) eklendi.

### [v0.8.0] — 2026-11-15
* **[Mimari / Veritabanı]** Yerel SQLite (`sorular.db`, `ilerleme.db`) yapısı tamamen kaldırılarak **PostgreSQL 18 (`localhost:5432`, `kademeli_egitim`, `Npgsql`)** mimarisine geçildi. Tüm tabloları ve indeksleri oluşturan `database/schema.sql` ile merkezi `VeritabaniBaglantisi` sınıfı eklendi.
* **[Yapılandırma]** Bağlantı dizelerinin kod içine gömülmesi engellendi; `Microsoft.Extensions.Configuration.Json` ile `appsettings.json` dosyasından okunması sağlandı (`appsettings.json` `.gitignore` içine alındı ve `appsettings.example.json` şablonu eklendi).
* **[Güvenlik]** `SHA256` ve `HMACSHA256` kullanımları tamamen kaldırıldı; `BCrypt.Net-Next` (`BCrypt.HashPassword` ve `BCrypt.Verify`) ile tuzlu (salted) parola özetleme uygulayan `KullaniciServisi` ve `SoruKoruyucu` sınıfları yazıldı.
* **[Veri Aktarımı]** JSON ve `.dat` soru paketi formatları kaldırıldı; `ClosedXML` kütüphanesi ile **Excel (`.xlsx`)** ve **`.csv`** formatlarında soru içe/dışa aktarımı (`SoruServisi.DisaridanPaketYukle`, `DisariAktar`, `PaketOlustur`) ve `tools/SoruPaketleyici` desteği eklendi.
* **[Kademeli Geçiş]** `Kolay -> Orta` (%70 baraj) ve `Orta -> Zor` (%75 baraj) geçişlerinde katı blokaj yerine **"Seviye Geçiş Ara Sınavı"** akışı uygulandı. Tüm ara sınav denemeleri PostgreSQL `seviye_gecis_sinavlari` tablosunda kayıt altına alınıyor; öğrenci ara sınavı geçmeden üst zorluk açılmıyor, geçtiğinde ise kilit kalıcı olarak açılıyor.

### [v0.7.0] — 2026-11-12
* **[Güvenlik]** Soru havuzu artık düz metin dağıtılmıyor. `Data/Sorular.dat` paketi ve SQLite'taki `Veri` sütunu **AES-256-GCM** ile şifreleniyor; başlıklar **HMAC-SHA256** özetiyle saklanıyor.
* **[Güvenlik]** Eski (v0.6 ve öncesi) düz metin `sorular.db` dosyaları ilk açılışta otomatik olarak şifreli şemaya yükseltiliyor; USB ile eklenmiş sorular ve kullanıcı ilerlemesi korunuyor.
* **[Eklendi]** Her modül için 5 soruluk **ara sınav**: Görsel Programlama, Veri Tabanları ve Siber Güvenlik. Geçme barajı **%70**; sonuç kalıcı saklanıyor.
* **[Eklendi]** Yeni kademe kuralları: üç ara sınavın tamamı geçilince **Orta Mod**, Orta Mod karma sınavı **%75** ile geçilince **Zor Mod** açılıyor.
* **[Geliştirildi]** Veri erişim katmanını soyutlamak için `ISoruDeposu` ve `IIlerlemeDeposu` arayüzleri eklendi.

### [v0.6.0] — 2026-11-11
* **[Performans]** Ekranlar tembel oluşturuluyor: açılışta yalnızca ana menü kurulur; sınav ve simülatör ilk açılışta hazırlanır.
* **[Performans]** Roslyn kod analizi `Task.Run` ile arka planda çalışıyor; analiz sırasında arayüz donmuyor.
* **[Performans]** Panellerde çift tampon (`BufferedPanel`), `SuspendLayout/ResumeLayout` ve `ResizeRedraw` kullanılarak titreme azaltıldı.

### [v0.5.0] — 2026-11-11
* **[Eklendi]** Kod simülatörü Regex tahmininden **gerçek Roslyn derleyicisine** (`Microsoft.CodeAnalysis.CSharp`) taşındı.
* **[Eklendi]** Soru bazlı kalıcı hata karnesi, şık karıştırma (`SoruGorunumu`), `1`–`4` ve `Enter` klavye kısayolları ve xUnit test paketi eklendi.

### [v0.4.2] — 2026-11-10
* **[Düzeltildi]** Disiplin bazlı puanlama, zorluk filtresi, satır numarası hizalama ve genel hata yakalama iyileştirmeleri yapıldı.

### [v0.3.0] — 2026-11-09
* **[Eklendi]** Sol yan navigasyon menüsü (Sidebar), özel çizimli parlak kart çerçeveleri, VS Code tarzı satır numaralı kod görüntüleyici ve canlı süre sayacı eklendi.

### [v0.2.0] — 2026-11-09
* **[Eklendi]** `TableLayoutPanel` ve DPI ölçeklendirme ile 1080p Full HD tam ekran responsive tasarım, Gömülü Kod Simülatörü ve İpucu Jetonu eklendi.

### [v0.1.0] — 2026-11-09
* **[Eklendi]** Windows Forms (.NET 8.0) proje iskeleti, `Models`/`Services` katmanlı mimarisi ve "Neden Yanlış Açıklaması" paneli oluşturuldu.

---

## 📄 Lisans

Bu proje [MIT](LICENSE) lisansı ile lisanslanmıştır.
