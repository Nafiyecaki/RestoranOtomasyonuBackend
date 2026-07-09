using System;
using System.Collections.Generic;

namespace Restoran.Data.Entities;

public partial class SiparisDetay
{
    public int SiparisDetayId { get; set; }

    public int Adet { get; set; }

    public decimal BirimFiyat { get; set; }

    public string? DetayNot { get; set; }

    public int SiparisId { get; set; }

    public int UrunId { get; set; }

    public virtual ICollection<Iade> Iades { get; set; } = new List<Iade>();

    public virtual Siparisler Siparis { get; set; } = null!;

    public virtual Urunler Urun { get; set; } = null!;
}
