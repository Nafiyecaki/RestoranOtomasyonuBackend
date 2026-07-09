using System;
using System.Collections.Generic;

namespace Restoran.Data.Entities;

public partial class Siparisler
{
    public int SiparisId { get; set; }

    public string SiparisDurumu { get; set; } = null!;

    public string SiparisTipi { get; set; } = null!;

    public decimal? ToplamTutar { get; set; }

    public DateTime? SiparisTarihi { get; set; }

    public int? UyeId { get; set; }

    public int? MasaId { get; set; }

    public int? PersonelId { get; set; }

    public virtual Masa? Masa { get; set; }

    public virtual ICollection<Odeme> Odemes { get; set; } = new List<Odeme>();

    public virtual Personel? Personel { get; set; }

    public virtual ICollection<SiparisDetay> SiparisDetays { get; set; } = new List<SiparisDetay>();

    public virtual Uyeler? Uye { get; set; }
}
