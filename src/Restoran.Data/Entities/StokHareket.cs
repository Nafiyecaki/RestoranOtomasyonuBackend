using System;
using System.Collections.Generic;

namespace Restoran.Data.Entities;

public partial class StokHareket
{
    public int StokHareketId { get; set; }

    public string StokIslemTipi { get; set; } = null!;

    public int StokMiktari { get; set; }

    public DateTime? IsleminTarihSaati { get; set; }

    public string? IsleminAciklamasi { get; set; }

    public int? UrunId { get; set; }

    public int? PersonelId { get; set; }

    public virtual Personel? Personel { get; set; }

    public virtual Urunler? Urun { get; set; }
}
