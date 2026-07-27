using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.Data;
using Restoran.Data.Entities;
using Restoran.API.Dtos;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;

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

    // GET /api/Urunler -> Tüm ürünleri (aktif + pasif) listeler
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
                IsActive = u.IsActive,
                KategoriId = u.KategoriId,
                KategoriAdi = u.Kategori != null ? u.Kategori.KategoriAdi : null,
                u.SilinmeTarihi
            })
            .ToListAsync();

        return Ok(urunler);
    }

    // GET /api/Urunler/5 -> Tek ürünü getirir
    [HttpGet("{id}")]
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
                IsActive = u.IsActive,  // ✅ Olduğu gibi gönder
                KategoriAdi = u.Kategori != null ? u.Kategori.KategoriAdi : null,
                u.SilinmeTarihi
            })
            .FirstOrDefaultAsync();

        if (urun == null) return NotFound("Aradığınız ürün bulunamadı.");
        return Ok(urun);
    }

    // POST /api/Urunler -> Yeni ürün ekler
    [HttpPost]
    public async Task<IActionResult> Ekle([FromBody] UrunEkleDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (dto.KategoriId.HasValue)
        {
            var kategoriVarMi = await _context.Set<Kategori>().AnyAsync(k => k.KategoriId == dto.KategoriId);
            if (!kategoriVarMi)
            {
                return BadRequest($"Gönderilen KategoriId ({dto.KategoriId}) sistemde tanımlı değil.");
            }
        }

        var yeniUrun = new Urunler
        {
            UrunAdi = dto.UrunAdi,
            Fiyat = dto.Fiyat,
            StokMiktari = dto.StokMiktari,
            Aciklamalar = dto.Aciklamalar,
            KategoriId = dto.KategoriId,
            IsActive = true,
            SilinmeTarihi = null
        };

        await _context.Urunlers.AddAsync(yeniUrun);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = yeniUrun.UrunId }, new
        {
            Mesaj = "Ürün başarıyla menüye eklendi.",
            UrunId = yeniUrun.UrunId,
            yeniUrun.UrunAdi,
            yeniUrun.Fiyat,
            yeniUrun.IsActive
        });
    }

    // PUT /api/Urunler/5 -> Ürün bilgilerini günceller
    [HttpPut("{id}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] UrunGuncelleDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var urun = await _context.Urunlers.FindAsync(id);
        if (urun == null) return NotFound("Güncellenmek istenen ürün bulunamadı.");

        if (dto.KategoriId.HasValue)
        {
            var kategoriVarMi = await _context.Set<Kategori>().AnyAsync(k => k.KategoriId == dto.KategoriId);
            if (!kategoriVarMi)
            {
                return BadRequest($"Gönderilen KategoriId ({dto.KategoriId}) geçerli bir kategori değil.");
            }
        }

        urun.UrunAdi = dto.UrunAdi ?? urun.UrunAdi;
        urun.Fiyat = dto.Fiyat;
        urun.StokMiktari = dto.StokMiktari;
        urun.Aciklamalar = dto.Aciklamalar ?? urun.Aciklamalar;
        urun.KategoriId = dto.KategoriId ?? urun.KategoriId;

        // ✅ IsActive güncelleme desteği
        if (dto.IsActive.HasValue)
        {
            urun.IsActive = dto.IsActive.Value;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Ürün bilgileri başarıyla güncellendi.",
            UrunId = urun.UrunId,
            urun.UrunAdi,
            urun.Fiyat,
            urun.StokMiktari,
            urun.IsActive
        });
    }

    // DELETE /api/Urunler/5 -> Ürünü fiziksel silmez, PASİFE ÇEKER (Soft Delete)
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var urun = await _context.Urunlers.FindAsync(id);
        if (urun == null)
            return NotFound("Silinmek istenen ürün bulunamadı.");

        // Zaten pasif mi kontrol et
        if (urun.IsActive == false)
            return BadRequest(new { Mesaj = "Bu ürün zaten pasif durumda." });

        // Fiziksel Silmek (Remove) yerine durumunu pasife çekiyoruz
        urun.IsActive = false;
        urun.SilinmeTarihi = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Ürün menüden kaldırıldı ve pasif duruma getirildi.",
            UrunId = id,
            IsActive = urun.IsActive
        });
    }
}