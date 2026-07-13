namespace Restoran.API.Dtos;

public class UyeEkleDto
{
    public string UyeAdi { get; set; } = null!;
    public string UyeSoyadi { get; set; } = null!;
    public string? UyeTelefon { get; set; }
    public string UyeEmail { get; set; } = null!;
    public string UyeSifre { get; set; } = null!;
    public string? Cinsiyet { get; set; }
}