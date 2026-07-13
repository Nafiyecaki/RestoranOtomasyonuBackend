using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Restoran.Data;
using Restoran.API.Dtos;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly DbRestoranContext _context;
    private readonly IConfiguration _config;

    public AuthController(DbRestoranContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    // POST /api/Auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var personel = await _context.Personels
            .Include(p => p.Rol)
            .FirstOrDefaultAsync(p => p.KullaniciAdi == dto.KullaniciAdi);

        // NOT: Test verisinde şifreler düz metin ("hash_..." formatında).
        if (personel == null || personel.PersonelSifre != dto.Sifre)
            return Unauthorized("Kullanıcı adı veya şifre hatalı.");

        var token = TokenUret(personel.PersonelId,
                              personel.KullaniciAdi,
                              personel.Rol?.RolAdi ?? "Bilinmiyor");

        return Ok(new
        {
            Token = token,
            PersonelId = personel.PersonelId,
            AdSoyad = personel.PersonelAdi + " " + personel.PersonelSoyadi,
            Rol = personel.Rol?.RolAdi
        });
    }

    private string TokenUret(int personelId, string kullaniciAdi, string rol)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, personelId.ToString()),
            new Claim(ClaimTypes.Name, kullaniciAdi),
            new Claim(ClaimTypes.Role, rol)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(
                double.Parse(_config["Jwt:ExpireMinutes"] ?? "480")),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}