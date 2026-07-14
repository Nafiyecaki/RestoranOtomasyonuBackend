using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Restoran.Data;
using Restoran.Data.Entities;
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

        // Refresh token üret ve kaydet (oturum yenileme için)
        var refreshToken = RefreshTokenUret();
        personel.RefreshToken = refreshToken;
        personel.RefreshTokenBitis = DateTime.Now.AddDays(7);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Token = token,
            RefreshToken = refreshToken,
            PersonelId = personel.PersonelId,
            AdSoyad = personel.PersonelAdi + " " + personel.PersonelSoyadi,
            Rol = personel.Rol?.RolAdi
        });
    }

    // POST /api/Auth/register -> yeni kullanıcı kaydı
    [HttpPost("register")]
    [AllowAnonymous]  
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (dto == null) return BadRequest();

        if (string.IsNullOrWhiteSpace(dto.KullaniciAdi) || string.IsNullOrWhiteSpace(dto.Sifre))
            return BadRequest(new { Mesaj = "Kullanıcı adı ve şifre boş olamaz." });

        var kullaniciAdiAlinmis = await _context.Personels
            .AnyAsync(p => p.KullaniciAdi == dto.KullaniciAdi);
        if (kullaniciAdiAlinmis)
            return Conflict(new { Mesaj = "Bu kullanıcı adı zaten kullanılıyor." });

        // Rol gönderilmediyse varsayılan rol atanır
        var rolId = dto.RolId ?? 2; // 2 = varsayılan rol, DB'ndeki gerçek ID'ye göre ayarla

        var rolVarMi = await _context.Rollers
            .AnyAsync(r => r.RolId == rolId && r.RolDurumu == true);
        if (!rolVarMi)
            return NotFound(new { Mesaj = "Belirtilen rol bulunamadı veya pasif durumda." });

        var personel = new Personel
        {
            PersonelAdi = dto.PersonelAdi,
            PersonelSoyadi = dto.PersonelSoyadi,
            KullaniciAdi = dto.KullaniciAdi,
            PersonelSifre = dto.Sifre, // NOT: sunumdan önce hash'e geçirilecek
            RolId = rolId
        };

        _context.Personels.Add(personel);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Kayıt başarılı.",
            personel.PersonelId,
            personel.KullaniciAdi
        });
    }

    // POST /api/Auth/refresh -> oturum yenileme
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshDto dto)
    {
        if (string.IsNullOrEmpty(dto?.RefreshToken)) return BadRequest();

        var personel = await _context.Personels
            .Include(p => p.Rol)
            .FirstOrDefaultAsync(p => p.RefreshToken == dto.RefreshToken);

        if (personel == null || personel.RefreshTokenBitis < DateTime.Now)
            return Unauthorized(new { Mesaj = "Refresh token geçersiz veya süresi dolmuş. Tekrar giriş yapın." });

        var yeniToken = TokenUret(personel.PersonelId,
                                  personel.KullaniciAdi,
                                  personel.Rol?.RolAdi ?? "Bilinmiyor");

        // Token rotasyonu: eskisi iptal, yenisi verilir
        var yeniRefreshToken = RefreshTokenUret();
        personel.RefreshToken = yeniRefreshToken;
        personel.RefreshTokenBitis = DateTime.Now.AddDays(7);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Token = yeniToken,
            RefreshToken = yeniRefreshToken
        });
    }

    // POST /api/Auth/sifre-degistir -> giriş yapmış kullanıcı kendi şifresini değiştirir
    [HttpPost("sifre-degistir")]
    [Authorize]
    public async Task<IActionResult> SifreDegistir([FromBody] SifreDegistirDto dto)
    {
        if (dto == null) return BadRequest();

        // Token'daki NameIdentifier claim'inden kullanıcıyı buluyoruz
        var personelIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(personelIdStr, out var personelId))
            return Unauthorized();

        var personel = await _context.Personels.FindAsync(personelId);
        if (personel == null) return NotFound();

        if (personel.PersonelSifre != dto.EskiSifre)
            return BadRequest(new { Mesaj = "Mevcut şifre hatalı." });

        if (dto.YeniSifre.Length < 6)
            return BadRequest(new { Mesaj = "Yeni şifre en az 6 karakter olmalı." });

        personel.PersonelSifre = dto.YeniSifre;
        // Güvenlik: şifre değişince tüm oturumlar düşer
        personel.RefreshToken = null;
        personel.RefreshTokenBitis = null;
        await _context.SaveChangesAsync();

        return Ok(new { Mesaj = "Şifre başarıyla değiştirildi. Lütfen tekrar giriş yapın." });
    }

    // POST /api/Auth/logout -> güvenli çıkış (refresh token iptal edilir)
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var personelIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(personelIdStr, out var personelId))
            return Unauthorized();

        var personel = await _context.Personels.FindAsync(personelId);
        if (personel == null) return NotFound();

        personel.RefreshToken = null;
        personel.RefreshTokenBitis = null;
        await _context.SaveChangesAsync();

        return Ok(new { Mesaj = "Çıkış yapıldı." });
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

    private static string RefreshTokenUret()
    {
        // Kriptografik olarak güvenli 64 byte rastgele token
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}