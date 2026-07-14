using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.Data;
using Microsoft.AspNetCore.Authorization;

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

    // GET /api/Urunler
    [HttpGet]
  //  [Authorize]                                    // <-- EKLENDÝ: token'sýz giriþ yasak
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
                KategoriAdi = u.Kategori != null ? u.Kategori.KategoriAdi : null
            })
            .ToListAsync();
        return Ok(urunler);
    }

    // GET /api/Urunler/5
    [HttpGet("{id}")]
   // [Authorize]                                    // <-- EKLENDÝ
    public async Task<IActionResult> GetById(int id)
    {
        var urun = await _context.Urunlers
            .Where(u => u.UrunId == id)
            .Select(u => new
            {
                u.UrunId,
                u.UrunAdi,
                u.Fiyat,
                u.StokMiktari,
                u.Aciklamalar,
                KategoriAdi = u.Kategori != null ? u.Kategori.KategoriAdi : null
            })
            .FirstOrDefaultAsync();

        if (urun == null) return NotFound();
        return Ok(urun);
    }
}