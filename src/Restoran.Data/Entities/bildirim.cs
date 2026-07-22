using System;

namespace Restoran.Data.Entities;

public class Bildirim
{
    public int BildirimId { get; set; }
    public int? KullaniciId { get; set; }
    public string? Baslik { get; set; }
    public string? Mesaj { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public bool OkunduMu { get; set; }
    public string? Tip { get; set; }

    public virtual Uyeler? Kullanici { get; set; }
}