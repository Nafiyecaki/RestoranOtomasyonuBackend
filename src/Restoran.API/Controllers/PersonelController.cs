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
        var personeller = await _context.Personels
            .Include(p => p.Rol)  // ← ROL TABLOSUNU DAHİL ET!
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
                p.RolId,
                RolAdi = p.Rol != null ? p.Rol.RolAdi : "Bilinmiyor"  // ← ROL ADI
            })
            .ToListAsync();

        return Ok(personeller);
    }

    // GET /api/Personel/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var personel = await _context.Personels
            .Include(p => p.Rol)  // ← ROL TABLOSUNU DAHİL ET!
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
                p.RolId,
                RolAdi = p.Rol != null ? p.Rol.RolAdi : "Bilinmiyor"  // ← ROL ADI
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

        var kullaniciAdiVarMi = await _context.Personels.AnyAsync(p => p.KullaniciAdi == dto.KullaniciAdi);
        if (kullaniciAdiVarMi) return BadRequest("Bu kullanıcı adı zaten alınmış.");

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
            PersonelSifre = dto.PersonelSifre,
            PersonelTelefon = dto.PersonelTelefon,
            Cinsiyet = dto.Cinsiyet,
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
            personel.KullaniciAdi,
            RolId = personel.RolId
        });
    }

    // PUT /api/Personel/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] PersonelGuncelleDto dto)
    {
        if (dto == null) return BadRequest();

        var personel = await _context.Personels.FindAsync(id);
        if (personel == null) return NotFound("Güncellenmek istenen personel bulunamadı.");

        if (personel.KullaniciAdi != dto.KullaniciAdi)
        {
            var kullaniciAdiVarMi = await _context.Personels.AnyAsync(p => p.KullaniciAdi == dto.KullaniciAdi && p.PersonelId != id);
            if (kullaniciAdiVarMi) return BadRequest("Bu kullanıcı adı başka bir personel tarafından zaten kullanılıyor.");
        }

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

        if (!string.IsNullOrEmpty(dto.PersonelSifre))
        {
            personel.PersonelSifre = dto.PersonelSifre;
        }

        await _context.SaveChangesAsync();
        return Ok(new { Mesaj = "Personel bilgileri başarıyla güncellendi." });
    }

    // DELETE /api/Personel/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var personel = await _context.Personels.FindAsync(id);
        if (personel == null) return NotFound("Silinmek istenen personel bulunamadı.");

        if (personel.IsActive == false)
            return BadRequest(new { Mesaj = "Bu personel zaten silinmiş (pasif) durumda." });

        personel.IsActive = false;
        personel.SilinmeTarihi = DateTime.Now;
        personel.RefreshToken = null;
        personel.RefreshTokenBitis = null;

        await _context.SaveChangesAsync();
        return Ok(new { Mesaj = "Personel silindi (pasife alındı).", PersonelId = id });
    }
}