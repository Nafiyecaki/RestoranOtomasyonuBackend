using System;
using System.Collections.Generic;

namespace Restoran.Data.Entities;

public partial class Malzemeler
{
    public int MalzemeId { get; set; }

    public string MalzemeAdi { get; set; } = null!;

    public decimal StokMiktari { get; set; }

    public string Birim { get; set; } = null!;

    public decimal? BirimMaliyeti { get; set; }

    public virtual ICollection<UrunRecetesi> UrunRecetesis { get; set; } = new List<UrunRecetesi>();
}
