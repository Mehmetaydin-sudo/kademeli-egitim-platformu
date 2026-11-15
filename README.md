# 🛡️ Kademeli Bilişim ve Siber Güvenlik Eğitim Platformu

Ortaöğretim ve ön lisans öğrencileri için **PostgreSQL 18 (`Npgsql`)**, **`BCrypt.Net-Next`**, **`ClosedXML` (`.xlsx` / `.csv`)** ve **gömülü Roslyn derleyicisi** mimarili masaüstü öğrenme ve sözdizimi (syntax) simülasyon platformu.

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

İlk açılışta `sorular` tablosu boşsa uygulama 18 soruluk varsayılan eğitim ve ara sınav havuzunu otomatik olarak PostgreSQL veritabanına tohumlar.

---

## 📊 Soru Havuzu ve Excel (`.xlsx`) / `.csv` Paket Biçimi

Soru içe ve dışa aktarım işlemlerinde **Excel (`.xlsx`)** ve **`.csv`** formatları kullanılır. Tablo sütun sırası aşağıdaki gibidir:

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
├── BufferedPanel.cs              # Çift tamponlu panel (titremesiz çizim)
├── Form1.cs                      # Çekirdek form, sidebar, ekran geçişi, kısayollar
├── Form1.AnaMenu.cs              # Kontrol paneli, modül/zorluk kartları, .xlsx/.csv içe aktarma
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
