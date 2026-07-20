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

    // GET /api/Urunler -> Sadece AKTÝF ürünleri kategorisiyle birlikte listeler
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var urunler = await _context.Urunlers
            .Where(u => (u.IsActive == true || u.IsActive == null) && u.SilinmeTarihi == null) // SADECE AKTÝFLER
            .Include(u => u.Kategori)
            .Select(u => new
            {
                u.UrunId,
                u.UrunAdi,
                u.Fiyat,
                u.StokMiktari,
                u.Aciklamalar,
                IsActive = u.IsActive ?? true,
                KategoriAdi = u.Kategori != null ? u.Kategori.KategoriAdi : null
            })
            .ToListAsync();

        return Ok(urunler);
    }

    // GET /api/Urunler/5 -> Tek ürünü getirir (Aktifse)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var urun = await _context.Urunlers
            .Where(u => u.UrunId == id && (u.IsActive == true || u.IsActive == null) && u.SilinmeTarihi == null)
            .Select(u => new
            {
                u.UrunId,
                u.UrunAdi,
                u.Fiyat,
                u.StokMiktari,
                u.Aciklamalar,
                IsActive = u.IsActive ?? true,
                KategoriAdi = u.Kategori != null ? u.Kategori.KategoriAdi : null
            })
            .FirstOrDefaultAsync();

        if (urun == null) return NotFound("Aradýðýnýz ürün bulunamadý veya satýþta deðil.");
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
                return BadRequest($"Gönderilen KategoriId ({dto.KategoriId}) sistemde tanýmlý deðil.");
            }
        }

        var yeniUrun = new Urunler
        {
            UrunAdi = dto.UrunAdi,
            Fiyat = dto.Fiyat,
            StokMiktari = dto.StokMiktari,
            Aciklamalar = dto.Aciklamalar,
            KategoriId = dto.KategoriId,
            IsActive = true, // YENÝ ÜRÜN VARSAYILAN OLARAK AKTÝF GELÝR
            SilinmeTarihi = null
        };

        await _context.Urunlers.AddAsync(yeniUrun);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = yeniUrun.UrunId }, new
        {
            Mesaj = "Ürün baþarýyla menüye eklendi.",
            UrunId = yeniUrun.UrunId,
            yeniUrun.UrunAdi,
            yeniUrun.Fiyat
        });
    }

    // PUT /api/Urunler/5 -> Ürün bilgilerini günceller
    [HttpPut("{id}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] UrunGuncelleDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var urun = await _context.Urunlers.FindAsync(id);
        if (urun == null || urun.SilinmeTarihi != null) return NotFound("Güncellenmek istenen ürün bulunamadý.");

        if (dto.KategoriId.HasValue)
        {
            var kategoriVarMi = await _context.Set<Kategori>().AnyAsync(k => k.KategoriId == dto.KategoriId);
            if (!kategoriVarMi)
            {
                return BadRequest($"Gönderilen KategoriId ({dto.KategoriId}) geçerli bir kategori deðil.");
            }
        }

        urun.UrunAdi = dto.UrunAdi;
        urun.Fiyat = dto.Fiyat;
        urun.StokMiktari = dto.StokMiktari;
        urun.Aciklamalar = dto.Aciklamalar;
        urun.KategoriId = dto.KategoriId;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Ürün bilgileri baþarýyla güncellendi.",
            UrunId = urun.UrunId,
            urun.UrunAdi,
            urun.Fiyat,
            urun.StokMiktari
        });
    }

    // DELETE /api/Urunler/5 -> Ürünü fiziksel silmez, PASÝFE ÇEKER (Soft Delete)
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var urun = await _context.Urunlers.FindAsync(id);
        if (urun == null || urun.SilinmeTarihi != null)
            return NotFound("Silinmek istenen ürün bulunamadý veya zaten pasif durumda.");

        // Fiziksel Silmek (Remove) yerine durumunu pasife çekiyoruz
        urun.IsActive = false;
        urun.SilinmeTarihi = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new { Mesaj = "Ürün menüden kaldýrýldý ve pasif duruma getirildi." });
    }
}