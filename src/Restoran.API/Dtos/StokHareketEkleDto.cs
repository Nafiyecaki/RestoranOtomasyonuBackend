namespace Restoran.API.Dtos;

public class StokHareketEkleDto
{
    public int UrunId { get; set; }
    public string StokIslemTipi { get; set; } = null!;   // GIRIS / CIKIS / FIRE
    public int StokMiktari { get; set; }
    public string? IsleminAciklamasi { get; set; }
    public int? PersonelId { get; set; }
}