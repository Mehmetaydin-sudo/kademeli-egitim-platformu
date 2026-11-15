using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KademeliEgitimPlatformu.Models
{
    public enum ZorlukSeviyesi
    {
        Kolay = 0,
        Orta = 1,
        Zor = 2
    }

    public enum DisiplinTuru
    {
        GorselProgramlama = 0,
        VeriTabanlari = 1,
        SiberGuvenlik = 2
    }

    public class Soru
    {
        public int Id { get; set; }
        public DisiplinTuru Disiplin { get; set; }
        public ZorlukSeviyesi Zorluk { get; set; }

        /// <summary>
        /// İsteğe bağlı etiket. "AraSinav" olan sorular modül ara sınavlarında kullanılır;
        /// boş bırakılan sorular normal modül/karma sınav havuzuna girer.
        /// </summary>
        public string? Etiket { get; set; }

        public string Baslik { get; set; } = string.Empty;
        public string? KodBlogu { get; set; }
        public List<string> Secenekler { get; set; } = new();
        public int DogruSecenekIndex { get; set; }
        public Dictionary<int, string> YanlisCevapAciklamalari { get; set; } = new();
    }
}
