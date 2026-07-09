using System;
using System.Collections.Generic;

namespace Restoran.Data.Entities;

public partial class Uyeler
{
    public int UyeId { get; set; }

    public string UyeAdi { get; set; } = null!;

    public string UyeSoyadi { get; set; } = null!;

    public string? UyeTelefon { get; set; }

    public string UyeEmail { get; set; } = null!;

    public string UyeSifre { get; set; } = null!;

    public string? Cinsiyet { get; set; }

    public DateTime? KayitTarihi { get; set; }

    public virtual ICollection<Adre> Adres { get; set; } = new List<Adre>();

    public virtual ICollection<Siparisler> Siparislers { get; set; } = new List<Siparisler>();
}
