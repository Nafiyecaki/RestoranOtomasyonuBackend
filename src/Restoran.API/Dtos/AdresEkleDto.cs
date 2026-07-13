namespace Restoran.API.Dtos;

public class AdresEkleDto
{
    public string AdresTipi { get; set; } = null!; // Ev, İş vb.
    public string AcikAdres { get; set; } = null!;
    public bool? TeslimatBolgesindeMi { get; set; } = true; // Boş gelirse varsayılan olarak bölge içi kabul edilsin
    public int? UyeId { get; set; }
}