using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.Data;
using Restoran.Data.Entities; // <-- ENTITY'LERÝN OLDUÐU KLASÖR (Urunler vb. için)
using Restoran.API.Dtos;      // <-- DTO'LARIN OLDUÐU KLASÖR
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

    // GET /api/Urunler -> Tüm ürünleri kategorisiyle birlikte listeler
    [HttpGet]
    // [Authorize] 
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
                KategoriAdi = u.Kategori != null ? u.Kategori.KategoriAdi : null
            })
            .ToListAsync();
        return Ok(urunler);
    }

    // GET /api/Urunler/5 -> Tek ürünü getirir
    [HttpGet("{id}")]
    // [Authorize] 
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
                KategoriAdi = u.Kategori != null ? u.Kategori.KategoriAdi : null
            })
            .FirstOrDefaultAsync();

        if (urun == null) return NotFound("Aradýðýnýz ürün bulunamadý.");
        return Ok(urun);
    }

    // POST /api/Urunler -> Yeni ürün ekler
    [HttpPost]
    // [Authorize(Roles = "Yönetici")] // <-- Sadece yöneticiler menüye ekleme yapabilsin dersen açarsýn
    public async Task<IActionResult> Ekle([FromBody] UrunEkleDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // KategoriId gönderildiyse veritabanýnda böyle bir kategorinin gerçekten var olup olmadýðýný kontrol edelim
        if (dto.KategoriId.HasValue)
        {
            var kategoriVarMi = await _context.Set<Kategori>().AnyAsync(k => k.KategoriId == dto.KategoriId);
            if (!kategoriVarMi)
            {
                return BadRequest($"Gönderilen KategoriId ({dto.KategoriId}) sistemde tanýmlý deðil.");
            }
        }

        // Yeni Ürün Entity nesnesini oluþturup eþliyoruz
        var yeniUrun = new Urunler
        {
            UrunAdi = dto.UrunAdi,
            Fiyat = dto.Fiyat,
            StokMiktari = dto.StokMiktari,
            Aciklamalar = dto.Aciklamalar,
            KategoriId = dto.KategoriId
        };

        await _context.Urunlers.AddAsync(yeniUrun);
        await _context.SaveChangesAsync();

        // 201 Created döndürüyoruz ve yeni eklenen ürünün detay adresi ile objesini veriyoruz
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
    // [Authorize(Roles = "Yönetici")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] UrunGuncelleDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var urun = await _context.Urunlers.FindAsync(id);
        if (urun == null) return NotFound("Güncellenmek istenen ürün bulunamadý.");

        // Kategori doðrulamasý (Eðer kategori deðiþtirilmek istendiyse)
        if (dto.KategoriId.HasValue)
        {
            var kategoriVarMi = await _context.Set<Kategori>().AnyAsync(k => k.KategoriId == dto.KategoriId);
            if (!kategoriVarMi)
            {
                return BadRequest($"Gönderilen KategoriId ({dto.KategoriId}) geçerli bir kategori deðil.");
            }
        }

        // Alanlarý güncelle
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

    // DELETE /api/Urunler/5 -> Ürünü siler
    [HttpDelete("{id}")]
    // [Authorize(Roles = "Yönetici")]
    public async Task<IActionResult> Sil(int id)
    {
        var urun = await _context.Urunlers.FindAsync(id);
        if (urun == null) return NotFound("Silinmek istenen ürün bulunamadý.");

        try
        {
            _context.Urunlers.Remove(urun);
            await _context.SaveChangesAsync();
            return Ok(new { Mesaj = "Ürün menüden baþarýyla silindi." });
        }
        catch (DbUpdateException)
        {
            // ÝLÝÞKÝ KORUMASI: Bu ürün geçmiþ sipariþ detaylarýnda (SiparisDetay) kayýtlýysa SQL hata verir.
            // Bu hatayý yakalayýp kullanýcýya temiz bir dille aktarýyoruz.
            return BadRequest("Bu ürün daha önce sipariþlerde kullanýldýðý için veritabanýndan fiziksel olarak silinemez! Silmek yerine stok miktarýný 0 yapabilir veya açýklamasýna 'Satýþta Deðil' yazabilirsin.");
        }
    }
}