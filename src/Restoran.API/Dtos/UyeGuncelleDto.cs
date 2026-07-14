using System;
using System;
using System.ComponentModel.DataAnnotations;

namespace Restoran.API.Dtos;

public class UyeGuncelleDto
{
    [Required(ErrorMessage = "Üye adı boş bırakılamaz.")]
    public string UyeAdi { get; set; } = null!;

    [Required(ErrorMessage = "Üye soyadı boş bırakılamaz.")]
    public string UyeSoyadi { get; set; } = null!;

    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçersiz e-posta formatı.")]
    public string UyeEmail { get; set; } = null!;

    public string? UyeTelefon { get; set; }
    public string? Cinsiyet { get; set; }

    // Şifre boş gelirse eski şifre veritabanında korunur, dolu gelirse ezilir
    public string? UyeSifre { get; set; }
}