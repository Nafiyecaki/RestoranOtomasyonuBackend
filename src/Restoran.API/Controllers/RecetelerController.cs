using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.Data;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecetelerController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public RecetelerController(DbRestoranContext context)
    {
        _context = context;
    }

    // GET /api/Receteler/urun/5 -> bir ürünün reçetesi + malzeme maliyeti
    [HttpGet("urun/{urunId}")]
   // [Authorize(Roles = "Yönetici,Aşçı")]        // <-- EKLE (reçeteyi aşçı da görsün)

    public async Task<IActionResult> GetByUrun(int urunId)
    {
        var urun = await _context.Urunlers
            .Where(u => u.UrunId == urunId)
            .Select(u => new
            {
                u.UrunId,
                u.UrunAdi,
                SatisFiyati = u.Fiyat,
                Recete = u.UrunRecetesis.Select(r => new
                {
                    r.Malzeme!.MalzemeAdi,
                    r.KullanimMiktari,
                    r.Malzeme.Birim,
                    r.Malzeme.BirimMaliyeti,
                    SatirMaliyeti = r.KullanimMiktari * (r.Malzeme.BirimMaliyeti ?? 0)
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (urun == null) return NotFound("Ürün bulunamadı.");

        var toplamMaliyet = urun.Recete.Sum(r => r.SatirMaliyeti);

        return Ok(new
        {
            urun.UrunId,
            urun.UrunAdi,
            urun.SatisFiyati,
            MalzemeMaliyeti = Math.Round(toplamMaliyet, 2),
            BrutKar = Math.Round(urun.SatisFiyati - toplamMaliyet, 2),
            KarMarjiYuzde = urun.SatisFiyati > 0
                ? Math.Round((urun.SatisFiyati - toplamMaliyet) / urun.SatisFiyati * 100, 1)
                : 0,
            urun.Recete
        });
    }

    // GET /api/Receteler/karlilik -> tüm ürünlerin kâr analizi (en kârlı üstte)
    [HttpGet("karlilik")]
    //[Authorize(Roles = "Yönetici")]             // <-- EKLE (kâr bilgisi sadece yönetici!)

    public async Task<IActionResult> KarlilikAnalizi()
    {
        var analiz = await _context.Urunlers
            .Where(u => u.UrunRecetesis.Any())
            .Select(u => new
            {
                u.UrunAdi,
                SatisFiyati = u.Fiyat,
                MalzemeMaliyeti = u.UrunRecetesis
                    .Sum(r => r.KullanimMiktari * (r.Malzeme!.BirimMaliyeti ?? 0))
            })
            .ToListAsync();

        var sonuc = analiz
            .Select(x => new
            {
                x.UrunAdi,
                x.SatisFiyati,
                MalzemeMaliyeti = Math.Round(x.MalzemeMaliyeti, 2),
                BrutKar = Math.Round(x.SatisFiyati - x.MalzemeMaliyeti, 2)
            })
            .OrderByDescending(x => x.BrutKar)
            .ToList();

        return Ok(sonuc);
    }
}