# 🛡️ Kademeli Bilişim ve Siber Güvenlik Eğitim Platformu

Ortaöğretim ve ön lisans öğrencileri için **%100 çevrimdışı (offline)** çalışan, yerel SQLite veri tabanı mimarili masaüstü öğrenme ve sözdizimi (syntax) simülasyon platformu.

---

## 👥 Proje Geliştirme Ekibi
* **Mehmet Aydın** — Yazılım Mimarisi, SQLite Veri Tabanı Motoru ve Gömülü Kod Simülatörü
* **Ünal Küçük** — Görsel Programlama & Veri Tabanları Soru Havuzu ve Pedagojik Açıklamalar
* **İsmail Can Şal** — Siber Güvenlik Modülü ve Gizli Syntax Tuzak Senaryoları
* **Yağız Domaç** — Arayüz (UI/UX) Tasarımı, Oyunlaştırma Rozetleri ve Çevrimdışı Testler

---

## 🚀 Temel Özellikler

* **%100 Çevrimdışı SQLite Mimarisi:** Sunucu veya internet gerektirmeden sorular `sorular.db`, kullanıcı ilerlemesi ise `ilerleme.db` üzerinde yerel SQLite tablolarında tutulur. Veri erişimi `ISoruDeposu` / `IIlerlemeDeposu` arayüzleri arkasındadır; ileride merkezi bir sunucuya (ör. PostgreSQL) geçiş için yalnızca yeni depo uygulaması yazılır.
* **Şifreli Soru Havuzu:** Sorular düz metin olarak dağıtılmaz. `Data/Sorular.dat` paketi ve SQLite'taki `Veri` sütunu **AES-256-GCM** ile şifrelenir; başlıklar **HMAC-SHA256** özeti olarak saklanır. Böylece dosya veya veritabanı bir araçla açılsa bile soru metinleri okunamaz.
* **Kademeli Zorluk Sistemi:**
  * **Kolay Mod** — her zaman açıktır.
  * **Ara Sınavlar** — her modül için 5 soruluk bir ara sınav vardır; geçme barajı **%70**'tir.
  * **Orta Mod** — üç modülün ara sınavı da geçildiğinde açılır; karma sınav barajı **%75**'tir.
  * **Zor Mod** — Orta Mod karma sınavı %75 ile geçilince açılan, gizli sözdizimi tuzaklarını içeren gömülü kod simülatörüdür. Kilit yalnızca görsel değildir; giriş noktaları da kontrol eder.
