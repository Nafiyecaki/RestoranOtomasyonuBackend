using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.Data;
using Restoran.Data.Entities;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KasaController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public KasaController(DbRestoranContext context)
    {
        _context = context;
    }

    // GET /api/kasa
    [HttpGet]
    [Authorize(Roles = "Yönetici")]                 // kasayı açma/kapatma yönetici işi
    public async Task<IActionResult> GetAll()
    {
        var kasalar = await _context.Kasas
            .Select(k => new
            {
                k.KasaId,
                k.AcilisTarihi,
                k.KapanisTarihi,
                k.KasaDurumu,
                k.PersonelId
            })
            .ToListAsync();

        return Ok(kasalar);
    }

    // GET /api/kasa/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var kasa = await _context.Kasas
            .Where(k => k.KasaId == id)
            .Select(k => new
            {
                k.KasaId,
                k.AcilisTarihi,
                k.KapanisTarihi,
                k.KasaDurumu,
                k.PersonelId
            })
            .FirstOrDefaultAsync();

        if (kasa == null) return NotFound();
        return Ok(kasa);
    }

    // POST /api/kasa
    [HttpPost]
    public async Task<IActionResult> KasaAc([FromBody] KasaEkleDto dto)
    {
        if (dto == null) return BadRequest();

        // GÜVENLİK KONTROLÜ: Zaten "Açık" olan bir kasa varsa, yenisini açmayı engelliyoruz
        var acikKasaVarMi = await _context.Kasas.AnyAsync(k => k.KasaDurumu == "Açık");
        if (acikKasaVarMi) return BadRequest("Şu anda zaten açık bir kasa bulunuyor. Yeni kasa açmadan önce mevcut olanı kapatmalısınız.");

        var kasa = new Kasa
        {
            AcilisTarihi = DateTime.Now,  // Kasa şu an açılıyor
            KapanisTarihi = null,         // Yeni açıldığı için kapanış henüz yok
            KasaDurumu = dto.KasaDurumu,  // "Açık"
            PersonelId = dto.PersonelId
        };

        _context.Kasas.Add(kasa);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Kasa başarıyla açıldı.",
            kasa.KasaId,
            kasa.AcilisTarihi,
            kasa.KasaDurumu,
            kasa.PersonelId
        });
    }
}