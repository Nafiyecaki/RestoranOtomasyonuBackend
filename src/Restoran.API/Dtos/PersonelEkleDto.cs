// Restoran.API/Dtos/PersonelEkleDto.cs
using System;

namespace Restoran.API.Dtos;

public class PersonelEkleDto
{
    public string PersonelAdi { get; set; }
    public string PersonelSoyadi { get; set; }
    public string KullaniciAdi { get; set; }
    public string PersonelSifre { get; set; }
    public string PersonelTelefon { get; set; }
    public string Cinsiyet { get; set; }
    public DateOnly? IseBaslamaTarihi { get; set; }
    public decimal? Maas { get; set; }
    public int? RolId { get; set; }

    // 🆕 Vardiya Alanları
    public string? VardiyaBaslangic { get; set; }
    public string? VardiyaBitis { get; set; }
    public string? CalismaGunleri { get; set; }
}