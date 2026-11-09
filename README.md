# 🛡️ Kademeli Bilişim ve Siber Güvenlik Eğitim Platformu

Ortaöğretim ve ön lisans öğrencileri için **%100 çevrimdışı (offline)** çalışan, yerel öğrenme ve sözdizimi (syntax) simülasyon mimarisi.

---

## 👥 Proje Geliştirme Ekibi
* **Mehmet Aydın** — Yazılım Mimarisi, Çekirdek Sınav Motoru ve Gömülü Simülatör
* **Ünal Küçük** — Görsel Programlama & Veri Tabanları Soru Havuzu ve Pedagojik Açıklamalar
* **İsmail Can Şal** — Siber Güvenlik Modülü ve Gizli Syntax Tuzak Senaryoları
* **Yağız Domaç** — Arayüz (UI/UX) Tasarımı, Oyunlaştırma Rozetleri ve Çevrimdışı Testler

---

## 🚀 Temel Özellikler
* **%100 Çevrimdışı Mimari:** Hiçbir sunucu veya internet bağlantısı gerektirmeden yerel JSON/SQLite veri yapısıyla çalışır.
* **Kademeli 3 Zorluk Modu:** Kolay Mod, Orta Mod (**%75 Başarı Barajı Kilitli**) ve Zor Mod (Sözdizimi Tuzak Analizi).
* **Pedagojik "Neden Yanlış" Paneli:** Öğrenci yanlış şıkkı seçtiğinde hatanın nedenini sade bir dille anında açıklar.
* **Gömülü Kod Simülatörü:** C# kod bloklarındaki gizli syntax hatalarını yerel olarak denetler.
* **USB / Klasörden Paket Yükleme:** İnternet olmayan ortamlarda dışarıdan `.json` soru paketlerini sisteme dahil eder.
* **Yerel Oyunlaştırma & Hata Karnesi:** İpucu jetonları, rozetler ve masaüstüne indirilebilir `.txt` karne raporu sunar.

---

## 📌 Sürüm Günlüğü ve Yama Notları (Changelog)

### [v0.3.0 - v0.3.1] — 2026-11-09

**Eklenenler (v0.3.0)**
- Sol sidebar navigasyonu ve 3 ekranlı yapı (Ana Menü, Sınav, Simülatör)
- VS Code temalı kod bloğu ve "Neden Yanlış" açıklama paneli
- İpucu jetonu sistemi ve %75 başarı barajı mantığı
- Gömülü kod simülatörü ve çevrimdışı hata karnesi (.txt)

**Düzeltilenler (v0.3.1)**
- Ekran geçişlerinde oluşan bellek sızıntısı giderildi (Dispose ve font önbelleği)
- Başarı yüzdesi artık çözülen disipline yazılıyor
- Simülatörde sınırsız puan kazanma açığı kapatıldı
- Kod bloğu satır numaraları dinamik hale getirildi
- Bozuk USB soru paketlerinin uygulamayı çökertmesi engellendi
- Simülatör kontrolü Regex ile güçlendirildi
  
### [v0.2.0] — 2026-11-09
* **[Eklendi]** Gömülü Kod Simülatörü (Zor Mod canlı C# syntax tuzağı analiz ekranı) eklendi.
* **[Eklendi]** USB / Klasörden dışarıdan `.json` soru paketi yükleme (`DisaridanPaketYukle`) özelliği devreye alındı.
* **[Eklendi]** Yerel oyunlaştırma rozetleri (*Hata Avcısı, Kararlı Geliştirici, SQL Ustası, Siber Muhafız*) ve İpucu Jetonu (2 yanlış şıkkı eleme) sistemi eklendi.
* **[Eklendi]** Masaüstüne tek tıkla indirilebilir Çevrimdışı Hata Karnesi (`.txt`) çıktı modülü eklendi.
* **[Geliştirildi]** Modül kartlarına dinamik görsel ilerleme çubukları (Progress Bar) ve %75 baraj kilidi entegre edildi.

### [v0.1.1] — 2026-11-09
* **[Eklendi]** `Models`, `Services` ve `Data` katmanlı mimarisi kuruldu.
* **[Eklendi]** Koyu tema ana kontrol paneli (Dashboard) ve "Neden Yanlış Açıklaması" sınav ekranı oluşturuldu.

### [v0.1.0] — 2026-11-09
* **[Eklendi]** Windows Forms (.NET 8.0) proje iskeleti ve GitHub depo bağlantısı oluşturuldu.
