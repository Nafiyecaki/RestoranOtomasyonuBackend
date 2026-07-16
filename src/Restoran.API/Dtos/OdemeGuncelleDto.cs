namespace Restoran.API.Dtos;

public class OdemeGuncelleDto
{
    public string OdemeTipi { get; set; } = null!;   // NAKIT / KREDI KARTI / ONLINE / KAPIDA NAKIT
    public int? PersonelId { get; set; }
    public int? KasaId { get; set; }
    // SiparisId bilinçli olarak YOK: ödemenin siparişi değiştirilemez.
    // Yanlış siparişe alınan ödeme silinip doğru siparişe yeniden alınır.
}