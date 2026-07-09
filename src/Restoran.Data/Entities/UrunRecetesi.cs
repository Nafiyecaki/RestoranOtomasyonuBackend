using System;
using System.Collections.Generic;

namespace Restoran.Data.Entities;

public partial class UrunRecetesi
{
    public int ReceteId { get; set; }

    public int UrunId { get; set; }

    public int MalzemeId { get; set; }

    public decimal KullanimMiktari { get; set; }

    public virtual Malzemeler Malzeme { get; set; } = null!;

    public virtual Urunler Urun { get; set; } = null!;
}
