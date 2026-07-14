using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.Data;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RezervasyonController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public RezervasyonController(DbRestoranContext context)
    {
        _context = context;
    }

    // GET /api/rezervasyon
    [HttpGet]
    //[Authorize(Roles = "Yönetici,Garson")]

    public async Task<IActionResult> GetAll()
    {
        var rezervasyonlar = await _context.Rezervasyons
            .Select(r => new
            {
                r.RezervasyonId,               
                r.MusteriAdi,
                r.MusteriSoyadi,
                r.Telefon,
                r.KisiSayisi,
                r.TarihSaat,
                r.Durum,
                r.OlusturulmaTarihi,
                r.Aciklama,
                r.MasaId,
                r.RezervasyonTipi,
                r.Masa,
            })
            .ToListAsync();

        return Ok(rezervasyonlar);
    }

    // GET /api/rezervasyon/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var rezervasyon = await _context.Rezervasyons
            .FirstOrDefaultAsync(r => r.RezervasyonId == id);

        if (rezervasyon == null) return NotFound();
        return Ok(rezervasyon);
    }
}