* **Pedagojik "Neden Yanlış" Paneli:** Yanlış şık seçildiğinde hatanın kaynağını sade bir dille anında açıklar.
* **Gömülü Kod Simülatörü (Roslyn):** Editördeki C# kodunu gerçek Roslyn derleyicisiyle, tamamen çevrimdışı denetler. Hatalar satır/sütun numarası ve Türkçe pedagojik ipuçlarıyla listelenir; sabit iki kalıba bağlı değildir.
* **Güvenli USB Paket Yükleme:** İnternetsiz ortamlarda dışarıdan şifreli `.dat` veya düz `.json` soru paketlerini şema doğrulaması (`SoruGecerliMi`) ve 5 MB boyut sınırıyla SQLite veri tabanına aktarır. Bozuk/yanlış anahtarlı paketler için anlaşılır hata mesajı üretir.
* **Kalıcı Oyunlaştırma:** Cihaz içi SQLite profilinde korunan başarı yüzdeleri, ipucu jetonları ve rozetler; masaüstüne indirilebilir `.txt` hata karnesi.
* **Soru Bazlı Hata Karnesi:** Yanlış cevaplar (soru, seçilen cevap, doğru cevap ve açıklama) `ilerleme.db` içindeki `YanlisCevaplar` tablosunda saklanır; karne son oturumla sınırlı değildir (en yeni 300 kayıt).
* **Disiplin Bazlı Puanlama:** Karma sınavda her disiplin kendi sorularından ayrı değerlendirilir; bir modülün yüzdesi diğerlerini ezmez.
* **Soru ve Şık Karıştırma:** Her sınavda soru sırası ve şıklar rastgelelenir; doğru cevap ve yanlış açıklamaları yeni sıraya birlikte taşınır (ezberleme önlenir).
* **Klavye Kısayolları:** Sınav sırasında `1`–`4` tuşlarıyla şık seçilir, `Enter` ile sonraki soruya geçilir.
* **Font Önbelleği:** Arayüz fontları `ConcurrentDictionary` tabanlı önbellekten alınır; ekran çizimlerinde GDI+ tutamacı sızıntısı oluşmaz.
* **Düşük Donanım Optimizasyonu:** Ekranlar tembel oluşturulur, SQLite WAL kullanılır, değişmeyen `Sorular.dat` senkronizasyonu HMAC-SHA256 özetiyle atlanır, Roslyn analizi arka planda çalışır ve paneller çift tamponludur. 1366×768 çözünürlük ve 4 GB RAM'li makineler hedeflenir.
* **Otomatik Testler ve CI:** xUnit test paketi ve GitHub Actions iş akışı; her push'ta Release derlemesi ve testler çalışır.
* **Dayanıklı Veri Konumu:** Uygulama klasörü yazılabilirse veriler orada (`Data/`), değilse `%LOCALAPPDATA%\KademeliEgitimPlatformu\Data` altında tutulur. Böylece Program Files kurulumlarında yönetici izni gerekmez.
* **Soru Havuzu Senkronizasyonu:** `Data/Sorular.dat` (şifreli paket) her açılışta veritabanıyla senkronize edilir; yeni sorular eklenir, aynı başlıklı sorular güncellenir. Paket değişmediyse HMAC özeti sayesinde senkronizasyon atlanır.

---

## 🖥️ Kurulum ve Çalıştırma

### Gereksinimler
* Windows 10/11 (32-bit veya 64-bit)
* **.NET 8 SDK** (geliştirme için). `Okul-x86` yayını self-contained olduğu için öğrenci bilgisayarlarında .NET kurulumu gerekmez.

### Kaynaktan çalıştırma
```bash
git clone https://github.com/Mehmetaydin-sudo/kademeli-egitim-platformu.git
cd kademeli-egitim-platformu
dotnet run
```

Visual Studio ile açmak için `KademeliEgitimPlatformu.slnx` dosyasını kullanın (Visual Studio 2022 17.13 veya üzeri).

### Yayınlama (publish)

Öğrenci ve okul bilgisayarları için **x86** profili önerilir. x86 sürümü hem 32-bit hem 64-bit Windows 10/11'de çalışır; self-contained olduğu için makinelerde .NET kurulu olması gerekmez.

```bash
dotnet publish KademeliEgitimPlatformu.csproj -p:PublishProfile=Okul-x86
```

Çıktı: `bin\Release\net8.0-windows\publish\win-x86\` (ReadyToRun açık, trimming kapalı).
Yayın çıktısına **yalnızca şifreli `Data/Sorular.dat`** kopyalanır; düz metin `Sorular.json` öğrenci bilgisayarlarına gitmez.

Yalnızca 64-bit makineler için daha küçük bir yayın istenirse:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishReadyToRun=true
```

---

## 📚 Soru Havuzu ve Paket Biçimi

Soru havuzunun düzenlenebilir kaynağı `Data/Sorular.json` dosyasıdır. Uygulama yayınlanırken bu dosya şifreli `Data/Sorular.dat` paketine dönüştürülür. Dışarıdan yüklenen paketler de aynı biçimi kullanır:

