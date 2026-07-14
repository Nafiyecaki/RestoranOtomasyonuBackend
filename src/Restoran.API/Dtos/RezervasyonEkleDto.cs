using System;

namespace Restoran.API.Dtos;

public class RezervasyonEkleDto
{
    public string MusteriAdi { get; set; } = null!;
    public string MusteriSoyadi { get; set; } = null!;

    public string Telefon { get; set; } = null!;
    public int KisiSayisi { get; set; }
    public DateTime TarihSaat { get; set; }
    public string? Durum { get; set; } // Beklemede, Onaylandı, İptal Edildi
    public string? Aciklama { get; set; }
    public int? MasaId { get; set; }
    public int? UyeId { get; set; } // üye rezervasyonuysa dolu, telefonla gelen misafirse boş
    public string? RezervasyonTipi { get; set; } // VIP, Standart, Doğum Günü vb.
}