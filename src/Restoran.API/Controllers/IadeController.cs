using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.Data;
using Restoran.Data.Entities;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IadeController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public IadeController(DbRestoranContext context)
    {
        _context = context;
    }

    // GET /api/Iade
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var iadeler = await _context.Iades
            .Select(i => new
            {
                i.IadeId,
                i.IadeTarihi,
                i.IadeSebebi,
                i.IadeDurumu,
                i.IadeTutari,
                i.SiparisDetayId,
                i.UrunId,
                i.PersonelId
            })
            .ToListAsync();

        return Ok(iadeler);
    }

    // GET /api/Iade/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var iade = await _context.Iades
            .Where(i => i.IadeId == id)
            .Select(i => new
            {
                i.IadeId,
                i.IadeTarihi,
                i.IadeSebebi,
                i.IadeDurumu,
                i.IadeTutari,
                i.SiparisDetayId,
                i.UrunId,
                i.PersonelId
            })
            .FirstOrDefaultAsync();

        if (iade == null) return NotFound();
        return Ok(iade);
    }

    // POST /api/Iade
    [HttpPost]
    public async Task<IActionResult> IadeAl([FromBody] IadeEkleDto dto)
    {
        if (dto == null) return BadRequest();

        var iade = new Iade
        {
            IadeTarihi = DateTime.Now, // İade zamanı otomatik sistem saati atanıyor
            IadeSebebi = dto.IadeSebebi,
            IadeDurumu = dto.IadeDurumu,
            IadeTutari = dto.IadeTutari,
            SiparisDetayId = dto.SiparisDetayId,
            UrunId = dto.UrunId,
            PersonelId = dto.PersonelId
        };

        _context.Iades.Add(iade);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "İade işlemi başarıyla kaydedildi.",
            iade.IadeId,
            iade.IadeTutari,
            iade.IadeTarihi
        });
    }

    [HttpPut("{id}/durum")]
    public async Task<IActionResult> DurumGuncelle(int id, [FromBody] IadeDurumGuncelleDto dto)
    {
        if (dto == null) return BadRequest();

        var iade = await _context.Iades.FindAsync(id);
        if (iade == null) return NotFound("İade kaydı bulunamadı.");

        // Sadece tanımlı durumlar kabul edilir
        var gecerliDurumlar = new[] { "BEKLEMEDE", "ONAYLANDI", "REDDEDILDI" };
        var yeniDurum = dto.IadeDurumu?.ToUpper()?.Trim()
            .Replace('İ', 'I').Replace('Ş', 'S').Replace('Ç', 'C');
        if (string.IsNullOrEmpty(yeniDurum) || !gecerliDurumlar.Contains(yeniDurum))
            return BadRequest(new { Mesaj = "Geçersiz iade durumu. Geçerli değerler: " + string.Join(", ", gecerliDurumlar) });

        iade.IadeDurumu = yeniDurum;
        await _context.SaveChangesAsync();

        return Ok(new { Mesaj = $"İade durumu başarıyla '{yeniDurum}' olarak güncellendi." });
    }


    // DELETE /api/Iade/{id}
    // Hatalı girilen bir iade kaydını sistemden tamamen kaldırmak veya iptal etmek için
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var iade = await _context.Iades.FindAsync(id);
        if (iade == null) return NotFound("Silinmek istenen iade kaydı bulunamadı.");

        _context.Iades.Remove(iade);
        await _context.SaveChangesAsync();

        return Ok(new { Mesaj = "İade kaydı sistemden başarıyla silindi." });
    }

}