```json
[
  {
    "Disiplin": "GorselProgramlama",
    "Zorluk": "Orta",
    "Etiket": "AraSinav",
    "Baslik": "Soru metni buraya yazılır.",
    "KodBlogu": "// İsteğe bağlı kod bloğu\nint x = 1;",
    "Secenekler": ["A şıkkı", "B şıkkı", "C şıkkı", "D şıkkı"],
    "DogruSecenekIndex": 2,
    "YanlisCevapAciklamalari": {
      "0": "Neden A yanlış?",
      "1": "Neden B yanlış?",
      "3": "Neden D yanlış?"
    }
  }
]
```

* `Disiplin`: `GorselProgramlama`, `VeriTabanlari`, `SiberGuvenlik` (sayısal değerler de kabul edilir).
* `Zorluk`: `Kolay`, `Orta`, `Zor` (sayısal değerler de kabul edilir).
* `Etiket`: İsteğe bağlıdır. `AraSinav` olan sorular yalnızca modül ara sınavlarında kullanılır; boş bırakılan sorular normal modül/karma sınav havuzuna girer.
* Her soruda **tam 4 şık** bulunmalıdır; `DogruSecenekIndex` 0–3 arasındadır.
* Aynı `Baslik` ile ikinci bir soru eklenmez (başlık benzersizdir; veritabanında HMAC özetiyle saklanır).

### Şifreli paket üretimi

`Data/Sorular.json` her değiştiğinde şifreli paket yeniden üretilmelidir:

```bash
dotnet run --project tools/SoruPaketleyici -- Data/Sorular.json Data/Sorular.dat
```

Araç, tüm soruları `SoruGecerliMi` ile doğrular; geçersiz soru varsa hangi başlıkta olduğunu söyleyerek durur. Çıktı **AES-256-GCM** ile şifrelenir ve `KEP1` imzası taşır; dosya içinde düz metin soru, başlık veya `AraSinav` etiketi bulunmaz.

> **Not:** Depodaki havuz **18 soru** içeriyor: 3 genel soru (Görsel Programlama, Veri Tabanları, Siber Güvenlik) ve her modül için 5'er ara sınav sorusu. Gerçek kullanımda havuzun genişletilmesi önerilir.

---

## 🗂️ Proje Yapısı

```
KademeliEgitimPlatformu/
├── Data/
│   ├── Sorular.json          # Düzenlenebilir soru kaynağı (yayına kopyalanmaz)
│   └── Sorular.dat           # AES-256-GCM ile şifrelenmiş soru paketi
├── Models/
│   ├── Soru.cs               # Soru, DisiplinTuru, ZorlukSeviyesi, Etiket
│   ├── SinavTuru.cs          # Normal sınav / ara sınav ayrımı
│   ├── SoruGorunumu.cs       # Şık karıştırma + doğru cevap/açıklama eşlemesi
│   └── YanlisKaydi.cs        # Hata karnesi kaydı
├── Services/
│   ├── SoruServisi.cs        # Şifreli SQLite soru havuzu + paket yükleme + migrasyon
│   ├── SoruKoruyucu.cs       # AES-256-GCM şifreleme ve HMAC-SHA256 başlık özeti
│   ├── KademeServisi.cs      # Ara sınav / Orta Mod / Zor Mod geçiş kuralları
│   ├── ISoruDeposu.cs        # Soru veri erişim arayüzü (sunucu geçişine hazır)
│   ├── IIlerlemeDeposu.cs    # İlerleme veri erişim arayüzü
│   ├── IlerlemeServisi.cs    # Kalıcı kullanıcı ilerlemesi (ilerleme.db)
│   ├── KarneServisi.cs       # Yanlış cevap kayıtları + karne raporu
│   ├── PuanlamaServisi.cs    # Disiplin bazlı puanlama (saf fonksiyonlar)
│   ├── SimulatorServisi.cs   # Roslyn tabanlı çevrimdışı kod analizi
│   ├── FontOnbellegi.cs      # GDI+ font önbelleği
│   ├── SqliteYardimcisi.cs   # WAL / busy_timeout ortak bağlantı ayarları
│   └── VeriYolu.cs           # Yazılabilir veri klasörünü belirler
├── tools/
│   └── SoruPaketleyici/      # JSON → şifreli .dat paket üretim aracı
├── BufferedPanel.cs          # Çift tamponlu panel (titremesiz çizim)
├── Form1.cs                  # Çekirdek form, sidebar, ekran geçişi, kısayollar
├── Form1.AnaMenu.cs          # Kontrol paneli, modül/zorluk kartları, USB paket yükleme
├── Form1.Sinav.cs            # Sınav ve ara sınav akışı, şık karıştırma, puanlama
├── Form1.Simulator.cs        # Roslyn simülatörü ve hata karnesi
├── Form1.Designer.cs
├── Program.cs                # Giriş noktası + genel hata yakalama
├── Properties/
│   └── PublishProfiles/
│       └── Okul-x86.pubxml   # Öğrenci bilgisayarları için x86 yayın profili
├── tests/
│   └── KademeliEgitimPlatformu.Tests/   # xUnit testleri
├── .github/workflows/ci.yml  # GitHub Actions derleme + test
└── KademeliEgitimPlatformu.csproj
```

