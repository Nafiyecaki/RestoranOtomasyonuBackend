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
        if (dto == null || string.IsNullOrWhiteSpace(dto.IadeSebebi))
            return BadRequest(new { Mesaj = "İade sebebi boş olamaz." });

        if (dto.IadeTutari <= 0)
            return BadRequest(new { Mesaj = "İade tutarı geçerli olmalı." });

        SiparisDetay? detay = null;
        if (dto.SiparisDetayId.HasValue)
        {
            detay = await _context.SiparisDetays.FindAsync(dto.SiparisDetayId.Value);
            if (detay == null)
                return NotFound(new { Mesaj = "İlgili sipariş kalemi bulunamadı." });

            var dahaOnceIadeEdilmis = await _context.Iades
                .AnyAsync(i => i.SiparisDetayId == dto.SiparisDetayId && i.IadeDurumu != "REDDEDILDI");
            if (dahaOnceIadeEdilmis)
                return BadRequest(new { Mesaj = "Bu ürün için zaten bir iade kaydı var." });
        }

        var gecerliDurumlar = new[] { "BEKLEMEDE", "ONAYLANDI", "REDDEDILDI" };
        var durum = (dto.IadeDurumu ?? "BEKLEMEDE").ToUpper().Trim()
            .Replace('İ', 'I').Replace('Ş', 'S').Replace('Ç', 'C');
        if (!gecerliDurumlar.Contains(durum))
            return BadRequest(new { Mesaj = "Geçersiz iade durumu." });

        var iade = new Iade
        {
            IadeTarihi = DateTime.Now,
            IadeSebebi = dto.IadeSebebi,
            IadeDurumu = durum,
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

    // ============================================================
    // ✅ SİPARİŞ BAZLI TOPLU İADE ENDPOINT'İ
    // ============================================================
    [HttpPost("siparis-iade")]
    public async Task<IActionResult> SiparisIade([FromBody] SiparisIadeDto dto)
    {
        if (dto == null || dto.SiparisId <= 0)
            return BadRequest(new { Mesaj = "Geçersiz sipariş ID." });

        if (string.IsNullOrWhiteSpace(dto.IadeSebebi))
            return BadRequest(new { Mesaj = "İade sebebi boş olamaz." });

        // 1. Siparişi bul
        var siparis = await _context.Siparislers
            .Include(s => s.Odemes)
            .Include(s => s.SiparisDetays)
            .ThenInclude(d => d.Urun)
            .FirstOrDefaultAsync(s => s.SiparisId == dto.SiparisId);

        if (siparis == null)
            return NotFound(new { Mesaj = "Sipariş bulunamadı." });

        // 2. Sipariş ödenmiş mi kontrol et
        if (siparis.SiparisDurumu != "ODENDI")
            return BadRequest(new { Mesaj = "Bu sipariş henüz ödenmemiş. İade için önce ödeme alınmalı." });

        // 3. Ödeme kaydını bul
        var odeme = siparis.Odemes.FirstOrDefault();
        if (odeme == null)
            return BadRequest(new { Mesaj = "Bu siparişe ait ödeme kaydı bulunamadı." });

        // 4. Sipariş detaylarını iade et
        foreach (var detay in siparis.SiparisDetays)
        {
            var iade = new Iade
            {
                IadeTarihi = DateTime.Now,
                IadeSebebi = dto.IadeSebebi,
                IadeDurumu = "ONAYLANDI",
                IadeTutari = detay.Adet * detay.BirimFiyat,
                SiparisDetayId = detay.SiparisDetayId,
                UrunId = detay.UrunId,
                PersonelId = dto.PersonelId
            };
            _context.Iades.Add(iade);
        }

        // 5. Ödemeyi sil
        _context.Odemes.Remove(odeme);

        // 6. Sipariş durumunu güncelle
        siparis.SiparisDurumu = "IADE";
        siparis.ToplamTutar = 0;

        // 7. Masayı boşalt
        if (siparis.MasaId.HasValue)
        {
            var masa = await _context.Masas.FindAsync(siparis.MasaId.Value);
            if (masa != null) masa.MasaDurumu = "BOŞ";
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Sipariş başarıyla iade edildi.",
            SiparisId = dto.SiparisId,
            IadeTutari = odeme.OdemeTutari
        });
    }

    [HttpPut("{id}/durum")]
    public async Task<IActionResult> DurumGuncelle(int id, [FromBody] IadeDurumGuncelleDto dto)
    {
        if (dto == null) return BadRequest();

        var iade = await _context.Iades.FindAsync(id);
        if (iade == null) return NotFound(new { Mesaj = "İade kaydı bulunamadı." });

        var gecerliDurumlar = new[] { "BEKLEMEDE", "ONAYLANDI", "REDDEDILDI" };
        var yeniDurum = dto.IadeDurumu?.ToUpper()?.Trim()
            .Replace('İ', 'I').Replace('Ş', 'S').Replace('Ç', 'C');
        if (string.IsNullOrEmpty(yeniDurum) || !gecerliDurumlar.Contains(yeniDurum))
            return BadRequest(new { Mesaj = "Geçersiz iade durumu. Geçerli değerler: " + string.Join(", ", gecerliDurumlar) });

        if (iade.IadeDurumu == "ONAYLANDI")
            return BadRequest(new { Mesaj = "Zaten onaylanmış bir iade tekrar güncellenemez." });

        if (yeniDurum == "ONAYLANDI" && iade.SiparisDetayId.HasValue)
        {
            var detay = await _context.SiparisDetays
                .Include(d => d.Siparis)
                .FirstOrDefaultAsync(d => d.SiparisDetayId == iade.SiparisDetayId.Value);

            if (detay?.Siparis != null && detay.Siparis.ToplamTutar.HasValue)
            {
                detay.Siparis.ToplamTutar = Math.Max(0, detay.Siparis.ToplamTutar.Value - iade.IadeTutari);
            }
        }

        iade.IadeDurumu = yeniDurum;
        await _context.SaveChangesAsync();

        return Ok(new { Mesaj = $"İade durumu başarıyla '{yeniDurum}' olarak güncellendi." });
    }

    // DELETE /api/Iade/{id}
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