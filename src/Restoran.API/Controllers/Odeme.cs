using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.Data;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OdemeController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public OdemeController(DbRestoranContext context)
    {
        _context = context;
    }

    // GET /api/odeme
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var odemeler = await _context.Odemes
            .Select(o => new
            {
                o.OdemeId,
                o.OdemeTipi,
                o.OdemeTutari,
                o.OdemeTarihi,
                o.PersonelId,
                o.SiparisId,
                o.KasaId
            })
            .ToListAsync();

        return Ok(odemeler);
    }

    // GET /api/odeme/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var odeme = await _context.Odemes
            .FirstOrDefaultAsync(o => o.OdemeId == id);

        if (odeme == null) return NotFound();
        return Ok(odeme);
    }
}