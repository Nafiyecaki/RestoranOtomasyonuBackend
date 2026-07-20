using System;
using System.ComponentModel.DataAnnotations;

namespace Restoran.API.Dtos;

public class UyeEkleDto
{
    [Required(ErrorMessage = "Üye adı boş bırakılamaz.")]

    public string UyeAdi { get; set; } = null!;
    public string UyeSoyadi { get; set; } = null!;

    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçersiz e-posta formatı.")]
    public string UyeEmail { get; set; } = null!;

    [Required(ErrorMessage = "Şifre alanı zorunludur.")]
    public string UyeSifre { get; set; } = null!;

    public string? UyeTelefon { get; set; }
    public string? Cinsiyet { get; set; }

    public string AdresTipi { get; set; }
    public string AcikAdres { get; set; }
    public bool? TeslimatBolgesindeMi { get; set; }
}