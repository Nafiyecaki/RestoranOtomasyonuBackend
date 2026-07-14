using System.ComponentModel.DataAnnotations;

namespace Restoran.API.Dtos;

public class UrunGuncelleDto
{
    [Required(ErrorMessage = "Ürün adı boş geçilemez.")]
    public string UrunAdi { get; set; } = null!;

    [Range(0, double.MaxValue, ErrorMessage = "Ürün fiyatı 0 veya daha büyük olmalıdır.")]
    public decimal Fiyat { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Stok miktarı negatif olamaz.")]
    public int StokMiktari { get; set; }

    public string? Aciklamalar { get; set; }

    public int? KategoriId { get; set; }
}