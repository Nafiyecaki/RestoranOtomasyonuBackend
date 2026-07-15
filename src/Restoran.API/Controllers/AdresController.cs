using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.Data;
using Restoran.Data.Entities;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdresController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public AdresController(DbRestoranContext context)
    {
        _context = context;
    }

    // GET /api/Adres
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // EF Core çoğul adlandırmasına göre burası '_context.Adreses' veya '_context.Adres' olabilir.
     
        var adresler = await _context.Adres
            .Select(a => new
            {
                a.AdresId,
                a.AdresTipi,
                a.AcikAdres,
                a.TeslimatBolgesindeMi,
                a.UyeId
            })
            .ToListAsync();

        return Ok(adresler);
    }

    // GET /api/Adres/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var adres = await _context.Adres
            .Where(a => a.AdresId == id)
            .Select(a => new
            {
                a.AdresId,
                a.AdresTipi,
                a.AcikAdres,
                a.TeslimatBolgesindeMi,
                a.UyeId
            })
            .FirstOrDefaultAsync();

        if (adres == null) return NotFound();
        return Ok(adres);
    }

    // POST /api/Adres
    [HttpPost]
    public async Task<IActionResult> AdresEkle([FromBody] AdresEkleDto dto)
    {
        if (dto == null) return BadRequest();

        // GÜVENLİK KONTROLÜ: Adres tanımlanacak üye veritabanında gerçekten var mı?
        if (dto.UyeId.HasValue)
        {
            // Entity ismi 'Uyeler' olduğu için context içinde muhtemelen 'Uyelers' diye tanımlanmıştır.
            var uyeVarMi = await _context.Uyelers.AnyAsync(u => u.UyeId == dto.UyeId);
            if (!uyeVarMi) return NotFound("Adres tanımlanmak istenen üye bulunamadı.");
        }

        var adres = new Adres
        {
            AdresTipi = dto.AdresTipi,
            AcikAdres = dto.AcikAdres,
            TeslimatBolgesindeMi = dto.TeslimatBolgesindeMi,
            UyeId = dto.UyeId
        };

        _context.Adres.Add(adres);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Adres başarıyla sisteme kaydedildi.",
            adres.AdresId,
            adres.UyeId,
            adres.AdresTipi
        });
    }
    // PUT /api/Adres/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] AdresEkleDto dto)
    {
        if (dto == null) return BadRequest();

        var adres = await _context.Adres.FindAsync(id);
        if (adres == null) return NotFound(new { Mesaj = "Adres bulunamadı." });

        // GÜVENLİK KONTROLÜ: Üye değiştiriliyorsa yeni üye gerçekten var mı?
        if (dto.UyeId.HasValue)
        {
            var uyeVarMi = await _context.Uyelers.AnyAsync(u => u.UyeId == dto.UyeId);
            if (!uyeVarMi) return NotFound(new { Mesaj = "Adres tanımlanmak istenen üye bulunamadı." });
        }

        adres.AdresTipi = dto.AdresTipi;
        adres.AcikAdres = dto.AcikAdres;
        adres.TeslimatBolgesindeMi = dto.TeslimatBolgesindeMi;
        adres.UyeId = dto.UyeId;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Adres başarıyla güncellendi.",
            adres.AdresId,
            adres.AdresTipi
        });
    }

    // DELETE /api/Adres/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var adres = await _context.Adres.FindAsync(id);
        if (adres == null) return NotFound(new { Mesaj = "Adres bulunamadı." });

        _context.Adres.Remove(adres);
        await _context.SaveChangesAsync();

        return Ok(new { Mesaj = "Adres silindi.", AdresId = id });
    }
}