### Testleri çalıştırma
```bash
dotnet test tests/KademeliEgitimPlatformu.Tests/KademeliEgitimPlatformu.Tests.csproj
```

---

## 📌 Sürüm Günlüğü ve Yama Notları (Changelog)

### [v0.7.0] — 2026-11-12
* **[Güvenlik]** Soru havuzu artık düz metin dağıtılmıyor. `Data/Sorular.dat` paketi ve SQLite'taki `Veri` sütunu **AES-256-GCM** ile şifreleniyor; başlıklar **HMAC-SHA256** özetiyle saklanıyor. SHA-256'nın "soruları metin olarak okunamaz yapma" beklentisini karşılamadığı hocanın geri bildirimi üzerine doğru kriptografik araçlara geçildi.
* **[Güvenlik]** Eski (v0.6 ve öncesi) düz metin `sorular.db` dosyaları ilk açılışta otomatik olarak şifreli şemaya yükseltiliyor; USB ile eklenmiş sorular ve kullanıcı ilerlemesi korunuyor. Veri kaybı yok.
* **[Güvenlik]** `Data/Sorular.json` yalnızca geliştirici kaynağı olarak repoda kalıyor; yayın çıktısına ve öğrenci bilgisayarlarına **kopyalanmıyor**. Paket üretimi `tools/SoruPaketleyici` ile yapılıyor.
* **[Eklendi]** Her modül için 5 soruluk **ara sınav**: Görsel Programlama, Veri Tabanları ve Siber Güvenlik. Geçme barajı **%70**; sonuç `ilerleme.db` içinde kalıcı saklanıyor.
* **[Eklendi]** Yeni kademe kuralları: üç ara sınavın tamamı geçilince **Orta Mod**, Orta Mod karma sınavı **%75** ile geçilince **Zor Mod** açılıyor. Zor Mod kilidi yalnızca görsel değil; menü ve simülatör giriş noktaları da kontrol ediyor.
* **[Eklendi]** Modül kartlarında "Ara Sınav" butonu ve geçildi/bekliyor durumu; zorluk kartlarında eksik ara sınavların adları gösteriliyor.
* **[Geliştirildi]** SQLite'a bağımlılığı azaltmak için `ISoruDeposu` ve `IIlerlemeDeposu` arayüzleri eklendi. İleride merkezi/çok kullanıcılı kullanım için PostgreSQL gibi bir sunucuya geçişte arayüz kodu değişmeyecek; yalnızca yeni depo uygulaması yazılacak.
* **[Geliştirildi]** Soru havuzu 18 soruya çıkarıldı: 3 genel soru + her modül için 5 ara sınav sorusu. SHA-256/PBKDF2/HMAC/AES içerikleri hocanın geri bildirimini doğrudan karşılayacak biçimde eklendi.
* **[Geliştirildi]** Test sayısı **65**'e çıktı: şifreleme/paket, düz metin sızıntısı, DB migrasyonu, ara sınav filtresi ve kademe geçiş kuralları test ediliyor.

