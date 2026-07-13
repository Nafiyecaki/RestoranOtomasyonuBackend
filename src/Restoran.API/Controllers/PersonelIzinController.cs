using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.Data;
using Restoran.Data.Entities;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonelIzinController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public PersonelIzinController(DbRestoranContext context)
    {
        _context = context;
    }

    // GET /api/PersonelIzin
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var izinler = await _context.PersonelIzins
            .Select(i => new
            {
                i.IzinId,
                i.IzinBaslangic,
                i.IzinBitis,
                i.IzinDurumu,
                i.IzinAciklamasi,
                i.PersonelId
            })
            .ToListAsync();

        return Ok(izinler);
    }

    // GET /api/PersonelIzin/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var izin = await _context.PersonelIzins
            .Where(i => i.IzinId == id)
            .Select(i => new
            {
                i.IzinId,
                i.IzinBaslangic,
                i.IzinBitis,
                i.IzinDurumu,
                i.IzinAciklamasi,
                i.PersonelId
            })
            .FirstOrDefaultAsync();

        if (izin == null) return NotFound();
        return Ok(izin);
    }

    // POST /api/PersonelIzin
    [HttpPost]
    public async Task<IActionResult> IzinEkle([FromBody] PersonelIzinEkleDto dto)
    {
        if (dto == null) return BadRequest();

        // MANTIK KONTROLÜ: Bitiş tarihi başlangıçtan önce olamaz
        if (dto.IzinBitis < dto.IzinBaslangic)
        {
            return BadRequest("İzin bitiş tarihi, başlangıç tarihinden önce olamaz dayıko.");
        }

        // GÜVENLİK KONTROLÜ: İzin yazılacak personel sistemde var mı?
        if (dto.PersonelId.HasValue)
        {
            var personelVarMi = await _context.Personels.AnyAsync(p => p.PersonelId == dto.PersonelId);
            if (!personelVarMi) return NotFound("İzin tanımlanmak istenen personel bulunamadı.");
        }

        var personelIzin = new PersonelIzin
        {
            IzinBaslangic = dto.IzinBaslangic,
            IzinBitis = dto.IzinBitis,
            IzinDurumu = dto.IzinDurumu,
            IzinAciklamasi = dto.IzinAciklamasi,
            PersonelId = dto.PersonelId
        };

        _context.PersonelIzins.Add(personelIzin);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Personel izni başarıyla sisteme işlendi.",
            personelIzin.IzinId,
            personelIzin.PersonelId,
            personelIzin.IzinBaslangic,
            personelIzin.IzinBitis
        });
    }
}