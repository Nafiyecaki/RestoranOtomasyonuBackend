using System;

namespace Restoran.API.Dtos;

public class PersonelIzinEkleDto
{
    public DateOnly IzinBaslangic { get; set; }
    public DateOnly IzinBitis { get; set; }

    // Yeni eklenen izinler varsayılan olarak "Beklemede" (Onay bekliyor) düşsün
    public string? IzinDurumu { get; set; } = "BEKLEMEDE";
    public string? IzinAciklamasi { get; set; }
    public int? PersonelId { get; set; }
}