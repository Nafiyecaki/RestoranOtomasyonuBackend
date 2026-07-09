using System;
using System.Collections.Generic;

namespace Restoran.Data.Entities;

public partial class Odeme
{
    public int OdemeId { get; set; }

    public string OdemeTipi { get; set; } = null!;

    public decimal OdemeTutari { get; set; }

    public DateTime? OdemeTarihi { get; set; }

    public int? PersonelId { get; set; }

    public int? SiparisId { get; set; }

    public int? KasaId { get; set; }

    public virtual Kasa? Kasa { get; set; }

    public virtual Personel? Personel { get; set; }

    public virtual Siparisler? Siparis { get; set; }
}
