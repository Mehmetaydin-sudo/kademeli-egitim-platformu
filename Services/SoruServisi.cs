using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using KademeliEgitimPlatformu.Models;

namespace KademeliEgitimPlatformu.Services
{
    public class SoruServisi
    {
        private readonly string _dosyaYolu;

        public SoruServisi()
        {
            // Uygulamanın çalıştığı klasördeki Data/Sorular.json dosyasını bulur
            _dosyaYolu = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Sorular.json");
        }

        public List<Soru> TumSorulariGetir()
        {
            if (!File.Exists(_dosyaYolu))
                return new List<Soru>();

            string jsonIcerik = File.ReadAllText(_dosyaYolu);
            return JsonSerializer.Deserialize<List<Soru>>(jsonIcerik) ?? new List<Soru>();
        }

        public List<Soru> SorulariFiltrele(DisiplinTuru disiplin, ZorlukSeviyesi? zorluk = null)
        {
            var tumSorular = TumSorulariGetir();
            var filtreli = tumSorular.Where(s => s.Disiplin == disiplin);

            if (zorluk.HasValue)
                filtreli = filtreli.Where(s => s.Zorluk == zorluk.Value);

            return filtreli.ToList();
        }
    }
}