### [v0.6.0] — 2026-11-11
* **[Performans]** Ekranlar tembel oluşturuluyor: açılışta yalnızca ana menü kurulur; sınav ve simülatör ilk açılışta hazırlanır. Menüye her dönüşte yapılan gereksiz yeniden çizim kaldırıldı.
* **[Performans]** Ana menüdeki soru sayısı `TumSorulariGetir().Count` yerine SQLite `COUNT(*)` ile alınıyor; tüm soruların JSON'dan çözülmesi engellendi.
* **[Performans]** `Sorular.json` dosyasının SHA-256 özeti değişmediyse senkronizasyon tamamen atlanıyor; her açılışta gereksiz JSON çözümleme ve yazma yapılmıyor.
* **[Performans]** SQLite bağlantılarına WAL günlükleme, `busy_timeout=3000` ve `synchronous=NORMAL` ayarları eklendi (`SqliteYardimcisi`).
* **[Performans]** Roslyn kod analizi `Task.Run` ile arka planda çalışıyor; 5 saniyeye kadar sürebilen analiz sırasında arayüz donmuyor.
* **[Performans]** Panellerde çift tampon (`BufferedPanel`), `SuspendLayout/ResumeLayout` ve `ResizeRedraw` kullanılarak titreme ve gereksiz yeniden çizim azaltıldı.
* **[Performans]** Minimum pencere boyutu 1024×700'e düşürüldü; 1366×768 ekranlarda görev çubuğuyla birlikte pencere ekrana sığar.
* **[Yayın]** `Okul-x86` yayın profili eklendi: self-contained, ReadyToRun, trimming kapalı; hem 32-bit hem 64-bit Windows 10/11'de çalışır.
* **[Geliştirildi]** `SoruServisi` yol enjeksiyonu (`jsonYolu`, `veritabaniYolu`) ve `SoruSayisi()` ile test edilebilir hale getirildi; test sayısı 43'e çıktı.

### [v0.5.0] — 2026-11-11
* **[Eklendi]** Kod simülatörü Regex tahmininden **gerçek Roslyn derleyicisine** (`Microsoft.CodeAnalysis.CSharp`) taşındı. Hatalar satır/sütun bilgisiyle ve kararlı tanı kodlarına (CS1002, CS0029, CS0165 vb.) dayalı Türkçe ipuçlarıyla gösterilir.
* **[Eklendi]** Soru bazlı kalıcı hata karnesi: yanlış cevaplar `ilerleme.db` içindeki `YanlisCevaplar` tablosuna yazılır (en yeni 300 kayıt). `.txt` raporu artık soru metnini, seçilen cevabı, doğru cevabı ve açıklamayı içerir; UTF-8 BOM ile kaydedilir.
* **[Eklendi]** Şıklar her sınavda karıştırılır; `SoruGorunumu` doğru cevap indeksini ve yanlış açıklamalarını yeni sıraya taşır.
* **[Eklendi]** Sınavda `1`–`4` tuşlarıyla şık seçimi ve `Enter` ile sonraki soru kısayolları.
* **[Eklendi]** xUnit test paketi (41 test: puanlama, karne, ilerleme, şık karıştırma, Roslyn analizi, `SoruGecerliMi`, Form1 smoke) ve GitHub Actions CI iş akışı.
* **[Geliştirildi]** `Form1.cs` partial class olarak ekranlara bölündü: `Form1.AnaMenu.cs`, `Form1.Sinav.cs`, `Form1.Simulator.cs`. Davranış korunurken bakım maliyeti düştü.
* **[Geliştirildi]** İş mantığı arayüzden ayrıldı: `PuanlamaServisi`, `KarneServisi`, `SimulatorServisi`, `FontOnbellegi`.
* **[Düzeltildi]** Her ekran çiziminde yeni `Font` nesnesi oluşturulmasından kaynaklanan GDI+ tutamacı sızıntısı, `ConcurrentDictionary` tabanlı font önbelleğiyle giderildi.
* **[Düzeltildi]** Simülatör kodu ekran geçişlerinde artık kaybolmuyor; analiz sonucu ve renk durumu korunuyor.
* **[Düzeltildi]** Roslyn tanı mesajları sistem diline göre yerelleştiği için ipucu eşlemesi mesaj metni yerine tanı koduna bağlandı.
* **[Düzeltildi]** USB paket yüklemede `FileNotFoundException`, `InvalidDataException` ve `UnauthorizedAccessException` ayrı, anlaşılır mesajlarla ele alınıyor.
* **[Geliştirildi]** `.slnx` dosyasına test projesi eklendi; sürüm `0.5.0` olarak güncellendi.

