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
        try
        {
            var personel = await _context.Personels
                 .Include(p => p.Rol)
                 .FirstOrDefaultAsync(p => p.KullaniciAdi == dto.KullaniciAdi && p.IsActive == true);

            if (personel == null)
                return Unauthorized(new { success = false, message = "Kullanıcı bulunamadı." });

            // ⛔ BCrypt hash kontrolü (şimdilik kapalı - açmak için alttaki düz metin kontrolünü kapatıp bunu aç)
            // if (!BCrypt.Net.BCrypt.Verify(dto.Sifre, personel.PersonelSifre))
            //     return Unauthorized(new { success = false, message = "Şifre hatalı." });

            // ✅ Düz metin şifre kontrolü (aktif)
            if (personel.PersonelSifre != dto.Sifre)
                return Unauthorized(new { success = false, message = "Şifre hatalı." });

            var token = TokenUret(personel.PersonelId,
                                  personel.KullaniciAdi,
                                  personel.Rol?.RolAdi ?? "Bilinmiyor");

            var refreshToken = RefreshTokenUret();
            personel.RefreshToken = refreshToken;
            personel.RefreshTokenBitis = DateTime.Now.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                Token = token,
                RefreshToken = refreshToken,
                PersonelId = personel.PersonelId,
                AdSoyad = personel.PersonelAdi + " " + personel.PersonelSoyadi,
                Rol = personel.Rol?.RolAdi ?? "Bilinmiyor"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Login Hatası: {ex.Message}");
            return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
        }
    }

    // POST /api/Auth/register -> yeni kullanıcı kaydı
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        try
        {
            if (dto == null)
                return BadRequest(new { success = false, message = "Geçersiz veri." });

            if (string.IsNullOrWhiteSpace(dto.KullaniciAdi) || string.IsNullOrWhiteSpace(dto.Sifre))
                return BadRequest(new { success = false, message = "Kullanıcı adı ve şifre boş olamaz." });

            if (dto.Sifre.Length < 6)
                return BadRequest(new { success = false, message = "Şifre en az 6 karakter olmalı." });

            // 🔥 Rol ID kontrolü (1:Admin, 2:Garson, 3:Aşçı, 4:Kurye)
            if (dto.RolId < 1 || dto.RolId > 4)
                return BadRequest(new { success = false, message = "Geçersiz rol ID. (1:Admin, 2:Garson, 3:Aşçı, 4:Kurye)" });

            var kullaniciAdiAlinmis = await _context.Personels
                .AnyAsync(p => p.KullaniciAdi == dto.KullaniciAdi);
            if (kullaniciAdiAlinmis)
                return Conflict(new { success = false, message = "Bu kullanıcı adı zaten kullanılıyor." });

            var rolVarMi = await _context.Rollers
                .AnyAsync(r => r.RolId == dto.RolId && r.RolDurumu == true);
            if (!rolVarMi)
                return NotFound(new { success = false, message = "Belirtilen rol bulunamadı veya pasif durumda." });

            var personel = new Personel
            {
                PersonelAdi = dto.PersonelAdi ?? "Bilinmiyor",
                PersonelSoyadi = dto.PersonelSoyadi ?? "Bilinmiyor",
                KullaniciAdi = dto.KullaniciAdi,
                PersonelSifre = dto.Sifre,
                RolId = dto.RolId, // ← RolId direkt atanıyor
                IsActive = true
            };

            _context.Personels.Add(personel);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Kayıt başarılı.",
                PersonelId = personel.PersonelId,
                KullaniciAdi = personel.KullaniciAdi,
                RolId = personel.RolId
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Register Hatası: {ex.Message}");
            return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
        }
    }





    // POST /api/Auth/refresh
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshDto dto)
    {
        try
        {
            if (string.IsNullOrEmpty(dto?.RefreshToken))
                return BadRequest(new { success = false, message = "Refresh token gerekli." });

            var personel = await _context.Personels
                .Include(p => p.Rol)
                .FirstOrDefaultAsync(p => p.RefreshToken == dto.RefreshToken && p.IsActive == true);

            if (personel == null || personel.RefreshTokenBitis < DateTime.Now)
                return Unauthorized(new { success = false, message = "Refresh token geçersiz veya süresi dolmuş." });

            var yeniToken = TokenUret(personel.PersonelId,
                                      personel.KullaniciAdi,
                                      personel.Rol?.RolAdi ?? "Bilinmiyor");

            var yeniRefreshToken = RefreshTokenUret();
            personel.RefreshToken = yeniRefreshToken;
            personel.RefreshTokenBitis = DateTime.Now.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                Token = yeniToken,
                RefreshToken = yeniRefreshToken
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Refresh Hatası: {ex.Message}");
            return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
        }
    }

    // POST /api/Auth/sifre-degistir
    [HttpPost("sifre-degistir")]
    [Authorize]
    public async Task<IActionResult> SifreDegistir([FromBody] SifreDegistirDto dto)
    {
        try
        {
            if (dto == null)
                return BadRequest(new { success = false, message = "Geçersiz veri." });

            var personelIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(personelIdStr, out var personelId))
                return Unauthorized(new { success = false, message = "Yetkisiz erişim." });

            var personel = await _context.Personels.FindAsync(personelId);
            if (personel == null)
                return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });

            // ⛔ BCrypt hash kontrolü (şimdilik kapalı)
            // if (!BCrypt.Net.BCrypt.Verify(dto.EskiSifre, personel.PersonelSifre))
            //     return BadRequest(new { success = false, message = "Mevcut şifre hatalı." });

            // ✅ Düz metin kontrol (aktif)
            if (personel.PersonelSifre != dto.EskiSifre)
                return BadRequest(new { success = false, message = "Mevcut şifre hatalı." });

            if (dto.YeniSifre.Length < 6)
                return BadRequest(new { success = false, message = "Yeni şifre en az 6 karakter olmalı." });

            // ⛔ Yeni şifreyi BCrypt ile hash'leyerek kaydetme (şimdilik kapalı)
            // personel.PersonelSifre = BCrypt.Net.BCrypt.HashPassword(dto.YeniSifre);

            // ✅ Düz metin kaydet (aktif)
            personel.PersonelSifre = dto.YeniSifre;

            personel.RefreshToken = null;
            personel.RefreshTokenBitis = null;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Şifre başarıyla değiştirildi. Lütfen tekrar giriş yapın." });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Şifre Değiştirme Hatası: {ex.Message}");
            return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
        }
    }

    // POST /api/Auth/logout
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var personelIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(personelIdStr, out var personelId))
                return Unauthorized(new { success = false, message = "Yetkisiz erişim." });

            var personel = await _context.Personels.FindAsync(personelId);
            if (personel == null)
                return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });

            personel.RefreshToken = null;
            personel.RefreshTokenBitis = null;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Çıkış yapıldı." });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Logout Hatası: {ex.Message}");
            return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
        }
    }

    // ============ ÖZEL METODLAR ============

    private string TokenUret(int personelId, string kullaniciAdi, string rol)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, personelId.ToString()),
            new Claim(ClaimTypes.Name, kullaniciAdi),
            new Claim(ClaimTypes.Role, rol)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "GizliAnahtar1234567890!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "RestoranAPI",
            audience: _config["Jwt:Audience"] ?? "RestoranClient",
            claims: claims,
            expires: DateTime.Now.AddMinutes(
                double.Parse(_config["Jwt:ExpireMinutes"] ?? "480")),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string RefreshTokenUret()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}