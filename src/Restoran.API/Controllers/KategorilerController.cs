using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.Data;
using Restoran.Data.Entities;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KategorilerController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public KategorilerController(DbRestoranContext context)
    {
        _context = context;
    }

    // GET /api/Kategoriler
    [HttpGet]
   // [Authorize]                       // kategorileri herkes görür (menü için)
                                      // POST/PUT/DELETE varsa → [Authorize(Roles = "Yönetici")]
    public async Task<IActionResult> GetAll()
    {
        var kategoriler = await _context.Kategoris
            .Select(k => new
            {
                k.KategoriId,
                k.KategoriAdi
            })
            .ToListAsync();

        return Ok(kategoriler);
    }

    // GET /api/Kategoriler/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var kategori = await _context.Kategoris
            .Where(k => k.KategoriId == id)
            .Select(k => new
            {
                k.KategoriId,
                k.KategoriAdi
            })
            .FirstOrDefaultAsync();

        if (kategori == null) return NotFound();
        return Ok(kategori);
    }

    // POST /api/Kategoriler
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Kategori kategori)
    {
        if (kategori == null) return BadRequest();

        _context.Kategoris.Add(kategori);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = kategori.KategoriId }, kategori);
    }
}