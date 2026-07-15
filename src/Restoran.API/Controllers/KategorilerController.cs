using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.Data;
using Restoran.Data.Entities;
using Restoran.API.Dtos;

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
            .Where(k => k.IsActive == true)
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
   
    public async Task<IActionResult> Create([FromBody] KategoriEkleDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.KategoriAdi))
            return BadRequest(new { Mesaj = "Kategori adı boş olamaz." });

        // Aynı isimde AKTİF kategori var mı?
        var adVarMi = await _context.Kategoris
            .AnyAsync(k => k.KategoriAdi == dto.KategoriAdi.Trim() && k.IsActive == true);
        if (adVarMi)
            return Conflict(new { Mesaj = "Bu isimde bir kategori zaten var." });

        var kategori = new Kategori
        {
            KategoriAdi = dto.KategoriAdi.Trim(),
            IsActive = true
        };

        _context.Kategoris.Add(kategori);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Kategori eklendi.",
            kategori.KategoriId,
            kategori.KategoriAdi
        });
    }

    // PUT /api/Kategoriler/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] KategoriEkleDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.KategoriAdi))
            return BadRequest(new { Mesaj = "Kategori adı boş olamaz." });

        var kategori = await _context.Kategoris.FindAsync(id);
        if (kategori == null) return NotFound(new { Mesaj = "Kategori bulunamadı." });

        // Yeni isim başka bir kategoride kullanılıyor mu?
        var adVarMi = await _context.Kategoris
            .AnyAsync(k => k.KategoriAdi == dto.KategoriAdi.Trim() && k.KategoriId != id);
        if (adVarMi)
            return Conflict(new { Mesaj = "Bu isimde başka bir kategori zaten var." });

        kategori.KategoriAdi = dto.KategoriAdi.Trim();
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Kategori güncellendi.",
            kategori.KategoriId,
            kategori.KategoriAdi
        });
    }

    // DELETE /api/Kategoriler/{id} -> SOFT DELETE: kayıt silinmez, pasife çekilir
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var kategori = await _context.Kategoris.FindAsync(id);
        if (kategori == null) return NotFound(new { Mesaj = "Kategori bulunamadı." });

        if (kategori.IsActive == false)
            return BadRequest(new { Mesaj = "Bu kategori zaten silinmiş (pasif) durumda." });

        // İçinde AKTİF ürün olan kategori pasife çekilemez
        var aktifUrunVarMi = await _context.Urunlers
            .AnyAsync(u => u.KategoriId == id && u.IsActive == true);
        if (aktifUrunVarMi)
            return BadRequest(new { Mesaj = "Bu kategoriye bağlı aktif ürünler var, silinemez. Önce ürünleri taşıyın veya silin." });

        kategori.IsActive = false;
        kategori.SilinmeTarihi = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(new { Mesaj = "Kategori silindi (pasife alındı).", KategoriId = id });
    }
}