using System;
using System.Collections.Generic;

namespace Restoran.Data.Entities;

public partial class Adres
{
    public int AdresId { get; set; }

    public string AdresTipi { get; set; } = null!;

    public string AcikAdres { get; set; } = null!;

    public bool? TeslimatBolgesindeMi { get; set; }

    public int? UyeId { get; set; }

    public virtual Uyeler? Uye { get; set; }
}
