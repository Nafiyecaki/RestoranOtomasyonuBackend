// Dtos/MasaDurumGuncelleDto.cs
namespace Restoran.API.Dtos;

public class MasaDurumGuncelleDto
{
    public string MasaDurumu { get; set; } = null!;
    public string? RezervasyonSaati { get; set; } // "19:00" gibi, opsiyonel
}