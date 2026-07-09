using System;
using System.Collections.Generic;

namespace Restoran.Data.Entities;

public partial class Iade
{
    public int IadeId { get; set; }

    public DateTime? IadeTarihi { get; set; }

    public string IadeSebebi { get; set; } = null!;

    public string? IadeDurumu { get; set; }

    public decimal IadeTutari { get; set; }

    public int? SiparisDetayId { get; set; }

    public int? UrunId { get; set; }

    public int? PersonelId { get; set; }

    public virtual Personel? Personel { get; set; }

    public virtual SiparisDetay? SiparisDetay { get; set; }

    public virtual Urunler? Urun { get; set; }
}
