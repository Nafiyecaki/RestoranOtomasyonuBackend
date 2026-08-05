using System;
using System.Collections.Generic;

namespace Restoran.Data.Entities;

public partial class Rezervasyon
{
    public int RezervasyonId { get; set; }

    public string? MusteriAdi { get; set; }

    public string? MusteriSoyadi { get; set; }

    public string Telefon { get; set; } = null!;

    public int KisiSayisi { get; set; }

    public DateTime TarihSaat { get; set; }

    public string? Durum { get; set; }

    public DateTime? OlusturulmaTarihi { get; set; }

    public string? Aciklama { get; set; }

    public int? MasaId { get; set; }

    public string? RezervasyonTipi { get; set; }
    public int? UyeId { get; set; }

    public virtual Uyeler? Uye { get; set; }
    public virtual Masa? Masa { get; set; }
}