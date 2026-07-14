using System.ComponentModel.DataAnnotations;

namespace Restoran.API.Dtos;

public class UrunEkleDto
{
    [Required(ErrorMessage = "Ürün adı boş geçilemez.")]
    public string UrunAdi { get; set; } = null!;

    [Range(0, 99999999, ErrorMessage = "Ürün fiyatı 0 ile 99.999.999 arasında olmalıdır.")]
    public decimal Fiyat { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Stok miktarı negatif olamaz.")]
    public int StokMiktari { get; set; }

    public string? Aciklamalar { get; set; }

    public int? KategoriId { get; set; }
}