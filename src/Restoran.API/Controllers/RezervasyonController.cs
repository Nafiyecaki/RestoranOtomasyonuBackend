using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.Data;
using Restoran.Data.Entities;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RezervasyonController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public RezervasyonController(DbRestoranContext context)
    {
        _context = context;
    }

    // GET /api/Rezervasyon
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var rezervasyonlar = await _context.Rezervasyons
            .Select(r => new
            {
                r.RezervasyonId,
                r.MusteriAdi,
                r.MusteriSoyadi,
                r.Telefon,
                r.KisiSayisi,
                r.TarihSaat,
                r.Durum,
                r.OlusturulmaTarihi,
                r.Aciklama,
                r.MasaId,
                r.RezervasyonTipi,
                // Senin Masa entity yapına göre burayı güncelledik dayıko!
                MasaNo = r.Masa != null ? r.Masa.MasaNo : null
            })
            .ToListAsync();

        return Ok(rezervasyonlar);
    }

    // GET /api/Rezervasyon/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var rezervasyon = await _context.Rezervasyons
            .Where(r => r.RezervasyonId == id)
            .Select(r => new
            {
                r.RezervasyonId,
                r.MusteriAdi,
                r.MusteriSoyadi,
                r.Telefon,
                r.KisiSayisi,
                r.TarihSaat,
                r.Durum,
                r.OlusturulmaTarihi,
                r.Aciklama,
                r.MasaId,
                r.RezervasyonTipi,
                MasaNo = r.Masa != null ? r.Masa.MasaNo : null
            })
            .FirstOrDefaultAsync();

        if (rezervasyon == null) return NotFound();
        return Ok(rezervasyon);
    }

    // POST /api/Rezervasyon
    [HttpPost]
    public async Task<IActionResult> RezervasyonEkle([FromBody] RezervasyonEkleDto dto)
    {
        if (dto == null) return BadRequest();

        // 1. ZAMAN KONTROLÜ: Rezervasyon geçmiş bir tarihe alınamaz
        if (dto.TarihSaat < DateTime.Now)
        {
            return BadRequest("Geçmiş bir tarihe veya saate rezervasyon oluşturulamaz dayıko.");
        }

        // 2. MASA ENTEGRASYONU & DOĞRULAMA
        if (dto.MasaId.HasValue)
        {
            // MasaId veritabanında var mı kontrolü
            var masa = await _context.Masas.FindAsync(dto.MasaId);
            if (masa == null) return BadRequest("Atanmak istenen masa sistemde bulunamadı.");

            // PRO REZERVASYON ALGORİTMASI: Aynı masaya aynı saat diliminde (±2 saat aralıkla) başka bir aktif rezervasyon var mı?
            var cakismaVarMi = await _context.Rezervasyons.AnyAsync(r =>
                r.MasaId == dto.MasaId &&
                r.Durum != "İptal Edildi" && r.Durum != "Reddedildi" &&
                r.TarihSaat >= dto.TarihSaat.AddHours(-2) &&
                r.TarihSaat <= dto.TarihSaat.AddHours(2)
            );

            if (cakismaVarMi)
            {
                return BadRequest($"{masa.MasaNo} numaralı masa, belirtilen saat aralığında (±2 saat) başka bir müşteriye rezerve edilmiş durumda dayıko.");
            }
        }

        var rezervasyon = new Rezervasyon
        {
            MusteriAdi = dto.MusteriAdi,
            MusteriSoyadi = dto.MusteriSoyadi,
            Telefon = dto.Telefon,
            KisiSayisi = dto.KisiSayisi,
            TarihSaat = dto.TarihSaat,
            Durum = dto.Durum ?? "Beklemede",
            OlusturulmaTarihi = DateTime.Now,
            Aciklama = dto.Aciklama,
            MasaId = dto.MasaId,
            RezervasyonTipi = dto.RezervasyonTipi
        };

        _context.Rezervasyons.Add(rezervasyon);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Rezervasyon başarıyla oluşturuldu.",
            rezervasyon.RezervasyonId,
            rezervasyon.MusteriAdi,
            rezervasyon.TarihSaat,
            rezervasyon.Durum
        });
    }

    // PUT /api/Rezervasyon/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] RezervasyonGuncelleDto dto)
    {
        if (dto == null) return BadRequest();

        var rezervasyon = await _context.Rezervasyons.FindAsync(id);
        if (rezervasyon == null) return NotFound("Güncellenmek istenen rezervasyon kaydı bulunamadı.");

        // 1. ZAMAN KONTROLÜ
        if (dto.TarihSaat < DateTime.Now)
        {
            return BadRequest("Geçmiş bir tarihe güncelleme yapılamaz dayıko.");
        }

        // 2. MASA VE ÇAKIŞMA KONTROLÜ
        if (dto.MasaId.HasValue)
        {
            var masa = await _context.Masas.FindAsync(dto.MasaId);
            if (masa == null) return BadRequest("Atanmak istenen masa sistemde bulunamadı.");

            // Güncellenen masada çakışma kontrolü (Kendisini hariç tutarak: r.RezervasyonId != id)
            var cakismaVarMi = await _context.Rezervasyons.AnyAsync(r =>
                r.MasaId == dto.MasaId &&
                r.RezervasyonId != id &&
                r.Durum != "İptal Edildi" && r.Durum != "Reddedildi" &&
                r.TarihSaat >= dto.TarihSaat.AddHours(-2) &&
                r.TarihSaat <= dto.TarihSaat.AddHours(2)
            );

            if (cakismaVarMi)
            {
                return BadRequest($"{masa.MasaNo} numaralı masa güncellemek istediğiniz saat diliminde (±2 saat) doludur dayıko.");
            }
        }

        rezervasyon.MusteriAdi = dto.MusteriAdi;
        rezervasyon.MusteriSoyadi = dto.MusteriSoyadi;
        rezervasyon.Telefon = dto.Telefon;
        rezervasyon.KisiSayisi = dto.KisiSayisi;
        rezervasyon.TarihSaat = dto.TarihSaat;
        rezervasyon.Durum = dto.Durum ?? rezervasyon.Durum;
        rezervasyon.Aciklama = dto.Aciklama;
        rezervasyon.MasaId = dto.MasaId;
        rezervasyon.RezervasyonTipi = dto.RezervasyonTipi;

        await _context.SaveChangesAsync();
        return Ok(new { Mesaj = "Rezervasyon bilgileri başarıyla güncellendi." });
    }

    // PUT /api/Rezervasyon/{id}/durum
    [HttpPut("{id}/durum")]
    public async Task<IActionResult> DurumGuncelle(int id, [FromBody] RezervasyonDurumGuncelleDto dto)
    {
        if (dto == null) return BadRequest();

        var rezervasyon = await _context.Rezervasyons.FindAsync(id);
        if (rezervasyon == null) return NotFound("Durumu güncellenmek istenen rezervasyon bulunamadı.");

        rezervasyon.Durum = dto.Durum;

        await _context.SaveChangesAsync();
        return Ok(new { Mesaj = $"Rezervasyon durumu başarıyla '{dto.Durum}' olarak güncellendi." });
    }

    // DELETE /api/Rezervasyon/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var rezervasyon = await _context.Rezervasyons.FindAsync(id);
        if (rezervasyon == null) return NotFound("Silinmek istenen rezervasyon kaydı bulunamadı.");

        _context.Rezervasyons.Remove(rezervasyon);
        await _context.SaveChangesAsync();

        return Ok(new { Mesaj = "Rezervasyon sistemden başarıyla kaldırıldı." });
    }
}