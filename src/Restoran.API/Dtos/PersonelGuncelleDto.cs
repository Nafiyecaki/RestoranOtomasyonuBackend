using System;

namespace Restoran.API.Dtos;

public class PersonelGuncelleDto
{
    public string PersonelAdi { get; set; } = null!;
    public string PersonelSoyadi { get; set; } = null!;
    public string KullaniciAdi { get; set; } = null!;
    public string? PersonelTelefon { get; set; }
    public string? Cinsiyet { get; set; }
    public DateOnly? IseBaslamaTarihi { get; set; }
    public decimal? Maas { get; set; }
    public int? RolId { get; set; }

    // Şifre alanı boş bırakılırsa eski şifre aynen korunacak
    public string? PersonelSifre { get; set; }
}