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
* **%100 Çevrimdışı SQLite Mimarisi:** Sunucu veya internet gerektirmeden sorular `Data/sorular.db`, kullanıcı ilerlemesi ise `Data/ilerleme.db` üzerinde yerel SQLite tablolarında tutulur.
* **Kademeli 3 Zorluk Modu:** Kolay Mod, Orta Mod (**%75 Başarı Barajı Kilitli**) ve Zor Mod (Canlı Sözdizimi Tuzak Analizi).
* **Pedagojik "Neden Yanlış" Paneli:** Yanlış şık seçildiğinde hatanın kaynağını sade bir dille anında açıklar.
* **Gömülü Kod Simülatörü:** C# kod bloklarındaki gizli syntax hatalarını yerel Regex motoruyla denetler.
* **Güvenli USB Paket Yükleme:** İnternetsiz ortamlarda dışarıdan `.json` soru paketlerini şema doğrulaması (`SoruGecerliMi`) ve 5 MB boyut sınırıyla SQLite veri tabanına aktarır.
* **Kalıcı Oyunlaştırma & Hata Karnesi:** Cihaz içi SQLite profilinde korunan başarı yüzdeleri, ipucu jetonları, rozetler ve masaüstüne indirilebilir `.txt` karne raporu sunar.

---

## 📌 Sürüm Günlüğü ve Yama Notları (Changelog)

### [v0.4.1] — 2026-11-10
* **[Eklendi]** `IlerlemeServisi` ana arayüze (`Form1.cs`) bağlandı; uygulama kapatılıp açıldığında başarı yüzdeleri, rozetler ve ipucu jetonları `Data/ilerleme.db` üzerinden kalıcı olarak yükleniyor.
* **[Geliştirildi]** USB paket yükleme sonucuna geçersiz olduğu için atlanan soru sayısı (`SonPaketAtlananSayisi`) bildirimi eklendi.

### [v0.4.0] — 2026-11-10
* **[Eklendi]** `SoruServisi` tamamen `Microsoft.Data.Sqlite` tabanlı yerel veri tabanı (`Data/sorular.db`) mimarisine geçirildi.
* **[Eklendi]** İlk çalıştırmada `Sorular.json` verilerini SQLite tablosuna aktaran otomatik tohumlama (`JsondanIlkAktarim`) ve `Disiplin/Zorluk` indekslemesi eklendi.
* **[Eklendi]** Kullanıcı ilerlemesini ACID Transaction güvencesiyle saklayan `IlerlemeServisi` (`Data/ilerleme.db`) oluşturuldu.
* **[Güvenlik]** Tüm SQL sorgularında parametreli yapı ve `UPSERT (ON CONFLICT)` kullanılarak SQL Injection önlemleri alındı; dış paket yüklemelerine 5 MB boyut sınırı ve `SoruGecerliMi` şema doğrulaması getirildi.

### [v0.3.1] — 2026-11-10
* **[Düzeltildi]** Ekran geçişlerinde oluşan GDI/bellek sızıntısı `PanelTemizle` (`.Dispose()`) metodu ve font önbellekleme ile giderildi.
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
