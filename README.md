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

* **%100 Çevrimdışı SQLite Mimarisi:** Sunucu veya internet gerektirmeden sorular `sorular.db`, kullanıcı ilerlemesi ise `ilerleme.db` üzerinde yerel SQLite tablolarında tutulur.
* **Kademeli Zorluk Sistemi:**
  * **Kolay Mod** — her zaman açıktır.
  * **Orta Mod** — en az bir modülde **%75 başarı** ile açılır (kilit gerçekten uygulanır, yalnızca görsel değildir).
  * **Zor Mod** — gizli sözdizimi tuzaklarını içeren gömülü kod simülatörüdür.
* **Pedagojik "Neden Yanlış" Paneli:** Yanlış şık seçildiğinde hatanın kaynağını sade bir dille anında açıklar.
* **Gömülü Kod Simülatörü:** Editördeki C# kod bloğunu yerel Regex kurallarıyla denetler (iki tanımlı tuzak: `if (...)` sonundaki `;` ve `=` / `==` karışıklığı).
* **Güvenli USB Paket Yükleme:** İnternetsiz ortamlarda dışarıdan `.json` soru paketlerini şema doğrulaması (`SoruGecerliMi`) ve 5 MB boyut sınırıyla SQLite veri tabanına aktarır. Hatalı JSON için anlaşılır hata mesajı üretir.
* **Kalıcı Oyunlaştırma:** Cihaz içi SQLite profilinde korunan başarı yüzdeleri, ipucu jetonları ve rozetler; masaüstüne indirilebilir `.txt` özet karne raporu.
* **Disiplin Bazlı Puanlama:** Karma sınavda her disiplin kendi sorularından ayrı değerlendirilir; bir modülün yüzdesi diğerlerini ezmez.
* **Soru Karıştırma:** Her sınavda soru sırası rastgelelenir (ezberlemeyi zorlaştırır).
* **Dayanıklı Veri Konumu:** Uygulama klasörü yazılabilirse veriler orada (`Data/`), değilse `%LOCALAPPDATA%\KademeliEgitimPlatformu\Data` altında tutulur. Böylece Program Files kurulumlarında yönetici izni gerekmez.
* **Soru Havuzu Senkronizasyonu:** `Data/Sorular.json` her açılışta veritabanıyla senkronize edilir; yeni sorular eklenir, aynı başlıklı sorular güncellenir.

---

## 🖥️ Kurulum ve Çalıştırma

### Gereksinimler
* Windows 10/11
* **.NET 8 SDK** (geliştirme için) veya **.NET 8 Desktop Runtime** (yalnızca çalıştırma için)

### Kaynaktan çalıştırma
```bash
git clone https://github.com/Mehmetaydin-sudo/kademeli-egitim-platformu.git
cd kademeli-egitim-platformu
dotnet run
```

Visual Studio ile açmak için `KademeliEgitimPlatformu.slnx` dosyasını kullanın (Visual Studio 2022 17.13 veya üzeri).

### Yayınlama (publish)
```bash
dotnet publish -c Release -r win-x64 --self-contained false
```
`Data/Sorular.json` çıktı klasörüne otomatik kopyalanır.

---

## 📚 Soru Havuzu ve Paket Biçimi

Soru havuzu `Data/Sorular.json` dosyasındadır. Dışarıdan yüklenen paketler de aynı biçimi kullanır:

```json
[
  {
    "Disiplin": "GorselProgramlama",
    "Zorluk": "Orta",
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
* Her soruda **tam 4 şık** bulunmalıdır; `DogruSecenekIndex` 0–3 arasındadır.
* Aynı `Baslik` ile ikinci bir soru eklenmez (başlık benzersizdir).

> **Not:** Depodaki örnek havuz şu anda **3 soru** içeriyor (Kolay 2, Orta 1, Zor 0). Gerçek kullanımda havuzun genişletilmesi gerekir; Zor seviyede soru bulunmadığı için Zor Mod, kod simülatörü olarak çalışır.

---

## 🗂️ Proje Yapısı

```
KademeliEgitimPlatformu/
├── Data/
│   └── Sorular.json          # Başlangıç soru havuzu
├── Models/
│   └── Soru.cs               # Soru, DisiplinTuru, ZorlukSeviyesi
├── Services/
│   ├── SoruServisi.cs        # SQLite soru havuzu + paket yükleme + senkronizasyon
│   ├── IlerlemeServisi.cs    # Kalıcı kullanıcı ilerlemesi (ilerleme.db)
│   └── VeriYolu.cs           # Yazılabilir veri klasörünü belirler
├── Form1.cs                  # Arayüz ve ekran akışı
├── Form1.Designer.cs
├── Program.cs                # Giriş noktası + genel hata yakalama
└── KademeliEgitimPlatformu.csproj
```

---

## 📌 Sürüm Günlüğü ve Yama Notları (Changelog)

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

* Depodaki örnek soru havuzu **3 sorudur** (Kolay 2, Orta 1, Zor 0). Zor seviyede soru eklenene kadar Zor Mod, kod simülatöründen ibarettir.
* Kod simülatörü gerçek bir C# derleyicisi değildir; iki tanımlı kalıbı Regex ile denetler. Kullanıcı değişken adlarını değiştirirse sonuç yanıltıcı olabilir.
* "Çevrimdışı Hata Karnesi" yalnızca başarı yüzdelerini özetleyen bir `.txt` raporudur; soru bazlı yanlış dökümü içermez.
* Arayüzde font önbellekleme yoktur; her ekran çiziminde yeni `Font` nesneleri oluşturulur.
* Otomatik testler ve CI yapılandırması bulunmamaktadır.

---

## 📄 Lisans

Bu proje [MIT](LICENSE) lisansı ile lisanslanmıştır.
