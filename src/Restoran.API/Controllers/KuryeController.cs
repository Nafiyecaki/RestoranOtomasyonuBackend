using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.Data;
using Restoran.Data.Entities;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KuryeController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public KuryeController(DbRestoranContext context)
    {
        _context = context;
    }

    [HttpGet]
    // GET /api/Kurye/listele
    // Rolü "Kurye" olan personelleri listeler
    [HttpGet("listele")]
    public async Task<IActionResult> GetKuryeler()
    {
        var kuryeler = await _context.Personels
            .Include(p => p.Rol)
            .Where(p => p.Rol != null
                     && p.Rol.RolAdi.ToLower() == "kurye")
            .Select(p => new KuryeDto
            {
                PersonelId = p.PersonelId,
                AdSoyad = p.PersonelAdi + " " + p.PersonelSoyadi,
                Telefon = p.PersonelTelefon,
                IsActive = true
            })
            .ToListAsync();

        return Ok(kuryeler);
    }

    // POST /api/Kurye/siparis-ata
    // Restoran panelinden siparişi kuryeye atar
    [HttpPost("siparis-ata")]
    public async Task<IActionResult> SiparisAta([FromBody] KuryeAtaDto dto)
    {
        if (dto == null) return BadRequest();

        var siparis = await _context.Siparislers.FindAsync(dto.SiparisId);
        if (siparis == null) return NotFound("Sipariş bulunamadı.");

        var kurye = await _context.Personels.FindAsync(dto.PersonelId);
        if (kurye == null) return NotFound("Kurye (Personel) bulunamadı.");

        siparis.PersonelId = kurye.PersonelId;
        siparis.SiparisDurumu = "Yolda";

        await _context.SaveChangesAsync();

        return Ok(new { Mesaj = $"Sipariş #{siparis.SiparisId}, {kurye.PersonelAdi} isimli kuryeye başarıyla atandı." });
    }

    // GET /api/Kurye/{personelId}/aktif-siparisler
    // Kuryenin üzerindeki teslim edilmemiş "Yolda" olan siparişleri ve müşteri adres bilgilerini getirir
    [HttpGet("{personelId}/aktif-siparisler")]
    public async Task<IActionResult> GetKuryeAktifSiparisleri(int personelId)
    {
        var siparisler = await _context.Siparislers
            .Include(s => s.Uye)
                .ThenInclude(u => u!.Adres)
            .Where(s => s.PersonelId == personelId && s.SiparisDurumu == "Yolda")
            .Select(s => new KuryeSiparisDto
            {
                SiparisId = s.SiparisId,
                SiparisDurumu = s.SiparisDurumu,
                ToplamTutar = s.ToplamTutar,
                SiparisTarihi = s.SiparisTarihi,
                MusteriAdSoyad = s.Uye != null ? s.Uye.UyeAdi + " " + s.Uye.UyeSoyadi : "Misafir Müşteri",
                MusteriTelefon = s.Uye != null ? s.Uye.UyeTelefon : "Telefon Bulunamadı",
                AcikAdres = s.Uye != null && s.Uye.Adres.Any()
                            ? s.Uye.Adres.FirstOrDefault()!.AcikAdres
                            : "Adres Bulunamadı"
            })
            .ToListAsync();

        return Ok(siparisler);
    }

    // PUT /api/Kurye/teslim-et/{siparisId}
    // Kurye siparişi teslim ettiğinde durumunu günceller
    [HttpPut("teslim-et/{siparisId}")]
    public async Task<IActionResult> SiparisTeslimEt(int siparisId)
    {
        var siparis = await _context.Siparislers.FindAsync(siparisId);
        if (siparis == null) return NotFound("Sipariş bulunamadı.");

        siparis.SiparisDurumu = "Teslim Edildi";
        await _context.SaveChangesAsync();

        return Ok(new { Mesaj = "Sipariş başarıyla teslim edildi olarak güncellendi." });
    }
}