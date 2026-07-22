using System;

namespace Restoran.API.Dtos;

public class RezervasyonEkleDto
{
    public string? MusteriAdi { get; set; }
    public string? MusteriSoyadi { get; set; }
    public string? Telefon { get; set; }
    public int? KisiSayisi { get; set; }
    public string? Aciklama { get; set; }
    public string? RezervasyonTipi { get; set; }

    // Tarih/Saat - Her iki format
    public DateTime? TarihSaat { get; set; }
    public string? Tarih { get; set; }
    public string? Saat { get; set; }

    public int? MasaId { get; set; }
    public int? UyeId { get; set; }
    public string? Durum { get; set; }
}