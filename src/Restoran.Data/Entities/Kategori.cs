using System;
using System.Collections.Generic;

namespace Restoran.Data.Entities;

public partial class Kategori
{
    public int KategoriId { get; set; }

    public string KategoriAdi { get; set; } = null!;

    public virtual ICollection<Urunler> Urunlers { get; set; } = new List<Urunler>();
}
