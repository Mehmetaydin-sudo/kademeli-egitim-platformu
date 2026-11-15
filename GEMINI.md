# Kademeli Eğitim Platformu - Proje ve Mimari Kuralları

## 1. Teknoloji Yığını ve Ortam
- **Dil & Framework:** C# / .NET 8 Windows Forms (WinForms).
- **Veritabanı:** Yerel PostgreSQL 18 (`localhost:5432`, veritabanı adı: `kademeli_egitim`, `Npgsql` kütüphanesi). Veritabanı bağlantı bilgisi `appsettings.json` içinden (`Microsoft.Extensions.Configuration.Json` ile) okunacaktır.
- **Güvenlik:** SHA256 kaldırılacak; parola ve hassas özetleme işlemlerinde `BCrypt.Net-Next` ile tuzlu (salted) parola özetleme kullanılacaktır.
- **Veri Aktarımı:** JSON formatı kaldırılacak; soru/veri içe ve dışa aktarım işlemlerinde `ClosedXML` kütüphanesi ile Excel (`.xlsx`) ve `.csv` formatları kullanılacaktır.
- **Kademeli Geçiş:** Katı engel yerine `Kolay -> Orta` ve `Orta -> Zor` geçişlerinde **"Seviye Geçiş Ara Sınavı"** uygulanacaktır.
- **Ekip & Git Akışı:** 4 kişilik ekip çalışması. Doğrudan `main` dalına push/merge yapma; geliştirmeleri mevcut geliştirme dalı (`gelistirme/v0.7.0` vb.) üzerinden yürüt.

## 2. WinForms Tasarımcı (`.Designer.cs`) Koruma Kuralları (KRİTİK)
- **Dokunulmazlık Kuralı:** `*.Designer.cs` dosyalarını kullanıcı açıkça izin vermedikçe ASLA düzenleme, yeniden biçimlendirme veya üzerine yazma.
- **Arayüz Değişiklikleri:** Yeni buton, panel veya bileşen eklenecekse bunu `.Designer.cs` dosyasını bozmadan yalnızca ilgili formun `.cs` (code-behind) dosyasında dinamik olarak oluştur veya kullanıcıya Visual Studio Designer üzerinde yapması gereken adımları tarif et.
- **Çözünürlük ve Düzen:** Tüm formlar 1080p (1920x1080) ekran standartlarına ve responsive (Anchor/Dock) yapısına uygun kalmalıdır.

## 3. PostgreSQL Veritabanı ve Servis Mimarisi
- **Katmanlı Yapı:** Form dosyalarının (`.cs`) içine doğrudan ham SQL sorguları yazma. Tüm veritabanı işlemlerini `SoruServisi`, `IlerlemeServisi` ve ilgili servis sınıfları üzerinden yönet.
- **Yapılandırma:** Bağlantı dizesini kod içine gömme; `appsettings.json` dosyasından oku.
- **Güvenlik ve Performans:** Tüm PostgreSQL (`Npgsql`) sorgularında SQL Injection'a karşı mutlaka parametreli sorgular (`@parametre`) kullan.
- **Bağlantı Yönetimi:** Bağlantı havuzu sızıntılarını önlemek için tüm `NpgsqlConnection`, `NpgsqlCommand` ve `NpgsqlTransaction` nesnelerini `using` blokları içinde aç ve iş bitiminde anında kapat.
- **Veri Bütünlüğü:** Excel (`.xlsx`) ve `.csv` içe aktarma (import) işlemlerinde mevcut ilerleme verilerini ezmeden işlem yap ve hatalara karşı `Transaction` kullan.

## 4. Kodlama Standartları ve Derleme Doğrulaması
- Değişiklik yaptıktan sonra terminal üzerinden `dotnet build` komutunu çalıştırarak derleme hatası (error) olmadığını doğrula.
- Kullanıcıya açıklama yaparken ve kod içi yorum satırlarında Türkçe kullan.
- Değişiklik tamamlandığında `CHANGELOG` / `README.md` güncellemelerini hatırlat.
