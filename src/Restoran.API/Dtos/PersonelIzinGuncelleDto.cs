using System;

namespace Restoran.API.Dtos;

public class PersonelIzinGuncelleDto
{
    public DateOnly IzinBaslangic { get; set; }
    public DateOnly IzinBitis { get; set; }
    public string? IzinDurumu { get; set; } // Beklemede, Onaylandı, Reddedildi vb.
    public string? IzinAciklamasi { get; set; }
    public int? PersonelId { get; set; }
}