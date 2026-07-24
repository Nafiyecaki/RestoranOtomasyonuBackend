using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restoran.Data.Entities
{
    public class vw_PersonelIzin_Gecmisi
    {
        public int IzinID { get; set; }
        public int PersonelID { get; set; }
        public string? PersonelAdSoyad { get; set; }
        public string? PersonelTelefon { get; set; }
        public string? PersonelRol { get; set; }
        public DateTime IzinBaslangic { get; set; }
        public string? IzinBaslangicFormatli { get; set; }
        public DateTime IzinBitis { get; set; }
        public string? IzinBitisFormatli { get; set; }
        public int IzinGunSayisi { get; set; }
        public string? IzinAciklamasi { get; set; }
        public string? BugunluziniMi { get; set; }
    }
}
