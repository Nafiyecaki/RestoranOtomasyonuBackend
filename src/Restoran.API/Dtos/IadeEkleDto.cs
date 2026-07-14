namespace Restoran.API.Dtos;

public class IadeEkleDto
{
    public string IadeSebebi { get; set; } = null!;
    public string? IadeDurumu { get; set; } = "BEKLEMEDE"; // Varsayılan: onay bekliyor
    public decimal IadeTutari { get; set; }
    public int? SiparisDetayId { get; set; }
    public int? UrunId { get; set; }
    public int? PersonelId { get; set; }
}