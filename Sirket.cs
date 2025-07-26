using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Firmalytics
{
    public class Sirket
    {
        public string IsletmeAdi { get; set; } = "Bulunamadı";
        public string Adres { get; set; } = "Bulunamadı";
        public string Telefon { get; set; } = "Bulunamadı";
        public string WebSitesi { get; set; } = "Bulunamadı";
        public string Puan { get; set; } = "Bulunamadı";
        public string YorumSayisi { get; set; } = "Bulunamadı";
        public string HaritaLinki { get; set; } = "Bulunamadı";
        public double Enlem { get; set; }
        public double Boylam { get; set; }
        public string Eposta { get; set; } = "Aranmadı"; // Yeni eklendi
        public string LinkedIn { get; set; } = "Aranmadı"; // Yeni eklendi
        public string Notlar { get; set; } // Yeni eklendi
    }
}
