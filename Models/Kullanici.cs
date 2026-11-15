using System;

namespace KademeliEgitimPlatformu.Models
{
    /// <summary>
    /// PostgreSQL kullanicilar tablosundaki kullanıcı kaydını temsil eder.
    /// Parolalar düz metin veya SHA256 olarak değil, BCrypt.Net-Next tuzlu özeti olarak tutulur.
    /// </summary>
    public class Kullanici
    {
        public int Id { get; set; }
        public string KullaniciAdi { get; set; } = string.Empty;
        public string AdSoyad { get; set; } = string.Empty;
        public string SifreHash { get; set; } = string.Empty;
        public string Rol { get; set; } = "Ogrenci";
        public bool AktifMi { get; set; } = true;
        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
        public DateTime? SonGirisTarihi { get; set; }
    }
}
