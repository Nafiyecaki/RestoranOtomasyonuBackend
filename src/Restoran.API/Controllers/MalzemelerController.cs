using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.Data;
using Restoran.Data.Entities;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MalzemelerController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public MalzemelerController(DbRestoranContext context)
    {
        _context = context;
    }

    // GET /api/Malzemeler -> tüm malzemeler
    [HttpGet]
    [Authorize(Roles = "Yönetici,Aşçı")]

    public async Task<IActionResult> GetAll()
    {
        var malzemeler = await _context.Malzemelers
            .Select(m => new
            {
                m.MalzemeId,
                m.MalzemeAdi,
                m.StokMiktari,
                m.Birim,
                m.BirimMaliyeti
            })
            .OrderBy(m => m.MalzemeAdi)
            .ToListAsync();

        return Ok(malzemeler);
    }

    // GET /api/Malzemeler/5 -> tek malzeme + hangi ürünlerde kullanıldığı
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var malzeme = await _context.Malzemelers
            .Where(m => m.MalzemeId == id)
            .Select(m => new
            {
                m.MalzemeId,
                m.MalzemeAdi,
                m.StokMiktari,
                m.Birim,
                m.BirimMaliyeti,
                KullanildigiUrunler = m.UrunRecetesis.Select(r => new
                {
                    r.Urun!.UrunId,
                    r.Urun.UrunAdi,
                    r.KullanimMiktari
                })
            })
            .FirstOrDefaultAsync();

        if (malzeme == null) return NotFound();
        return Ok(malzeme);
    }

    // POST /api/Malzemeler -> yeni malzeme ekle
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MalzemeEkleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.MalzemeAdi))
            return BadRequest("Malzeme adı boş olamaz.");

        var malzeme = new Malzemeler
        {
            MalzemeAdi = dto.MalzemeAdi.Trim(),
            StokMiktari = dto.StokMiktari,
            Birim = dto.Birim,
            BirimMaliyeti = dto.BirimMaliyeti
        };

        _context.Malzemelers.Add(malzeme);
        await _context.SaveChangesAsync();

        return Ok(new { Mesaj = "Malzeme eklendi.", malzeme.MalzemeId });
    }

    // PATCH /api/Malzemeler/5/stok -> malzeme stoğu ekle/düş
    [HttpPatch("{id}/stok")]
    public async Task<IActionResult> StokGuncelle(int id, [FromBody] MalzemeStokGuncelleDto dto)
    {
        var malzeme = await _context.Malzemelers.FindAsync(id);
        if (malzeme == null) return NotFound("Malzeme bulunamadı.");

        var yeniStok = malzeme.StokMiktari + dto.Miktar;
        if (yeniStok < 0)
            return BadRequest($"Yetersiz stok! Mevcut: {malzeme.StokMiktari} {malzeme.Birim}");

        malzeme.StokMiktari = yeniStok;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Malzeme stoğu güncellendi.",
            malzeme.MalzemeAdi,
            YeniStok = malzeme.StokMiktari,
            malzeme.Birim
        });
    }
}