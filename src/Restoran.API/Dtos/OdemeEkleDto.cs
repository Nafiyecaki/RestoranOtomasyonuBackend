namespace Restoran.API.Dtos;

public class OdemeEkleDto
{
    public int SiparisId { get; set; }
    public string OdemeTipi { get; set; } = null!;   // NAKIT / KREDI KARTI / ONLINE / KAPIDA NAKIT
    public int? PersonelId { get; set; }
    public int? KasaId { get; set; }
}