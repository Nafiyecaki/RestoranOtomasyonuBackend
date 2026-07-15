using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.Data;
using Restoran.Data.Entities;

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

    // GET /api/odeme
    [HttpGet]
    // [Authorize(Roles = "Yönetici,Garson,Kurye")]    // kurye kapıda tahsilat yapar!
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

    // GET /api/odeme/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var odeme = await _context.Odemes
            .FindAsync(id);  // FindAsync primary key için daha performanslı

        if (odeme == null)
            return NotFound($"Ödeme ID {id} bulunamadı.");

        return Ok(odeme);
    }

    // POST /api/odeme
    [HttpPost]
    public async Task<IActionResult> OdemeAl([FromBody] OdemeEkleDto dto)
    {
        // 1. Model doğrulama
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // 2. Sipariş kontrolü
        var siparis = await _context.Siparislers.FindAsync(dto.SiparisId);
        if (siparis == null)
            return NotFound("Sipariş bulunamadı.");

        // 3. Sipariş tutarı geçerli mi?
        if (!siparis.ToplamTutar.HasValue || siparis.ToplamTutar.Value <= 0)
            return BadRequest("Siparişin geçerli bir tutarı yok.");

        // 4. Personel geçerli mi?
        var personelVar = await _context.Personels.AnyAsync(p => p.PersonelId == dto.PersonelId);
        if (!personelVar)
            return BadRequest("Geçersiz personel ID.");

        // 5. Kasa geçerli mi?
        var kasaVar = await _context.Kasas.AnyAsync(k => k.KasaId == dto.KasaId);
        if (!kasaVar)
            return BadRequest("Geçersiz kasa ID.");

        // 6. Aynı siparişe daha önce ödeme alınmış mı?
        var odenmis = await _context.Odemes.AnyAsync(o => o.SiparisId == dto.SiparisId);
        if (odenmis)
            return BadRequest("Bu siparişin ödemesi zaten alınmış.");

        // 7. Yeni ödeme kaydı oluştur (tutar siparişten alınır)
        var odeme = new Odeme
        {
            SiparisId = dto.SiparisId,
            OdemeTipi = dto.OdemeTipi,
            OdemeTutari = siparis.ToplamTutar.Value,
            OdemeTarihi = DateTime.UtcNow,          // UTC kullan
            PersonelId = dto.PersonelId,
            KasaId = dto.KasaId
        };
        // 8. Siparişin durumunu "ODENDI" yap
        siparis.SiparisDurumu = "ODENDI";

        _context.Odemes.Add(odeme);
        await _context.SaveChangesAsync();
        _context.Odemes.Add(odeme);
        // 8. Siparişin durumunu "Ödendi" yap
        // 8. Siparişin durumunu "ODENDI" yap
        siparis.SiparisDurumu = "ODENDI";

        _context.Odemes.Add(odeme);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Ödeme alındı.",
            odeme.OdemeId,
            OdenenTutar = odeme.OdemeTutari
        });
    }

    // PUT /api/odeme/{id} -> ödeme bilgilerini güncelle (tutar hariç)
    [HttpPut("{id}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] OdemeEkleDto dto)
    {
        if (dto == null) return BadRequest();

        var odeme = await _context.Odemes.FindAsync(id);
        if (odeme == null) return NotFound(new { Mesaj = "Ödeme bulunamadı." });

        // Personel geçerli mi?
        var personelVar = await _context.Personels.AnyAsync(p => p.PersonelId == dto.PersonelId);
        if (!personelVar) return BadRequest(new { Mesaj = "Geçersiz personel ID." });

        // Kasa geçerli mi?
        var kasaVar = await _context.Kasas.AnyAsync(k => k.KasaId == dto.KasaId);
        if (!kasaVar) return BadRequest(new { Mesaj = "Geçersiz kasa ID." });

        // Sipariş değiştirilemez: ödeme hangi siparişe alındıysa ona bağlı kalır.
        // Yanlış siparişe ödeme alındıysa doğru akış: ödemeyi sil, doğru siparişe yeniden al.
        if (odeme.SiparisId != dto.SiparisId)
            return BadRequest(new { Mesaj = "Ödemenin siparişi değiştirilemez. Ödemeyi silip doğru siparişe yeniden alın." });

        odeme.OdemeTipi = dto.OdemeTipi;   // ör. Nakit -> Kredi Kartı düzeltmesi
        odeme.PersonelId = dto.PersonelId;
        odeme.KasaId = dto.KasaId;
        // OdemeTutari bilerek güncellenmiyor: tutar her zaman siparişten gelir.
        // OdemeTarihi de korunuyor.

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Ödeme güncellendi.",
            odeme.OdemeId,
            odeme.OdemeTipi,
            odeme.OdemeTutari
        });
    }

    // DELETE /api/odeme/{id} -> ödemeyi iptal et, siparişi tekrar ödenmemiş yap
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var odeme = await _context.Odemes.FindAsync(id);
        if (odeme == null) return NotFound(new { Mesaj = "Ödeme bulunamadı." });

        // Siparişin durumunu geri al (ödeme silindi -> tekrar ödeme alınabilir olmalı)
        var siparis = await _context.Siparislers.FindAsync(odeme.SiparisId);
        if (siparis != null && siparis.SiparisDurumu == "ODENDI")
        {
            siparis.SiparisDurumu = "BEKLEMEDE"; // ödeme silindi, tekrar ödeme alınabilir
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