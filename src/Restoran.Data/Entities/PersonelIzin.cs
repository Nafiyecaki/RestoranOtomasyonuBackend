using System;
using System.Collections.Generic;

namespace Restoran.Data.Entities;

public partial class PersonelIzin
{
    public int IzinId { get; set; }

    public DateOnly IzinBaslangic { get; set; }

    public DateOnly IzinBitis { get; set; }

    public string? IzinDurumu { get; set; }

    public string? IzinAciklamasi { get; set; }

    public int? PersonelId { get; set; }

    public virtual Personel? Personel { get; set; }
}
