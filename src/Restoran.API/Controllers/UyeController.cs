using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.Data;
using Restoran.Data.Entities;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UyelerController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public UyelerController(DbRestoranContext context)
    {
        _context = context;
    }

    // GET /api/Uyeler
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // EF Core çoğul adlandırmasına göre context içinde 'Uyelers' olarak tanımlanmıştır.
        var uyeler = await _context.Uyelers
            .Select(u => new
            {
                u.UyeId,
                u.UyeAdi,
                u.UyeSoyadi,
                u.UyeTelefon,
                u.UyeEmail,
                u.Cinsiyet,
                u.KayitTarihi
                // PRO TİP: Güvenlik için 'UyeSifre' alanını listelemeye dahil etmedik dayıko!
            })
            .ToListAsync();

        return Ok(uyeler);
    }

    // GET /api/Uyeler/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var uye = await _context.Uyelers
            .Where(u => u.UyeId == id)
            .Select(u => new
            {
                u.UyeId,
                u.UyeAdi,
                u.UyeSoyadi,
                u.UyeTelefon,
                u.UyeEmail,
                u.Cinsiyet,
                u.KayitTarihi
            })
            .FirstOrDefaultAsync();

        if (uye == null) return NotFound();
        return Ok(uye);
    }

    // POST /api/Uyeler
    [HttpPost]
    public async Task<IActionResult> UyeEkle([FromBody] UyeEkleDto dto)
    {
        if (dto == null) return BadRequest();

        // GÜVENLİK KONTROLÜ: Aynı e-posta adresiyle başka bir üye var mı?
        var emailVarMi = await _context.Uyelers.AnyAsync(u => u.UyeEmail == dto.UyeEmail);
        if (emailVarMi) return BadRequest("Bu e-posta adresiyle kayıtlı bir üye zaten mevcut.");

        var uye = new Uyeler
        {
            UyeAdi = dto.UyeAdi,
            UyeSoyadi = dto.UyeSoyadi,
            UyeTelefon = dto.UyeTelefon,
            UyeEmail = dto.UyeEmail,
            UyeSifre = dto.UyeSifre, // İleride buraya MD5/SHA256 şifre hashleme eklenebilir
            Cinsiyet = dto.Cinsiyet,
            KayitTarihi = DateTime.Now // Kayıt anındaki sistem saati otomatik basılıyor
        };

        _context.Uyelers.Add(uye);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Üye kaydı başarıyla tamamlandı.",
            uye.UyeId,
            uye.UyeAdi,
            uye.UyeSoyadi,
            uye.UyeEmail
        });
    }
}