using System.ComponentModel.DataAnnotations;

namespace Restoran.API.Dtos;

public class MasaEkleDto
{
    [Required(ErrorMessage = "Masa numarası boş bırakılamaz.")]
    [MaxLength(50, ErrorMessage = "Masa numarası en fazla 50 karakter olabilir.")]
    public string MasaNo { get; set; } = null!;

    [Required(ErrorMessage = "Masa durumu belirtilmelidir (Örn: Boş, Dolu, Rezerve).")]
    public string MasaDurumu { get; set; } = "BOŞ";

    public int? Kapasite { get; set; } = 4; // Varsayılan 4 kişilik
}