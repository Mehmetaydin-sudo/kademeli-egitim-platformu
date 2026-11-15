using System;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>
    /// Parola ve hassas metin özetleme/doğrulama işlemlerini BCrypt.Net-Next ile yürütür.
    /// SHA256 ve eski şifreli .dat paket yapıları mimari kurallar gereği kaldırılmıştır.
    /// </summary>
    public static class SoruKoruyucu
    {
        private const int VarsayilanMaliyetFaktoru = 11;

        /// <summary>
        /// Metni veya parolayı BCrypt.Net-Next ile tuzlu (salted) olarak özetler.
        /// </summary>
        public static string Ozetle(string duzMetin)
        {
            if (string.IsNullOrWhiteSpace(duzMetin))
                throw new ArgumentException("Özetlenecek metin boş olamaz.", nameof(duzMetin));

            return BCrypt.Net.BCrypt.HashPassword(duzMetin.Trim(), workFactor: VarsayilanMaliyetFaktoru);
        }

        /// <summary>
        /// Düz metni BCrypt tuzlu özetiyle doğrular.
        /// </summary>
        public static bool Dogrula(string duzMetin, string bcryptOzeti)
        {
            if (string.IsNullOrEmpty(duzMetin) || string.IsNullOrWhiteSpace(bcryptOzeti))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(duzMetin.Trim(), bcryptOzeti);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                return false;
            }
        }

        /// <summary>
        /// Verilen metnin geçerli bir BCrypt özeti ($2a$, $2b$, $2y$) olup olmadığını kontrol eder.
        /// </summary>
        public static bool BcryptOzetiMi(string? metin) =>
            !string.IsNullOrWhiteSpace(metin) &&
            metin.Length >= 59 &&
            (metin.StartsWith("$2a$", StringComparison.Ordinal) ||
             metin.StartsWith("$2b$", StringComparison.Ordinal) ||
             metin.StartsWith("$2y$", StringComparison.Ordinal));
    }
}
