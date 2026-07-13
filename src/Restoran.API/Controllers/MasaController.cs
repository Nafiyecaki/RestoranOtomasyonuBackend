using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.Data;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MasaController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public MasaController(DbRestoranContext context)
    {
        _context = context;
    }

    // GET /api/masa
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var masalar = await _context.Masas
            .Select(m => new
            {
                m.MasaId,
                m.MasaNo,
                m.MasaDurumu
            })
            .ToListAsync();

        return Ok(masalar);
    }

    // GET /api/masa/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var masa = await _context.Masas
            .FirstOrDefaultAsync(m => m.MasaId == id);

        if (masa == null) return NotFound();
        return Ok(masa);
    }
}