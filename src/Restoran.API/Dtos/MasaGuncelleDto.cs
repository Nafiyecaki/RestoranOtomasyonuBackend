using System.ComponentModel.DataAnnotations;

namespace Restoran.API.Dtos;

public class MasaGuncelleDto
{
    [Required(ErrorMessage = "Masa numarası boş bırakılamaz.")]
    [MaxLength(50, ErrorMessage = "Masa numarası en fazla 50 karakter olabilir.")]
    public string MasaNo { get; set; } = null!;

    [Required(ErrorMessage = "Masa durumu boş bırakılamaz.")]
    public string MasaDurumu { get; set; } = null!;
}