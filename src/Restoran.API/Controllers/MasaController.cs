using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.Data;
using Restoran.Data.Entities;
using System.Threading.Tasks;

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
            .Select(m => new
            {
                m.MasaId,
                m.MasaNo,
                m.MasaDurumu
            })
            .FirstOrDefaultAsync(m => m.MasaId == id);

        if (masa == null) return NotFound(new { Mesaj = "Masa bulunamadı." });
        return Ok(masa);
    }

    // POST /api/masa
    [HttpPost]
    public async Task<IActionResult> MasaEkle([FromBody] MasaEkleDto dto)
    {
        if (dto == null) return BadRequest("Masa verileri boş olamaz.");

        // Model geçerlilik kontrolü (DTO Validation tetiklenir)
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // GÜVENLİK KONTROLÜ: Aynı masa numarası iki kez tanımlanamaz
        var masaNoVarMi = await _context.Masas.AnyAsync(m => m.MasaNo == dto.MasaNo);
        if (masaNoVarMi)
            return Conflict(new { Mesaj = $"{dto.MasaNo} numaralı masa zaten tanımlı." });

        var masa = new Masa
        {
            MasaNo = dto.MasaNo,
            // Gelen veriyi otomatik olarak "BOS", "DOLU" gibi standart büyük harfe çevirir:
            MasaDurumu = dto.MasaDurumu.Trim().ToUpperInvariant()
        };

        _context.Masas.Add(masa);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Masa başarıyla eklendi.",
            masa.MasaId,
            masa.MasaNo,
            masa.MasaDurumu
        });
    }

    // PUT /api/masa/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] MasaGuncelleDto dto)
    {
        if (dto == null) return BadRequest("Güncelleme verileri boş olamaz.");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var masa = await _context.Masas.FindAsync(id);
        if (masa == null) return NotFound(new { Mesaj = "Güncellenmek istenen masa bulunamadı." });

        // Masa numarası değişiyorsa yeni numara başka masada kullanılıyor mu?
        if (masa.MasaNo != dto.MasaNo)
        {
            var masaNoVarMi = await _context.Masas
                .AnyAsync(m => m.MasaNo == dto.MasaNo && m.MasaId != id);
            if (masaNoVarMi)
                return Conflict(new { Mesaj = $"{dto.MasaNo} numaralı masa zaten tanımlı." });
        }

        masa.MasaNo = dto.MasaNo;
        masa.MasaDurumu = dto.MasaDurumu;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Masa başarıyla güncellendi.",
            masa.MasaId,
            masa.MasaNo,
            masa.MasaDurumu
        });
    }

    // DELETE /api/masa/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var masa = await _context.Masas.FindAsync(id);
        if (masa == null) return NotFound(new { Mesaj = "Masa bulunamadı." });

        // GÜVENLİK KONTROLÜ: Dolu masa silinemez
        if (masa.MasaDurumu == "Dolu")
            return BadRequest(new { Mesaj = "Dolu bir masa silinemez. Önce masayı boşaltın dayıko." });

        // İLİŞKİSEL SİLME KURALI: Sipariş geçmişi olan masa silinemez
        var siparisVarMi = await _context.Siparislers.AnyAsync(s => s.MasaId == id);
        if (siparisVarMi)
            return BadRequest(new { Mesaj = "Bu masaya ait geçmiş sipariş kayıtları var, silinemez." });

        _context.Masas.Remove(masa);
        await _context.SaveChangesAsync();

        return Ok(new { Mesaj = "Masa sistemden başarıyla silindi.", MasaId = id });
    }
}