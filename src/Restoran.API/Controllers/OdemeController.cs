using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.Data;
using Restoran.Data.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OdemeController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public OdemeController(DbRestoranContext context)
    {
        _context = context;
    }

    // GET /api/odeme -> Tüm ödemeleri getirir
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var odemeler = await _context.Odemes
            .Select(o => new
            {
                o.OdemeId,
                o.OdemeTipi,
                o.OdemeTutari,
                o.OdemeTarihi,
                o.PersonelId,
                o.SiparisId,
                o.KasaId
            })
            .ToListAsync();

        return Ok(odemeler);
    }

    // GET /api/odeme/5 -> Id ile tek ödeme getirir
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var odeme = await _context.Odemes.FindAsync(id);

        if (odeme == null)
            return NotFound($"Ödeme ID {id} bulunamadı.");

        return Ok(odeme);
    }

    // POST /api/odeme VE POST /api/odeme/odeme-al -> Ödeme Alır, Masayı Boşaltır ve Eski Siparişleri Temizler
    [HttpPost]
    [HttpPost("odeme-al")]
    public async Task<IActionResult> OdemeAl([FromBody] OdemeEkleDto dto)
    {
        // 1. Model doğrulama
        if (dto == null)
            return BadRequest(new { Mesaj = "Ödeme verileri boş olamaz." });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(dto.OdemeTipi))
            return BadRequest(new { Mesaj = "Ödeme tipi boş olamaz." });

        // 2. Sipariş kontrolü
        var siparis = await _context.Siparislers.FindAsync(dto.SiparisId);
        if (siparis == null)
            return NotFound(new { Mesaj = "Sipariş bulunamadı." });

        // 3. Sipariş tutarı geçerli mi?
        if (!siparis.ToplamTutar.HasValue || siparis.ToplamTutar.Value <= 0)
            return BadRequest(new { Mesaj = "Siparişin geçerli bir tutarı yok." });

        // 4. Personel geçerli mi? (Atlanan durumlarda varsayılan personel atanır)
        int personelId = dto.PersonelId ?? 1;
        var personelVar = await _context.Personels.AnyAsync(p => p.PersonelId == personelId);
        if (!personelVar)
            return BadRequest(new { Mesaj = "Geçersiz personel ID." });

        // 5. Kasa geçerli mi?
        int kasaId = dto.KasaId ?? 1;
        var kasaVar = await _context.Kasas.AnyAsync(k => k.KasaId == kasaId);
        if (!kasaVar)
            return BadRequest(new { Mesaj = "Geçersiz kasa ID." });

        // 6. Aynı siparişe daha önce ödeme alınmış mı?
        var odenmis = await _context.Odemes.AnyAsync(o => o.SiparisId == dto.SiparisId);
        if (odenmis)
            return BadRequest(new { Mesaj = "Bu siparişin ödemesi zaten alınmış." });

        // 7. Yeni ödeme kaydı oluştur (Tutar siparişten çekilir)
        var odeme = new Odeme
        {
            SiparisId = dto.SiparisId,
            OdemeTipi = dto.OdemeTipi.Trim().ToUpper(),
            OdemeTutari = siparis.ToplamTutar.Value,
            OdemeTarihi = DateTime.Now,
            PersonelId = personelId,
            KasaId = kasaId
        };

        // 8. Siparişin durumunu "ODENDI" yap
        siparis.SiparisDurumu = "ODENDI";

        // 9. Masayı BOŞ yap ve masadaki kapatılmamış diğer siparişleri IPTAL durumuna çek
        if (siparis.MasaId.HasValue)
        {
            var masa = await _context.Masas.FindAsync(siparis.MasaId.Value);
            if (masa != null)
            {
                masa.MasaDurumu = "BOŞ";
            }

            // Masada kalmış diğer eski/açık siparişleri IPTAL'e çek
            var digerAciklar = await _context.Siparislers
                .Where(s => s.MasaId == siparis.MasaId.Value &&
                            s.SiparisId != dto.SiparisId &&
                            s.SiparisDurumu != "ODENDI")
                .ToListAsync();

            foreach (var item in digerAciklar)
            {
                item.SiparisDurumu = "IPTAL";
            }
        }

        _context.Odemes.Add(odeme);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Ödeme başarıyla alındı. Masa boşa çıkarıldı.",
            odeme.OdemeId,
            OdenenTutar = odeme.OdemeTutari,
            SiparisId = dto.SiparisId
        });
    }

    // PUT /api/odeme/{id} -> Ödeme bilgilerini güncelle
    [HttpPut("{id}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] OdemeGuncelleDto dto)
    {
        if (dto == null) return BadRequest("Güncelleme verileri boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.OdemeTipi))
            return BadRequest(new { Mesaj = "Ödeme tipi boş olamaz." });

        var odeme = await _context.Odemes.FindAsync(id);
        if (odeme == null) return NotFound(new { Mesaj = "Ödeme bulunamadı." });

        var personelVar = await _context.Personels.AnyAsync(p => p.PersonelId == dto.PersonelId);
        if (!personelVar) return BadRequest(new { Mesaj = "Geçersiz personel ID." });

        var kasaVar = await _context.Kasas.AnyAsync(k => k.KasaId == dto.KasaId);
        if (!kasaVar) return BadRequest(new { Mesaj = "Geçersiz kasa ID." });

        odeme.OdemeTipi = dto.OdemeTipi.Trim();
        odeme.PersonelId = dto.PersonelId;
        odeme.KasaId = dto.KasaId;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Ödeme güncellendi.",
            odeme.OdemeId,
            odeme.OdemeTipi,
            odeme.OdemeTutari
        });
    }

    // DELETE /api/odeme/{id} -> Ödemeyi iptal et, siparişi tekrar BEKLEMEDE yap
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var odeme = await _context.Odemes.FindAsync(id);
        if (odeme == null) return NotFound(new { Mesaj = "Ödeme bulunamadı." });

        var siparis = await _context.Siparislers.FindAsync(odeme.SiparisId);
        if (siparis != null && siparis.SiparisDurumu == "ODENDI")
        {
            siparis.SiparisDurumu = "BEKLEMEDE";
        }

        _context.Odemes.Remove(odeme);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Ödeme silindi, sipariş tekrar ödenmemiş duruma alındı.",
            OdemeId = id,
            odeme.SiparisId
        });
    }
}