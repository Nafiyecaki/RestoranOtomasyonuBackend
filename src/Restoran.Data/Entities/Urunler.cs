using System;
using System.Collections.Generic;

namespace Restoran.Data.Entities;

public partial class Urunler
{
    public int UrunId { get; set; }

    public string UrunAdi { get; set; } = null!;

    public decimal Fiyat { get; set; }

    public int? StokMiktari { get; set; }

    public string? Aciklamalar { get; set; }

    public int? KategoriId { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? SilinmeTarihi { get; set; }

    public virtual ICollection<Iade> Iades { get; set; } = new List<Iade>();

    public virtual Kategori? Kategori { get; set; }

    public virtual ICollection<SiparisDetay> SiparisDetays { get; set; } = new List<SiparisDetay>();

    public virtual ICollection<StokHareket> StokHarekets { get; set; } = new List<StokHareket>();

    public virtual ICollection<UrunRecetesi> UrunRecetesis { get; set; } = new List<UrunRecetesi>();
}