### [v0.4.2] — 2026-11-10
* **[Düzeltildi]** `YanlisCevapAciklamalari` alanı `null` olan soruların yanlış cevap seçildiğinde `NullReferenceException` ile uygulamayı çökertmesi engellendi; alanlar okuma sırasında normalize ediliyor.
* **[Düzeltildi]** Zorluk filtresi gerçekten uygulanıyor: `Kolay`/`Orta` seviyeleri sınav başlatırken kullanılıyor ve `%75` Orta Mod kilidi artık atlanamıyor.
* **[Düzeltildi]** Karma sınav artık üç disipline aynı yüzdeyi yazmıyor; her disiplin kendi sorularından ayrı puanlanıyor.
* **[Düzeltildi]** Yeni kullanıcının varsayılan başarı yüzdeleri `65/40/10` yerine `0/0/0`; kazanılmamış ilerleme gösterilmiyor.
* **[Düzeltildi]** Veritabanı yolu, yazılamayan kurulum klasörlerinde (Program Files) uygulamanın açılmamasına yol açmıyor; `%LOCALAPPDATA%` yedeği eklendi.
* **[Düzeltildi]** `Sorular.json` yalnızca ilk çalıştırmada değil, her açılışta senkronize ediliyor; güncellenen sorular mevcut kullanıcılara da ulaşıyor.
* **[Düzeltildi]** Kod görüntüleyicide satır numaraları kaydırma ile senkron hale getirildi ve uzun satırların sarması (`WordWrap`) kapatıldı.
* **[Düzeltildi]** Ana menü açılışta iki kez çizilmiyor; sınav zamanlayıcısı form kapanırken dispose ediliyor.
* **[Eklendi]** Genel hata yakalama: beklenmeyen hatalarda kullanıcıya mesaj gösterilir ve `hata.log` dosyasına yazılır.
* **[Eklendi]** Sınav soruları rastgele sıralanır.
* **[Geliştirildi]** `.gitignore` dosyasına yerel SQLite dosyaları (`Data/*.db`) eklendi.

### [v0.4.1] — 2026-11-10
* **[Eklendi]** `IlerlemeServisi` ana arayüze (`Form1.cs`) bağlandı; uygulama kapatılıp açıldığında başarı yüzdeleri, rozetler ve ipucu jetonları `Data/ilerleme.db` üzerinden kalıcı olarak yükleniyor.
* **[Geliştirildi]** USB paket yükleme sonucuna geçersiz olduğu için atlanan soru sayısı (`SonPaketAtlananSayisi`) bildirimi eklendi.

