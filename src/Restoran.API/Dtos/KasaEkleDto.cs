
namespace Restoran.API.Dtos;

public class KasaEkleDto
{
    public int? PersonelId { get; set; }

    // Varsayılan olarak dışarıdan bir şey gönderilmezse "Açık" kabul edilecek
    public string KasaDurumu { get; set; } = "Açık";
}