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
        var uyeler = await _context.Uyelers
            .Include(u => u.Adres)
            .Select(u => new
            {
                u.UyeId,
                u.UyeAdi,
                u.UyeSoyadi,
                u.UyeTelefon,
                u.UyeEmail,
                Cinsiyet = u.Cinsiyet ?? string.Empty,
                KayitTarihi = u.KayitTarihi,
                IsActive = u.IsActive ?? false,
                Adresler = u.Adres.Select(a => new
                {
                    a.AdresId,              // ✅ Büyük harf ile
                    a.AdresTipi,
                    a.AcikAdres,
                    a.TeslimatBolgesindeMi,
                    a.UyeId                 // ✅ Büyük harf ile (UyeId  , UyelD değil!)
                }).ToList()
            })
            .ToListAsync();

        return Ok(uyeler);
    }

    // GET /api/Uyeler/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var uye = await _context.Uyelers
            .Include(u => u.Adres)
            .Where(u => u.UyeId == id)
            .Select(u => new
            {
                u.UyeId,
                u.UyeAdi,
                u.UyeSoyadi,
                u.UyeTelefon,
                u.UyeEmail,
                u.Cinsiyet,
                u.KayitTarihi,
                u.IsActive,
                Adresler = u.Adres.Select(a => new
                {
                    a.AdresId,              // ✅ Büyük harf ile
                    a.AdresTipi,
                    a.AcikAdres,
                    a.TeslimatBolgesindeMi,
                    a.UyeId                 // ✅ Büyük harf ile
                }).ToList()
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

        var emailVarMi = await _context.Uyelers.AnyAsync(u => u.UyeEmail == dto.UyeEmail);
        if (emailVarMi) return BadRequest("Bu e-posta adresiyle kayıtlı bir üye zaten mevcut.");

        var uye = new Uyeler
        {
            UyeAdi = dto.UyeAdi,
            UyeSoyadi = dto.UyeSoyadi,
            UyeTelefon = dto.UyeTelefon,
            UyeEmail = dto.UyeEmail,
            UyeSifre = dto.UyeSifre,
            Cinsiyet = dto.Cinsiyet,
            KayitTarihi = DateTime.Now,
            IsActive = true
        };

        // ✅ Adres ekleme
        if (!string.IsNullOrEmpty(dto.AcikAdres))
        {
            var adres = new Adres
            {
                AdresTipi = dto.AdresTipi ?? "Varsayılan",
                AcikAdres = dto.AcikAdres,
                TeslimatBolgesindeMi = dto.TeslimatBolgesindeMi ?? false,
                UyeId= uye.UyeId  // ✅ UyeID (UyelD değil!)
            };
            uye.Adres.Add(adres);
        }

        _context.Uyelers.Add(uye);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Üye kaydı başarıyla tamamlandı.",
            uye.UyeId,
            uye.UyeAdi,
            uye.UyeSoyadi,
            uye.UyeEmail,
            Adres = uye.Adres.Select(a => new
            {
                a.AdresId,
                a.AdresTipi,
                a.AcikAdres,
                a.TeslimatBolgesindeMi,
                a.UyeId
            }).FirstOrDefault()
        });
    }

    // PUT /api/Uyeler/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] UyeGuncelleDto dto)
    {
        if (dto == null) return BadRequest();

        var uye = await _context.Uyelers
            .Include(u => u.Adres)
            .FirstOrDefaultAsync(u => u.UyeId == id);

        if (uye == null) return NotFound("Güncellenmek istenen üye bulunamadı.");

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

        if (!string.IsNullOrEmpty(dto.UyeSifre))
        {
            uye.UyeSifre = dto.UyeSifre;
        }

        if (dto.IsActive.HasValue)
        {
            uye.IsActive = dto.IsActive.Value;
        }

        // ✅ Adres güncelleme
        if (!string.IsNullOrEmpty(dto.AcikAdres))
        {
            var mevcutAdres = uye.Adres.FirstOrDefault();

            if (mevcutAdres != null)
            {
                mevcutAdres.AdresTipi = dto.AdresTipi ?? mevcutAdres.AdresTipi;
                mevcutAdres.AcikAdres = dto.AcikAdres ?? mevcutAdres.AcikAdres;
                if (dto.TeslimatBolgesindeMi.HasValue)
                {
                    mevcutAdres.TeslimatBolgesindeMi = dto.TeslimatBolgesindeMi.Value;
                }
            }
            else
            {
                var yeniAdres = new Adres
                {
                    AdresTipi = dto.AdresTipi ?? "Varsayılan",
                    AcikAdres = dto.AcikAdres,
                    TeslimatBolgesindeMi = dto.TeslimatBolgesindeMi ?? false,
                    UyeId = uye.UyeId  // ✅ UyeID
                };
                uye.Adres.Add(yeniAdres);
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { Mesaj = "Üye bilgileri başarıyla güncellendi." });
    }

    // DELETE /api/Uyeler/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var uye = await _context.Uyelers
            .Include(u => u.Adres)
            .FirstOrDefaultAsync(u => u.UyeId == id);

        if (uye == null) return NotFound("Silinmek istenen üye bulunamadı.");

        uye.IsActive = false;
        await _context.SaveChangesAsync();
        return Ok(new { Mesaj = "Üye pasif hale getirildi.", UyeId = id });
    }
} 