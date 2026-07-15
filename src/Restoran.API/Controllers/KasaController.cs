using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.Data;
using Restoran.Data.Entities;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KasaController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public KasaController(DbRestoranContext context)
    {
        _context = context;
    }

    // GET /api/kasa
    [HttpGet]
   // [Authorize(Roles = "Yönetici")]                 // kasayı açma/kapatma yönetici işi
    public async Task<IActionResult> GetAll()
    {
        var kasalar = await _context.Kasas
             .Select(k => new
             {
                 k.KasaId,
                 k.AcilisTarihi,
                 k.KapanisTarihi,
                 k.KasaDurumu,
                 k.PersonelId,
                 k.AcilisBakiyesi,
                 k.KapanisBakiyesi
             })
             .ToListAsync();

        return Ok(kasalar);
    }

    // GET /api/kasa/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var kasa = await _context.Kasas
            .Where(k => k.KasaId == id)
            .Select(k => new
            {
                k.KasaId,
                k.AcilisTarihi,
                k.KapanisTarihi,
                k.KasaDurumu,
                k.PersonelId
            })
            .FirstOrDefaultAsync();

        if (kasa == null) return NotFound();
        return Ok(kasa);
    }

    // POST /api/kasa
    [HttpPost]
    public async Task<IActionResult> KasaAc([FromBody] KasaEkleDto dto)
    {
        if (dto == null) return BadRequest();

        // GÜVENLİK KONTROLÜ: Zaten "Açık" olan bir kasa varsa, yenisini açmayı engelliyoruz
        var acikKasaVarMi = await _context.Kasas.AnyAsync(k => k.KasaDurumu == "Açık");
        if (acikKasaVarMi) return BadRequest("Şu anda zaten açık bir kasa bulunuyor. Yeni kasa açmadan önce mevcut olanı kapatmalısınız.");

        var kasa = new Kasa
        {
            AcilisTarihi = DateTime.Now,  // Kasa şu an açılıyor
            KapanisTarihi = null,         // Yeni açıldığı için kapanış henüz yok
            KasaDurumu = dto.KasaDurumu,  // "Açık"
            PersonelId = dto.PersonelId,
            AcilisBakiyesi = dto.AcilisBakiyesi ?? 0
        };

        _context.Kasas.Add(kasa);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Kasa başarıyla açıldı.",
            kasa.KasaId,
            kasa.AcilisTarihi,
            kasa.KasaDurumu,
            kasa.PersonelId
        });
    }
    // PUT /api/kasa/{id}/kapat
    [HttpPut("{id}/kapat")]
    //[Authorize(Roles = "Yönetici")]
    public async Task<IActionResult> KasaKapat(int id, [FromBody] KasaKapatDto dto)
    {
        var kasa = await _context.Kasas.FindAsync(id);
        if (kasa == null) return NotFound(new { Mesaj = "Kasa bulunamadı." });

        // GÜVENLİK KONTROLÜ: Zaten kapalı bir kasa tekrar kapatılamaz
        if (kasa.KasaDurumu == "Kapalı")
            return BadRequest(new { Mesaj = "Bu kasa zaten kapalı." });

        kasa.KasaDurumu = "Kapalı";
        kasa.KapanisTarihi = DateTime.Now;
        kasa.KapanisBakiyesi = dto?.KapanisBakiyesi; // gün sonu sayım tutarı

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Kasa başarıyla kapatıldı.",
            kasa.KasaId,
            kasa.AcilisTarihi,
            kasa.KapanisTarihi,
            kasa.AcilisBakiyesi,
            kasa.KapanisBakiyesi
        });
    }

    // PUT /api/kasa/{id}
    [HttpPut("{id}")]
   // [Authorize(Roles = "Yönetici")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] KasaEkleDto dto)
    {
        if (dto == null) return BadRequest();

        var kasa = await _context.Kasas.FindAsync(id);
        if (kasa == null) return NotFound(new { Mesaj = "Kasa bulunamadı." });

        kasa.KasaDurumu = dto.KasaDurumu;
        kasa.PersonelId = dto.PersonelId;
        // Açılış/kapanış tarihleri elle değiştirilmiyor; kapatma için /kapat endpoint'i kullanılmalı

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Kasa bilgileri güncellendi.",
            kasa.KasaId,
            kasa.KasaDurumu,
            kasa.PersonelId
        });
    }

    // DELETE /api/kasa/{id}
    [HttpDelete("{id}")]
   // [Authorize(Roles = "Yönetici")]
    public async Task<IActionResult> Sil(int id)
    {
        var kasa = await _context.Kasas.FindAsync(id);
        if (kasa == null) return NotFound(new { Mesaj = "Kasa bulunamadı." });

        // GÜVENLİK KONTROLÜ: Açık kasa silinemez, önce kapatılmalı
        if (kasa.KasaDurumu == "Açık")
            return BadRequest(new { Mesaj = "Açık bir kasa silinemez. Önce kasayı kapatın." });

        _context.Kasas.Remove(kasa);
        await _context.SaveChangesAsync();

        return Ok(new { Mesaj = "Kasa kaydı silindi.", KasaId = id });
    }
}