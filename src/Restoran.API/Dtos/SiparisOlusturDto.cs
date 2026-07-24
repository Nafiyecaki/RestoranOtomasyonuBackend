using System.Collections.Generic;

namespace Restoran.API.Dtos
{
    public class SiparisOlusturDto
    {
        public int? SiparisId { get; set; }
        public int? MasaId { get; set; }
        public int? UyeId { get; set; }
        public int? PersonelId { get; set; }
        public string? SiparisTipi { get; set; }
        public List<SiparisDetayDto> Detaylar { get; set; } = new();
    }

    public class SiparisDetayDto
    {
        public int UrunId { get; set; }
        public int Adet { get; set; } = 1;
        public string? DetayNot { get; set; }
    }
}