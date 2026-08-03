using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.Data;
using Restoran.Data.Entities;
using System.Security.Claims;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/uyeler/adresler")]
[Authorize]
public class AdresController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public AdresController(DbRestoranContext context)
    {
        _context = context;
    }

    private int? GetCurrentUyeId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    // GET /api/uyeler/adresler  -> sadece giriş yapan üyenin adresleri
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var uyeId = GetCurrentUyeId();
        if (uyeId == null) return Unauthorized();

        var adresler = await _context.Adres
            .Where(a => a.UyeId == uyeId)
            .Select(a => new
            {
                a.AdresId,
                a.AdresTipi,
                a.AcikAdres,
                a.TeslimatBolgesindeMi
            })
            .ToListAsync();

        return Ok(adresler);
    }

    // GET /api/uyeler/adresler/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var uyeId = GetCurrentUyeId();
        if (uyeId == null) return Unauthorized();

        var adres = await _context.Adres
            .Where(a => a.AdresId == id && a.UyeId == uyeId)
            .Select(a => new
            {
                a.AdresId,
                a.AdresTipi,
                a.AcikAdres,
                a.TeslimatBolgesindeMi
            })
            .FirstOrDefaultAsync();

        if (adres == null) return NotFound();
        return Ok(adres);
    }

    // POST /api/uyeler/adresler
    [HttpPost]
    public async Task<IActionResult> AdresEkle([FromBody] AdresEkleDto dto)
    {
        if (dto == null) return BadRequest();

        var uyeId = GetCurrentUyeId();
        if (uyeId == null) return Unauthorized();

        var uyeVarMi = await _context.Uyelers.AnyAsync(u => u.UyeId == uyeId);
        if (!uyeVarMi) return NotFound("Üye bulunamadı.");

        var adres = new Adres
        {
            AdresTipi = dto.AdresTipi,
            AcikAdres = dto.AcikAdres,
            TeslimatBolgesindeMi = dto.TeslimatBolgesindeMi,
            UyeId = uyeId
        };

        _context.Adres.Add(adres);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            adres.AdresId,
            adres.AdresTipi,
            adres.AcikAdres,
            adres.TeslimatBolgesindeMi
        });
    }

    // PUT /api/uyeler/adresler/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] AdresEkleDto dto)
    {
        if (dto == null) return BadRequest();

        var uyeId = GetCurrentUyeId();
        if (uyeId == null) return Unauthorized();

        var adres = await _context.Adres
            .FirstOrDefaultAsync(a => a.AdresId == id && a.UyeId == uyeId);
        if (adres == null) return NotFound(new { Mesaj = "Adres bulunamadı." });

        adres.AdresTipi = dto.AdresTipi;
        adres.AcikAdres = dto.AcikAdres;
        adres.TeslimatBolgesindeMi = dto.TeslimatBolgesindeMi;

        await _context.SaveChangesAsync();

        return Ok(new { Mesaj = "Adres güncellendi.", adres.AdresId });
    }

    // DELETE /api/uyeler/adresler/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var uyeId = GetCurrentUyeId();
        if (uyeId == null) return Unauthorized();

        var adres = await _context.Adres
            .FirstOrDefaultAsync(a => a.AdresId == id && a.UyeId == uyeId);
        if (adres == null) return NotFound(new { Mesaj = "Adres bulunamadı." });

        _context.Adres.Remove(adres);
        await _context.SaveChangesAsync();

        return Ok(new { Mesaj = "Adres silindi.", AdresId = id });
    }
}