namespace Restoran.API.Dtos;

public class LoginDto
{
    public string KullaniciAdi { get; set; } = null!;
    public string Sifre { get; set; } = null!;
}
public class RegisterDto
{
    public string PersonelAdi { get; set; } = null!;
    public string PersonelSoyadi { get; set; } = null!;
    public string KullaniciAdi { get; set; } = null!;
    public string Sifre { get; set; } = null!;
    public int? RolId { get; set; }   // gönderilmezse varsayılan rol atanır
}

public class RefreshDto
{
    public string RefreshToken { get; set; } = null!;
}

public class SifreDegistirDto
{
    public string EskiSifre { get; set; } = null!;
    public string YeniSifre { get; set; } = null!;
}