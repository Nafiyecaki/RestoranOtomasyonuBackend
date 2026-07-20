using System;
using System.Collections.Generic;

namespace Restoran.Data.Entities;

public partial class Uyeler
{

    public int UyeId { get; set; }
    public string UyeAdi { get; set; }
    public string UyeSoyadi { get; set; }
    public string UyeTelefon { get; set; }
    public string UyeEmail { get; set; }
    public string UyeSifre { get; set; }
    public string Cinsiyet { get; set; }
    public DateTime KayitTarihi { get; set; }

    public bool IsActive { get; set; } = true;

  


    public virtual ICollection<Adres> Adres { get; set; } = new List<Adres>();

    public virtual ICollection<Siparisler> Siparislers { get; set; } = new List<Siparisler>();
}
