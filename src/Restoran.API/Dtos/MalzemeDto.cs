namespace Restoran.API.Dtos;

public class MalzemeEkleDto
{
    public string MalzemeAdi { get; set; } = null!;
    public decimal StokMiktari { get; set; }
    public string Birim { get; set; } = null!;        // kg / lt / adet / demet
    public decimal? BirimMaliyeti { get; set; }
}

public class MalzemeStokGuncelleDto
{
    public decimal Miktar { get; set; }               // eklenecek (+) veya düşülecek (-) miktar
    public string? Aciklama { get; set; }
}