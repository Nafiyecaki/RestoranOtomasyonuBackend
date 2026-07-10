using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.Data;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UrunlerController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public UrunlerController(DbRestoranContext context)
    {
        _context = context;
    }

    // GET /api/urunler
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var urunler = await _context.Urunlers
            .Include(u => u.Kategori)
            .Select(u => new
            {
                u.UrunId,
                u.UrunAdi,
                u.Fiyat,
                u.StokMiktari,
                u.Aciklamalar,
                KategoriAdi = u.Kategori.KategoriAdi
            })
            .ToListAsync();

        return Ok(urunler);
    }

    // GET /api/urunler/1207
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var urun = await _context.Urunlers
            .Include(u => u.Kategori)
            .FirstOrDefaultAsync(u => u.UrunId == id);

        if (urun == null) return NotFound();
        return Ok(urun);
    }
}