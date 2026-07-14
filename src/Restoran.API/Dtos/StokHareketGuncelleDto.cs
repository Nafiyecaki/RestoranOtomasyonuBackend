using System.ComponentModel.DataAnnotations;

namespace Restoran.API.Dtos;

public class StokHareketGuncelleDto
{
    [Required(ErrorMessage = "Ürün seçimi zorunludur.")]
    public int UrunId { get; set; }

    [Required(ErrorMessage = "İşlem tipi (GIRIS, CIKIS, FIRE) boş geçilemez.")]
    public string StokIslemTipi { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = "Stok miktarı en az 1 olmalıdır.")]
    public int StokMiktari { get; set; }

    public string? IsleminAciklamasi { get; set; }

    public int? PersonelId { get; set; }
}