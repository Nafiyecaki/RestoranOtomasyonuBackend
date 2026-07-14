using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.Data;
using Restoran.Data.Entities;

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

    // GET /api/Receteler -> tüm reçete satırları
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var receteler = await _context.UrunRecetesis
            .Select(r => new
            {
                r.ReceteId,
                r.UrunId,
                UrunAdi = r.Urun!.UrunAdi,
                r.MalzemeId,
                MalzemeAdi = r.Malzeme!.MalzemeAdi,
                r.KullanimMiktari,
                r.Malzeme.Birim
            })
            .OrderBy(r => r.UrunAdi)
            .ToListAsync();

        return Ok(receteler);
    }

    // GET /api/Receteler/urun/5 -> bir ürünün reçetesi + malzeme maliyeti
    [HttpGet("urun/{urunId}")]
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
                    r.ReceteId,
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
            urun.Recete
        });
    }

    // POST /api/Receteler -> ürüne reçete satırı ekle
    [HttpPost]
    public async Task<IActionResult> Ekle([FromBody] ReceteEkleDto dto)
    {
        if (dto == null) return BadRequest();

        if (dto.KullanimMiktari <= 0)
            return BadRequest(new { Mesaj = "Kullanım miktarı sıfırdan büyük olmalı." });

        var urunVarMi = await _context.Urunlers.AnyAsync(u => u.UrunId == dto.UrunId);
        if (!urunVarMi) return NotFound(new { Mesaj = "Ürün bulunamadı." });

        var malzemeVarMi = await _context.Malzemelers.AnyAsync(m => m.MalzemeId == dto.MalzemeId);
        if (!malzemeVarMi) return NotFound(new { Mesaj = "Malzeme bulunamadı." });

        // Aynı ürüne aynı malzeme ikinci kez eklenemez
        var satirVarMi = await _context.UrunRecetesis
            .AnyAsync(r => r.UrunId == dto.UrunId && r.MalzemeId == dto.MalzemeId);
        if (satirVarMi)
            return Conflict(new { Mesaj = "Bu malzeme bu ürünün reçetesinde zaten var. Miktarı güncellemek için PUT kullanın." });

        var recete = new UrunRecetesi
        {
            UrunId = dto.UrunId,
            MalzemeId = dto.MalzemeId,
            KullanimMiktari = dto.KullanimMiktari
        };

        _context.UrunRecetesis.Add(recete);
        await _context.SaveChangesAsync();

        return Ok(new { Mesaj = "Reçete satırı eklendi.", recete.ReceteId });
    }

    // PUT /api/Receteler/{id} -> reçete satırını güncelle
    [HttpPut("{id}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] ReceteEkleDto dto)
    {
        if (dto == null) return BadRequest();

        if (dto.KullanimMiktari <= 0)
            return BadRequest(new { Mesaj = "Kullanım miktarı sıfırdan büyük olmalı." });

        var recete = await _context.UrunRecetesis.FindAsync(id);
        if (recete == null) return NotFound(new { Mesaj = "Reçete satırı bulunamadı." });

        var urunVarMi = await _context.Urunlers.AnyAsync(u => u.UrunId == dto.UrunId);
        if (!urunVarMi) return NotFound(new { Mesaj = "Ürün bulunamadı." });

        var malzemeVarMi = await _context.Malzemelers.AnyAsync(m => m.MalzemeId == dto.MalzemeId);
        if (!malzemeVarMi) return NotFound(new { Mesaj = "Malzeme bulunamadı." });

        recete.UrunId = dto.UrunId;
        recete.MalzemeId = dto.MalzemeId;
        recete.KullanimMiktari = dto.KullanimMiktari;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Reçete satırı güncellendi.",
            recete.ReceteId,
            recete.KullanimMiktari
        });
    }

    // DELETE /api/Receteler/{id} -> reçete satırını sil
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var recete = await _context.UrunRecetesis.FindAsync(id);
        if (recete == null) return NotFound(new { Mesaj = "Reçete satırı bulunamadı." });

        _context.UrunRecetesis.Remove(recete);
        await _context.SaveChangesAsync();

        return Ok(new { Mesaj = "Reçete satırı silindi.", ReceteId = id });
    }
}