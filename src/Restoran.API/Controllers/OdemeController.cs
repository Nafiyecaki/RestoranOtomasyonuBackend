using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.Data;
using Restoran.Data.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

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

    // GET /api/odeme -> Tüm ödemeleri getirir
    [HttpGet]
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

    // GET /api/odeme/5 -> Id ile tek ödeme getirir
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var odeme = await _context.Odemes.FindAsync(id);

        if (odeme == null)
            return NotFound($"Ödeme ID {id} bulunamadı.");

        return Ok(odeme);
    }

    // ============================================================
    // ✅ MEVCUT: SiparisId ile ödeme al
    // ============================================================
    [HttpPost("odeme-al")]
    public async Task<IActionResult> OdemeAl([FromBody] OdemeEkleDto dto)
    {
        if (dto == null)
            return BadRequest(new { Mesaj = "Ödeme verileri boş olamaz." });

        if (string.IsNullOrWhiteSpace(dto.OdemeTipi))
            return BadRequest(new { Mesaj = "Ödeme tipi boş olamaz." });

        var gecerliOdemeTipleri = new[] { "NAKIT", "KREDI KARTI", "ONLINE", "KAPIDA NAKIT" };
        var odemeTipi = dto.OdemeTipi.Trim().ToUpper(new System.Globalization.CultureInfo("tr-TR"));
        if (!gecerliOdemeTipleri.Contains(odemeTipi))
            return BadRequest(new { Mesaj = "Geçersiz ödeme tipi. Geçerli değerler: " + string.Join(", ", gecerliOdemeTipleri) });

        var siparis = await _context.Siparislers.FindAsync(dto.SiparisId);
        if (siparis == null)
            return NotFound(new { Mesaj = "Sipariş bulunamadı." });

        if (siparis.SiparisDurumu == "IPTAL")
            return BadRequest(new { Mesaj = "İptal edilmiş bir siparişe ödeme alınamaz." });

        if (siparis.SiparisDurumu == "ODENDI")
            return BadRequest(new { Mesaj = "Bu siparişin ödemesi zaten alınmış." });

        if (!siparis.ToplamTutar.HasValue || siparis.ToplamTutar.Value <= 0)
            return BadRequest(new { Mesaj = "Siparişin geçerli bir tutarı yok." });

        int personelId = dto.PersonelId.HasValue && dto.PersonelId.Value > 0 ? dto.PersonelId.Value : 1;
        int kasaId = dto.KasaId.HasValue && dto.KasaId.Value > 0 ? dto.KasaId.Value : 1;

        var personelVar = await _context.Personels.AnyAsync(p => p.PersonelId == personelId);
        if (!personelVar)
            return BadRequest(new { Mesaj = "Geçersiz personel ID." });

        var odenmis = await _context.Odemes.AnyAsync(o => o.SiparisId == dto.SiparisId);
        if (odenmis)
            return BadRequest(new { Mesaj = "Bu siparişin ödemesi zaten alınmış." });

        var odeme = new Odeme
        {
            SiparisId = dto.SiparisId,
            OdemeTipi = odemeTipi,
            OdemeTutari = siparis.ToplamTutar.Value,
            OdemeTarihi = DateTime.Now,
            PersonelId = personelId,
            KasaId = kasaId
        };

        siparis.SiparisDurumu = "ODENDI";

        if (siparis.MasaId.HasValue)
        {
            var masa = await _context.Masas.FindAsync(siparis.MasaId.Value);
            if (masa != null) masa.MasaDurumu = "BOŞ";

            var eskiAcikSiparisler = await _context.Siparislers
                .Where(s => s.MasaId == siparis.MasaId.Value &&
                            s.SiparisId != dto.SiparisId &&
                            s.SiparisDurumu != "ODENDI")
                .ToListAsync();

            foreach (var item in eskiAcikSiparisler)
                item.SiparisDurumu = "IPTAL";
        }

        _context.Odemes.Add(odeme);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Ödeme başarıyla alındı. Masa boşa çıkarıldı.",
            odeme.OdemeId,
            OdenenTutar = odeme.OdemeTutari,
            SiparisId = dto.SiparisId
        });
    }

    // ============================================================
    // ✅ YENİ: MasaId ile ödeme al (Garson paneli için)
    // ============================================================
    [HttpPost("masa-odeme")]
    public async Task<IActionResult> MasaOdemeAl([FromBody] OdemeEkleDto dto)
    {
        if (dto == null)
            return BadRequest(new { Mesaj = "Ödeme verileri boş olamaz." });

        if (string.IsNullOrWhiteSpace(dto.OdemeTipi))
            return BadRequest(new { Mesaj = "Ödeme tipi boş olamaz." });

        // Ödeme tipi kontrolü
        var gecerliOdemeTipleri = new[] { "NAKIT", "KREDI KARTI", "ONLINE", "KAPIDA NAKIT" };
        var odemeTipi = dto.OdemeTipi.Trim().ToUpper(new System.Globalization.CultureInfo("tr-TR"));
        if (!gecerliOdemeTipleri.Contains(odemeTipi))
            return BadRequest(new { Mesaj = "Geçersiz ödeme tipi. Geçerli değerler: " + string.Join(", ", gecerliOdemeTipleri) });

        // ⚠️ ÖNEMLİ: MasaId'yi dto'dan al (SiparisId yerine MasaId kullan)
        if (!dto.MasaId.HasValue || dto.MasaId.Value <= 0)
            return BadRequest(new { Mesaj = "Geçersiz masa ID." });

        // Masaya ait açık siparişi bul
        var siparis = await _context.Siparislers
            .FirstOrDefaultAsync(s => s.MasaId == dto.MasaId.Value &&
                                      s.SiparisDurumu != "ODENDI" &&
                                      s.SiparisDurumu != "IPTAL" &&
                                      s.SiparisDurumu != "IADE");

        if (siparis == null)
            return NotFound(new { Mesaj = "Bu masada ödenecek aktif sipariş bulunamadı." });

        if (siparis.ToplamTutar == null || siparis.ToplamTutar <= 0)
            return BadRequest(new { Mesaj = "Sipariş tutarı geçersiz (0 TL)." });

        int personelId = dto.PersonelId.HasValue && dto.PersonelId.Value > 0 ? dto.PersonelId.Value : 1;
        int kasaId = dto.KasaId.HasValue && dto.KasaId.Value > 0 ? dto.KasaId.Value : 1;

        // Personel kontrolü
        var personelVar = await _context.Personels.AnyAsync(p => p.PersonelId == personelId);
        if (!personelVar)
            return BadRequest(new { Mesaj = "Geçersiz personel ID." });

        // Tekrar ödeme kontrolü
        var odenmis = await _context.Odemes.AnyAsync(o => o.SiparisId == siparis.SiparisId);
        if (odenmis)
            return BadRequest(new { Mesaj = "Bu siparişin ödemesi zaten alınmış." });

        // Ödeme kaydını oluştur
        var odeme = new Odeme
        {
            SiparisId = siparis.SiparisId,
            OdemeTipi = odemeTipi,
            OdemeTutari = siparis.ToplamTutar.Value,
            OdemeTarihi = DateTime.Now,
            PersonelId = personelId,
            KasaId = kasaId
        };

        // Sipariş durumunu güncelle
        siparis.SiparisDurumu = "ODENDI";

        // Masayı boşalt
        var masa = await _context.Masas.FindAsync(dto.MasaId.Value);
        if (masa != null)
        {
            masa.MasaDurumu = "BOŞ";
        }

        // Aynı masadaki diğer açık siparişleri iptal et
        var eskiAcikSiparisler = await _context.Siparislers
            .Where(s => s.MasaId == dto.MasaId.Value &&
                        s.SiparisId != siparis.SiparisId &&
                        s.SiparisDurumu != "ODENDI" &&
                        s.SiparisDurumu != "IPTAL")
            .ToListAsync();

        foreach (var item in eskiAcikSiparisler)
        {
            item.SiparisDurumu = "IPTAL";
        }

        _context.Odemes.Add(odeme);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Ödeme başarıyla alındı. Masa boşa çıkarıldı.",
            OdemeId = odeme.OdemeId,
            OdenenTutar = odeme.OdemeTutari,
            SiparisId = siparis.SiparisId,
            MasaId = dto.MasaId.Value
        });
    }

    // PUT /api/odeme/{id} -> Ödeme güncelle
    [HttpPut("{id}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] OdemeGuncelleDto dto)
    {
        if (dto == null) return BadRequest("Güncelleme verileri boş olamaz.");

        if (string.IsNullOrWhiteSpace(dto.OdemeTipi))
            return BadRequest(new { Mesaj = "Ödeme tipi boş olamaz." });

        var odeme = await _context.Odemes.FindAsync(id);
        if (odeme == null) return NotFound(new { Mesaj = "Ödeme bulunamadı." });

        odeme.OdemeTipi = dto.OdemeTipi.Trim();
        if (dto.PersonelId.HasValue) odeme.PersonelId = dto.PersonelId.Value;
        if (dto.KasaId.HasValue) odeme.KasaId = dto.KasaId.Value;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Ödeme başarıyla güncellendi.",
            odeme.OdemeId,
            odeme.OdemeTipi,
            odeme.OdemeTutari
        });
    }

    // DELETE /api/odeme/{id} -> Ödemeyi iptal et
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var odeme = await _context.Odemes.FindAsync(id);
        if (odeme == null) return NotFound(new { Mesaj = "Ödeme bulunamadı." });

        var siparis = await _context.Siparislers.FindAsync(odeme.SiparisId);
        if (siparis != null && siparis.SiparisDurumu == "ODENDI")
        {
            siparis.SiparisDurumu = "BEKLEMEDE";
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