using System;
using System.Collections.Generic;

namespace Restoran.Data.Entities;

public partial class Personel
{
    public int PersonelId { get; set; }

    public string PersonelAdi { get; set; } = null!;

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenBitis { get; set; }

    public string PersonelSoyadi { get; set; } = null!;

    public string KullaniciAdi { get; set; } = null!;

    public string PersonelSifre { get; set; } = null!;

    public string? PersonelTelefon { get; set; }

    public string? Cinsiyet { get; set; }

    public DateOnly? IseBaslamaTarihi { get; set; }

    public decimal? Maas { get; set; }

    public int? RolId { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? SilinmeTarihi { get; set; }


    public virtual ICollection<Iade> Iades { get; set; } = new List<Iade>();

    public virtual ICollection<Kasa> Kasas { get; set; } = new List<Kasa>();

    public virtual ICollection<Odeme> Odemes { get; set; } = new List<Odeme>();
   
    public virtual ICollection<PersonelIzin> PersonelIzins { get; set; } = new List<PersonelIzin>();

    public virtual Roller? Rol { get; set; }

    public virtual ICollection<Siparisler> Siparislers { get; set; } = new List<Siparisler>();

    public virtual ICollection<StokHareket> StokHarekets { get; set; } = new List<StokHareket>();
}
