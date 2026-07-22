using System;

namespace Restoran.API.Dtos
{
    public class MalzemeTalepDto
    {
        public int TalepId { get; set; }
        public int MalzemeId { get; set; }
        public string MalzemeAdi { get; set; }
        public int Miktar { get; set; }
        public string Birim { get; set; }
        public string TalepEden { get; set; }
        public int? PersonelId { get; set; }
        public string Durum { get; set; }
        public string Aciklama { get; set; }
        public DateTime TalepTarihi { get; set; }
        public DateTime? CevaplamaTarihi { get; set; }
        public string Cevaplayan { get; set; }
    }

    public class MalzemeTalepOlusturDto
    {
        public int MalzemeId { get; set; }
        public int Miktar { get; set; }
        public string Birim { get; set; } = "adet";
        public string Aciklama { get; set; }
        public int? PersonelId { get; set; }
    }

    public class MalzemeTalepCevaplaDto
    {
        public int TalepId { get; set; }
        public string Durum { get; set; }
        public string Cevaplayan { get; set; }
    }
}