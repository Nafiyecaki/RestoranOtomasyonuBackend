namespace Restoran.API.Dtos;

public class OdemeEkleDto
{
    public int? SiparisId { get; set; }
    public int? MasaId { get; set; }  // ✅ YENİ: MasaId eklendi
    public string OdemeTipi { get; set; } = null!;
    public int? PersonelId { get; set; }
    public int? KasaId { get; set; }
}

