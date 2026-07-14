using System;

namespace Restoran.API.Dtos;

public class RezervasyonGuncelleDto
{
    public string MusteriAdi { get; set; } = null!;
    public string MusteriSoyadi { get; set; } = null!;
    public string Telefon { get; set; } = null!;
    public int KisiSayisi { get; set; }
    public DateTime TarihSaat { get; set; }
    public string? Durum { get; set; }
    public string? Aciklama { get; set; }
    public int? MasaId { get; set; }
    public string? RezervasyonTipi { get; set; }
}