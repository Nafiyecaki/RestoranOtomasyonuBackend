using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.Data;
using Restoran.Data.Entities; // Odeme entity'sini görebilmesi için

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

    // GET /api/odeme (Tüm ödemeleri listeler)
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

    // GET /api/odeme/5 (Id'ye göre tek bir ödeme getirir)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var odeme = await _context.Odemes
            .FirstOrDefaultAsync(o => o.OdemeId == id);

        if (odeme == null) return NotFound();
        return Ok(odeme);
    }

    // POST /api/odeme (Yeni ödeme yapar / veritabanına kaydeder)
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Odeme yeniOdeme)
    {
        if (yeniOdeme == null) return BadRequest("Geçersiz ödeme verisi.");

        // OdemeId veritabanında otomatik artan (identity) olduğu için buraya eklemiyoruz.
        _context.Odemes.Add(yeniOdeme);

        // Değişiklikleri veritabanına kesin olarak işler (Save)
        await _context.SaveChangesAsync();

        // Başarılı olduktan sonra hem 201 Created döner hem de eklenen veriyi gösterir
        return CreatedAtAction(nameof(GetById), new { id = yeniOdeme.OdemeId }, yeniOdeme);
    }
}