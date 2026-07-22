namespace Restoran.API.Dtos;

public class KuryeDto
{
    public int PersonelId { get; set; }
    public string AdSoyad { get; set; } = null!;
    public string? Telefon { get; set; }
    public bool IsActive { get; set; }
}

public class KuryeSiparisDto
{
    public int SiparisId { get; set; }
    public string SiparisDurumu { get; set; } = null!;
    public decimal? ToplamTutar { get; set; }
    public DateTime? SiparisTarihi { get; set; }
    public string MusteriAdSoyad { get; set; } = null!;
    public string? MusteriTelefon { get; set; }
    public string? AcikAdres { get; set; }
}

public class KuryeAtaDto
{
    public int SiparisId { get; set; }
    public int PersonelId { get; set; }
}


public class KapidaOdemeDto
{
    public int KuryeId { get; set; }
    public string OdemeTipi { get; set; } = "NAKIT";

}