using System;
using System.Collections.Generic;

namespace Restoran.Data.Entities;

public partial class Roller
{
    public int RolId { get; set; }

    public string RolAdi { get; set; } = null!;

    public bool? RolDurumu { get; set; }

    public virtual ICollection<Personel> Personels { get; set; } = new List<Personel>();
}
