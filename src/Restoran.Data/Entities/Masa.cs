using System;
using System.Collections.Generic;

namespace Restoran.Data.Entities;

public partial class Masa
{
    public int MasaId { get; set; }

    public string? MasaNo { get; set; }

    public string? MasaDurumu { get; set; }

    public virtual ICollection<Rezervasyon> Rezervasyons { get; set; } = new List<Rezervasyon>();

    public virtual ICollection<Siparisler> Siparislers { get; set; } = new List<Siparisler>();
}
