using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Firmalytics
{
    public class Sirket
    {
        public string IsletmeAdi { get; set; }
        public string Adres { get; set; }
        public string Telefon { get; set; }
        public string WebSitesi { get; set; }
        public string Puan { get; set; }
        public string YorumSayisi { get; set; }
        public string Kategori { get; set; } // Bu alan şimdilik boş kalabilir, Haritalar'dan çekmek zor.
        public string HaritaLinki { get; set; } // Bunu doğrudan driver.Url ile dolduracağız.

        // YENİ EKLENEN ALANLAR
        public double Enlem { get; set; }
        public double Boylam { get; set; }

        public string Eposta { get; set; }
        public string LinkedIn { get; set; }
        public string Notlar { get; set; }
    }
}
