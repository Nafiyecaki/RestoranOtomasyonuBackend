using System.Collections.Generic;

namespace Restoran.API.Dtos;

public class SiparisGuncelleDto
{
    public string? SiparisTipi { get; set; } // SALON, GEL-AL, PAKET
    public int? UyeId { get; set; }
    public int? MasaId { get; set; }
    public int? PersonelId { get; set; }

    // Siparişin güncellenmiş detay listesi (Ürünler ve Adetleri)
    public List<SiparisDetayGuncelleDto> Detaylar { get; set; } = new();
}

public class SiparisDetayGuncelleDto
{
    public int UrunId { get; set; }
    public int Adet { get; set; }
    public string? DetayNot { get; set; }
}