using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.Data;
using Restoran.Data.Entities;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonelController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public PersonelController(DbRestoranContext context)
    {
        _context = context;
    }

    // GET /api/Personel
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // EF Core çoğul adı genelde 'Personels' veya 'Personeller' olur. 

        var personeller = await _context.Personels
           .Where(p => p.IsActive == true)
           .Select(p => new
           {
               p.PersonelId,
               p.PersonelAdi,
               p.PersonelSoyadi,
               p.KullaniciAdi,
               p.PersonelTelefon,
               p.Cinsiyet,
               p.IseBaslamaTarihi,
               p.Maas,
               p.RolId
           })
            .ToListAsync();

        return Ok(personeller);
    }

    // GET /api/Personel/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var personel = await _context.Personels
            .Where(p => p.PersonelId == id)
            .Select(p => new
            {
                p.PersonelId,
                p.PersonelAdi,
                p.PersonelSoyadi,
                p.KullaniciAdi,
                p.PersonelTelefon,
                p.Cinsiyet,
                p.IseBaslamaTarihi,
                p.Maas,
                p.RolId
            })
            .FirstOrDefaultAsync();

        if (personel == null) return NotFound();
        return Ok(personel);
    }

    // POST /api/Personel
    [HttpPost]
    public async Task<IActionResult> PersonelEkle([FromBody] PersonelEkleDto dto)
    {
        if (dto == null) return BadRequest();

        // GÜVENLİK KONTROLÜ: Aynı kullanıcı adına sahip başka personel var mı?
        var kullaniciAdiVarMi = await _context.Personels.AnyAsync(p => p.KullaniciAdi == dto.KullaniciAdi);
        if (kullaniciAdiVarMi) return BadRequest("Bu kullanıcı adı zaten alınmış.");

        // İLİŞKİLİ TABLO KONTROLÜ: Seçilen RolId gerçekten Roller tablosunda var mı?
        if (dto.RolId.HasValue)
        {
            var rolVarMi = await _context.Rollers.AnyAsync(r => r.RolId == dto.RolId);
            if (!rolVarMi) return BadRequest("Seçilen Rol veritabanında tanımlı değil!");
        }

        var personel = new Personel
        {
            PersonelAdi = dto.PersonelAdi,
            PersonelSoyadi = dto.PersonelSoyadi,
            KullaniciAdi = dto.KullaniciAdi,

            // ⛔ BCrypt ile hash'leyerek kaydetme (şimdilik kapalı - AuthController ile uyumlu olması için)
            // PersonelSifre = BCrypt.Net.BCrypt.HashPassword(dto.PersonelSifre), // şifre hash'lenerek saklanır

            // ✅ Düz metin kaydet (aktif)
            PersonelSifre = dto.PersonelSifre,

            PersonelTelefon = dto.PersonelTelefon,
            Cinsiyet = dto.Cinsiyet,
            // Eğer işe başlama tarihi yollanmadıysa bugünün tarihini DateOnly olarak ata
            IseBaslamaTarihi = dto.IseBaslamaTarihi ?? DateOnly.FromDateTime(DateTime.Now),
            Maas = dto.Maas,
            RolId = dto.RolId,
            IsActive = true

        };

        _context.Personels.Add(personel);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Personel başarıyla sisteme eklendi.",
            personel.PersonelId,
            personel.PersonelAdi,
            personel.PersonelSoyadi,
            personel.KullaniciAdi
        });
    }

    // PUT /api/Personel/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] PersonelGuncelleDto dto)
    {
        if (dto == null) return BadRequest();

        var personel = await _context.Personels.FindAsync(id);
        if (personel == null) return NotFound("Güncellenmek istenen personel bulunamadı.");

        // 1. GÜVENLİK KONTROLÜ: Kullanıcı adı değiştiyse, yeni seçilen adın başkasında olmadığından emin olalım
        if (personel.KullaniciAdi != dto.KullaniciAdi)
        {
            var kullaniciAdiVarMi = await _context.Personels.AnyAsync(p => p.KullaniciAdi == dto.KullaniciAdi && p.PersonelId != id);
            if (kullaniciAdiVarMi) return BadRequest("Bu kullanıcı adı başka bir personel tarafından zaten kullanılıyor.");
        }

        // 2. İLİŞKİLİ TABLO KONTROLÜ: Atanmak istenen yeni RolId gerçekten Roller tablosunda mevcut mu?
        if (dto.RolId.HasValue)
        {
            var rolVarMi = await _context.Rollers.AnyAsync(r => r.RolId == dto.RolId);
            if (!rolVarMi) return BadRequest("Seçilen yeni Rol veritabanında tanımlı değil!");
        }

        personel.PersonelAdi = dto.PersonelAdi;
        personel.PersonelSoyadi = dto.PersonelSoyadi;
        personel.KullaniciAdi = dto.KullaniciAdi;
        personel.PersonelTelefon = dto.PersonelTelefon;
        personel.Cinsiyet = dto.Cinsiyet;

        if (dto.IseBaslamaTarihi.HasValue)
            personel.IseBaslamaTarihi = dto.IseBaslamaTarihi;

        personel.Maas = dto.Maas;
        personel.RolId = dto.RolId;

        // Şifre alanı boş gönderilmediyse yeni şifreyi ata
        if (!string.IsNullOrEmpty(dto.PersonelSifre))
        {
            // ⛔ BCrypt ile hash'leyerek kaydetme (şimdilik kapalı - AuthController ile uyumlu olması için)
            // personel.PersonelSifre = BCrypt.Net.BCrypt.HashPassword(dto.PersonelSifre);

            // ✅ Düz metin kaydet (aktif)
            personel.PersonelSifre = dto.PersonelSifre;
        }

        await _context.SaveChangesAsync();
        return Ok(new { Mesaj = "Personel bilgileri başarıyla güncellendi." });
    }

    // DELETE /api/Personel/{id} -> SOFT DELETE: kayıt silinmez, pasife çekilir
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var personel = await _context.Personels.FindAsync(id);
        if (personel == null) return NotFound("Silinmek istenen personel bulunamadı.");

        if (personel.IsActive == false)
            return BadRequest(new { Mesaj = "Bu personel zaten silinmiş (pasif) durumda." });

        personel.IsActive = false;
        personel.SilinmeTarihi = DateTime.Now;
        // Pasif personelin oturumu da düşsün
        personel.RefreshToken = null;
        personel.RefreshTokenBitis = null;

        await _context.SaveChangesAsync();
        return Ok(new { Mesaj = "Personel silindi (pasife alındı).", PersonelId = id });

    }
}