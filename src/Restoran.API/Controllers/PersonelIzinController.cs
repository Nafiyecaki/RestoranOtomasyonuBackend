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

    // GET /api/PersonelIzin/personel/3
    // Belirli bir personelin tüm izin geçmişini listeler (Filtreleme)
    [HttpGet("personel/{personelId}")]
    public async Task<IActionResult> GetByPersonelId(int personelId)
    {
        var personelVarMi = await _context.Personels.AnyAsync(p => p.PersonelId == personelId);
        if (!personelVarMi) return NotFound("İzin geçmişi aranacak personel sistemde bulunamadı.");

        var izinler = await _context.PersonelIzins
            .Where(i => i.PersonelId == personelId)
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

    // PUT /api/PersonelIzin/{id}
    // İzin bilgilerini tamamen güncellemek için
    [HttpPut("{id}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] PersonelIzinGuncelleDto dto)
    {
        if (dto == null) return BadRequest();

        var izin = await _context.PersonelIzins.FindAsync(id);
        if (izin == null) return NotFound("Güncellenmek istenen izin kaydı bulunamadı.");

        // MANTIK KONTROLÜ: Güncellenen bitiş tarihi başlangıçtan önce olamaz
        if (dto.IzinBitis < dto.IzinBaslangic)
        {
            return BadRequest("İzin bitiş tarihi, başlangıç tarihinden önce olamaz dayıko.");
        }

        // GÜVENLİK KONTROLÜ: Yeni atanacak personel veritabanında mevcut mu?
        if (dto.PersonelId.HasValue)
        {
            var personelVarMi = await _context.Personels.AnyAsync(p => p.PersonelId == dto.PersonelId);
            if (!personelVarMi) return NotFound("İzin atanmak istenen yeni personel bulunamadı.");
        }

        izin.IzinBaslangic = dto.IzinBaslangic;
        izin.IzinBitis = dto.IzinBitis;
        izin.IzinDurumu = dto.IzinDurumu;
        izin.IzinAciklamasi = dto.IzinAciklamasi;
        izin.PersonelId = dto.PersonelId;

        await _context.SaveChangesAsync();
        return Ok(new { Mesaj = "İzin bilgileri başarıyla güncellendi." });
    }

    // PUT /api/PersonelIzin/{id}/durum
    // Sadece izin durumunu (Onaylandı, Reddedildi, Beklemede) güncellemek için (Yönetici Onay Paneli)
    [HttpPut("{id}/durum")]
    public async Task<IActionResult> DurumGuncelle(int id, [FromBody] IzinDurumGuncelleDto dto)
    {
        if (dto == null) return BadRequest();

        var izin = await _context.PersonelIzins.FindAsync(id);
        if (izin == null) return NotFound("Durumu güncellenmek istenen izin kaydı bulunamadı.");

        izin.IzinDurumu = dto.IzinDurumu;

        await _context.SaveChangesAsync();
        return Ok(new { Mesaj = $"İzin durumu başarıyla '{dto.IzinDurumu}' olarak güncellendi." });
    }

    // DELETE /api/PersonelIzin/{id}
    // İzin talebini tamamen iptal etmek/silmek için
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var izin = await _context.PersonelIzins.FindAsync(id);
        if (izin == null) return NotFound("Silinmek istenen izin kaydı bulunamadı.");

        _context.PersonelIzins.Remove(izin);
        await _context.SaveChangesAsync();

        return Ok(new { Mesaj = "İzin kaydı sistemden başarıyla silindi." });
    }
}