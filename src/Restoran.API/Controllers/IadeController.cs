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
            .OrderByDescending(i => i.IadeTarihi)
            .Select(i => new
            {
                i.IadeId,
                i.IadeTarihi,
                i.IadeSebebi,
                i.IadeDurumu,
                i.IadeTutari,
                i.SiparisDetayId,
                i.UrunId,
                i.PersonelId,
                UrunAdi = i.Urun != null ? i.Urun.UrunAdi : null
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

    [HttpPost]
    public async Task<IActionResult> IadeAl([FromBody] IadeEkleDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.IadeSebebi))
            return BadRequest(new { Mesaj = "İade sebebi boş olamaz." });

        if (dto.IadeTutari <= 0)
            return BadRequest(new { Mesaj = "İade tutarı geçerli olmalı." });

        // SiparisDetayId kontrolü
        if (!dto.SiparisDetayId.HasValue)
            return BadRequest(new { Mesaj = "Sipariş detay ID gerekli." });

        var iade = new Iade
        {
            IadeTarihi = DateTime.Now,
            IadeSebebi = dto.IadeSebebi,
            IadeDurumu = "BEKLEMEDE",
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

        var iade = await _context.Iades
            .Include(i => i.SiparisDetay)
                .ThenInclude(d => d.Siparis)
            .FirstOrDefaultAsync(i => i.IadeId == id);

        if (iade == null) return NotFound(new { Mesaj = "İade kaydı bulunamadı." });

        var gecerliDurumlar = new[] { "BEKLEMEDE", "ONAYLANDI", "REDDEDILDI" };
        var yeniDurum = dto.IadeDurumu?.ToUpper()?.Trim()
            .Replace('İ', 'I').Replace('Ş', 'S').Replace('Ç', 'C');

        if (string.IsNullOrEmpty(yeniDurum) || !gecerliDurumlar.Contains(yeniDurum))
            return BadRequest(new { Mesaj = "Geçersiz iade durumu." });

        if (iade.IadeDurumu == "ONAYLANDI")
            return BadRequest(new { Mesaj = "Zaten onaylanmış bir iade tekrar güncellenemez." });

        // ONAYLANDI ise iade işlemini uygula
        if (yeniDurum == "ONAYLANDI")
        {
            // SiparişDetay üzerinden Sipariş'i ve TÜM detaylarını çek
            var detay = await _context.SiparisDetays
                .Include(d => d.Siparis)
                    .ThenInclude(s => s.SiparisDetays)
                .FirstOrDefaultAsync(d => d.SiparisDetayId == iade.SiparisDetayId);

            if (detay?.Siparis != null)
            {
                // İade edilen ürünü işaretle
                detay.IadeEdildi = true;

                // ToplamTutar'a DOKUNMUYORUZ — orijinal/brüt tutar olarak kalır

                // Siparişteki TÜM ürünler iade edildi mi kontrol et
                bool hepsiIadeEdildi = detay.Siparis.SiparisDetays.All(d => d.IadeEdildi);

                detay.Siparis.SiparisDurumu = hepsiIadeEdildi ? "IADE" : "KISMI_IADE";
            }
        }

        iade.IadeDurumu = yeniDurum;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = $"İade durumu başarıyla '{yeniDurum}' olarak güncellendi.",
            SiparisDurumu = iade.SiparisDetay?.Siparis?.SiparisDurumu,
            IadeTutari = iade.IadeTutari
        });
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