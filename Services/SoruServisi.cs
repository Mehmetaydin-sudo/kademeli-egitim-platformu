using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using KademeliEgitimPlatformu.Models;

namespace KademeliEgitimPlatformu.Services
{
    public class SoruServisi
    {
        private string _dosyaYolu;

        public SoruServisi()
        {
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

        // Slayt 12 & 13: İnternet olmadan USB veya klasörden yeni soru paketi (.json) yükleme
        public int DisaridanPaketYukle(string secilenDosyaYolu)
        {
            string yeniJson = File.ReadAllText(secilenDosyaYolu);
            var yeniSorular = JsonSerializer.Deserialize<List<Soru>>(yeniJson) ?? new List<Soru>();

            if (yeniSorular.Count == 0)
                return 0;

            var mevcutSorular = TumSorulariGetir();
            int eklenenSayisi = 0;

            foreach (var yeniSoru in yeniSorular)
            {
                if (!mevcutSorular.Any(s => s.Baslik == yeniSoru.Baslik))
                {
                    yeniSoru.Id = mevcutSorular.Count + 1;
                    mevcutSorular.Add(yeniSoru);
                    eklenenSayisi++;
                }
            }

            // Güncel havuzu yerel veri dosyasına kaydet
            Directory.CreateDirectory(Path.GetDirectoryName(_dosyaYolu)!);
            string kaydedilecekJson = JsonSerializer.Serialize(mevcutSorular, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_dosyaYolu, kaydedilecekJson);

            return eklenenSayisi;
        }
    }
}