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
        // Altı çizilirse context'indeki isme göre düzeltirsin dayıko.
        var personeller = await _context.Personels
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

        var personel = new Personel
        {
            PersonelAdi = dto.PersonelAdi,
            PersonelSoyadi = dto.PersonelSoyadi,
            KullaniciAdi = dto.KullaniciAdi,
            PersonelSifre = dto.PersonelSifre, // İleride buraya şifre hashleme gelebilir
            PersonelTelefon = dto.PersonelTelefon,
            Cinsiyet = dto.Cinsiyet,
            // Eğer işe başlama tarihi yollanmadıysa bugünün tarihini DateOnly olarak ata
            IseBaslamaTarihi = dto.IseBaslamaTarihi ?? DateOnly.FromDateTime(DateTime.Now),
            Maas = dto.Maas,
            RolId = dto.RolId
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
}