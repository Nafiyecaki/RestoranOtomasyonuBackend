using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.Data;

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
                k.PersonelId,
                k.Odemes,
                k.Personel,

            })
            .ToListAsync();

        return Ok(kasalar);
    }

    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var kasa = await _context.Kasas
            .FirstOrDefaultAsync(k => k.KasaId == id);

        if (kasa == null) return NotFound();
        return Ok(kasa);
    }
}