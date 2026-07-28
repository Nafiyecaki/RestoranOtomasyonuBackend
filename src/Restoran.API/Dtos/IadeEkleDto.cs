using System;

namespace Restoran.API.Dtos
{
    public class IadeEkleDto
    {
        // ✅ EKSİK: SiparisId property'si eklendi
        public int? SiparisId { get; set; }

        public string IadeSebebi { get; set; } = null!;
        public string? IadeDurumu { get; set; }
        public decimal IadeTutari { get; set; }
        public int? SiparisDetayId { get; set; }
        public int? UrunId { get; set; }
        public int? PersonelId { get; set; }
    }
}