using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.Data;
using Restoran.Data.Entities;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UyelerController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public UyelerController(DbRestoranContext context)
    {
        _context = context;
    }

    // GET /api/Uyeler
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // EF Core çoğul adlandırmasına göre context içinde 'Uyelers' olarak tanımlanmıştır.
        var uyeler = await _context.Uyelers
            .Select(u => new
            {
                u.UyeId,
                u.UyeAdi,
                u.UyeSoyadi,
                u.UyeTelefon,
                u.UyeEmail,
                u.Cinsiyet,
                u.KayitTarihi
                // PRO TİP: Güvenlik için 'UyeSifre' alanını listelemeye dahil etmedik dayıko!
            })
            .ToListAsync();

        return Ok(uyeler);
    }

    // GET /api/Uyeler/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var uye = await _context.Uyelers
            .Where(u => u.UyeId == id)
            .Select(u => new
            {
                u.UyeId,
                u.UyeAdi,
                u.UyeSoyadi,
                u.UyeTelefon,
                u.UyeEmail,
                u.Cinsiyet,
                u.KayitTarihi
            })
            .FirstOrDefaultAsync();

        if (uye == null) return NotFound();
        return Ok(uye);
    }

    // POST /api/Uyeler
    [HttpPost]
    public async Task<IActionResult> UyeEkle([FromBody] UyeEkleDto dto)
    {
        if (dto == null) return BadRequest();

        // GÜVENLİK KONTROLÜ: Aynı e-posta adresiyle başka bir üye var mı?
        var emailVarMi = await _context.Uyelers.AnyAsync(u => u.UyeEmail == dto.UyeEmail);
        if (emailVarMi) return BadRequest("Bu e-posta adresiyle kayıtlı bir üye zaten mevcut.");

        var uye = new Uyeler
        {
            UyeAdi = dto.UyeAdi,
            UyeSoyadi = dto.UyeSoyadi,
            UyeTelefon = dto.UyeTelefon,
            UyeEmail = dto.UyeEmail,
            UyeSifre = dto.UyeSifre, // İleride buraya MD5/SHA256 şifre hashleme eklenebilir
            Cinsiyet = dto.Cinsiyet,
            KayitTarihi = DateTime.Now // Kayıt anındaki sistem saati otomatik basılıyor
        };

        _context.Uyelers.Add(uye);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Üye kaydı başarıyla tamamlandı.",
            uye.UyeId,
            uye.UyeAdi,
            uye.UyeSoyadi,
            uye.UyeEmail
        });
    }

    // PUT /api/Uyeler/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] UyeGuncelleDto dto)
    {
        if (dto == null) return BadRequest();

        var uye = await _context.Uyelers.FindAsync(id);
        if (uye == null) return NotFound("Güncellenmek istenen üye bulunamadı.");

        // GÜVENLİK KONTROLÜ: E-posta değiştiyse, yeni yazılan e-postanın başkasında olmadığından emin olalım
        if (uye.UyeEmail != dto.UyeEmail)
        {
            var emailVarMi = await _context.Uyelers.AnyAsync(u => u.UyeEmail == dto.UyeEmail && u.UyeId != id);
            if (emailVarMi) return BadRequest("Bu e-posta adresi başka bir üye tarafından zaten kullanılıyor.");
        }

        uye.UyeAdi = dto.UyeAdi;
        uye.UyeSoyadi = dto.UyeSoyadi;
        uye.UyeTelefon = dto.UyeTelefon;
        uye.UyeEmail = dto.UyeEmail;
        uye.Cinsiyet = dto.Cinsiyet;

        // Şifre alanı boş gönderilmediyse yeni şifreyi ata
        if (!string.IsNullOrEmpty(dto.UyeSifre))
        {
            uye.UyeSifre = dto.UyeSifre;
        }

        await _context.SaveChangesAsync();
        return Ok(new { Mesaj = "Üye bilgileri başarıyla güncellendi." });
    }

    // DELETE /api/Uyeler/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var uye = await _context.Uyelers.FindAsync(id);
        if (uye == null) return NotFound("Silinmek istenen üye bulunamadı.");

        // İLİŞKİLİ VERİ TABLOSU GÜVENLİK KORUMASI:
        // Eğer üyenin geçmişe dönük sipariş veya ödeme kaydı varsa SQL Server silmeye izin vermez.
        // Bunu try-catch ile yakalayıp API'nin çökmesini engelliyoruz.
        try
        {
            _context.Uyelers.Remove(uye);
            await _context.SaveChangesAsync();
            return Ok(new { Mesaj = "Üye sistemden başarıyla silindi." });
        }
        catch (DbUpdateException)
        {
            return BadRequest("Bu üyenin geçmişe dönük işlem kayıtları (Sipariş, Ödeme, Rezervasyon vb.) olduğu için doğrudan silinemez dayıko.");
        }
    }
}