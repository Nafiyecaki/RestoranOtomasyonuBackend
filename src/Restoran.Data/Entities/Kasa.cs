using System;
using System.Collections.Generic;

namespace Restoran.Data.Entities;

public partial class Kasa
{
    public int KasaId { get; set; }

    public decimal? AcilisBakiyesi { get; set; }
    public decimal? KapanisBakiyesi { get; set; }

    public DateTime AcilisTarihi { get; set; }

    public DateTime? KapanisTarihi { get; set; }

    public string KasaDurumu { get; set; } = null!;

    public int? PersonelId { get; set; }

    public virtual ICollection<Odeme> Odemes { get; set; } = new List<Odeme>();

    public virtual Personel? Personel { get; set; }
}