### [v0.4.0] — 2026-11-10
* **[Eklendi]** `SoruServisi` tamamen `Microsoft.Data.Sqlite` tabanlı yerel veri tabanı (`Data/sorular.db`) mimarisine geçirildi.
* **[Eklendi]** `Sorular.json` verilerini SQLite tablosuna aktaran otomatik tohumlama (`JsondanIlkAktarim`) ve `Disiplin/Zorluk` indekslemesi eklendi.
* **[Eklendi]** Kullanıcı ilerlemesini ACID Transaction güvencesiyle saklayan `IlerlemeServisi` (`Data/ilerleme.db`) oluşturuldu.
* **[Güvenlik]** Tüm SQL sorgularında parametreli yapı ve `UPSERT (ON CONFLICT)` kullanılarak SQL Injection önlemleri alındı; dış paket yüklemelerine 5 MB boyut sınırı ve `SoruGecerliMi` şema doğrulaması getirildi.

### [v0.3.1] — 2026-11-10
* **[Düzeltildi]** Ekran geçişlerinde oluşan kontrol/bellek sızıntısı `PanelTemizle` (`.Dispose()`) metodu ile giderildi.
* **[Düzeltildi]** Sınav sonunda başarı yüzdesinin yalnızca ilgili disipline yazılması sağlandı ve simülatörde tekrarlı puan kazanma açığı kapatıldı.

### [v0.3.0] — 2026-11-09
* **[Eklendi]** Sol yan navigasyon menüsü (Sidebar), özel çizimli parlak kart çerçeveleri ve VS Code tarzı satır numaralı kod görüntüleyici eklendi.
* **[Eklendi]** Sınav ekranına canlı süre sayacı (kronometre) entegre edildi.

### [v0.2.0] — 2026-11-09
* **[Eklendi]** `TableLayoutPanel` ve DPI ölçeklendirme ile 1080p Full HD tam ekran responsive tasarım uygulandı.
* **[Eklendi]** Zor Mod Gömülü Kod Simülatörü, İpucu Jetonu ve masaüstüne `.txt` Çevrimdışı Hata Karnesi indirme özelliği eklendi.

### [v0.1.1] — 2026-11-09
* **[Eklendi]** `Models`, `Services` ve `Data` katmanlı mimarisi, koyu tema arayüz ve "Neden Yanlış Açıklaması" paneli oluşturuldu.

### [v0.1.0] — 2026-11-09
* **[Eklendi]** Windows Forms (.NET 8.0) proje iskeleti ve GitHub depo bağlantısı kuruldu.

---

## ⚠️ Bilinen Sınırlar

* Depodaki örnek soru havuzu **18 sorudur**: 3 genel soru ve her modül için 5'er ara sınav sorusu. Gerçek sınav kapsamı için havuzun genişletilmesi gerekir.
* **Şifreleme sınırı:** Uygulama çevrimdışı dağıtıldığı için AES anahtarı programın içindedir. Kararlı bir kişi derlenmiş uygulamadan anahtarı çıkarıp paketi açabilir. Bu katman düz metin erişimini ve kopyala-yapıştır yoluyla soru sızmasını engelleyen **caydırıcı** bir korumadır; gerçek sınav güvenliği için soruların yetkili bir sunucudan gelmesi ve anahtarın istemcide bulunmaması gerekir.
* **SQLite sınırı:** SQLite tek dosyalı yerel kullanım için uygundur; aynı anda çok sayıda kullanıcının aynı veriye yazdığı merkezi senaryolarda yetersiz kalabilir. Bu nedenle veri erişimi arayüzler (`ISoruDeposu`, `IIlerlemeDeposu`) arkasına alındı; sunucu tabanlı bir veritabanına geçiş için yalnızca yeni depo uygulaması gerekir.
* Roslyn simülatörü kodu bellek içinde derler ve çalıştırmaz; yalnızca sözdizimi ve tip hatalarını bildirir. 20.000 karakter sınırı ve 5 saniyelik analiz zaman aşımı vardır.
* Hata karnesi en yeni **300** yanlış kaydı tutar; daha eski kayıtlar otomatik budanır.

---

## 📄 Lisans

Bu proje [MIT](LICENSE) lisansı ile lisanslanmıştır.
