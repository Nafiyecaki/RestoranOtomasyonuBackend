using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.Data;
using Restoran.Data.Entities;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IadeController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public IadeController(DbRestoranContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var iadeler = await _context.Iades
            .OrderByDescending(i => i.IadeTarihi)
            .Select(i => new
            {
                i.IadeId,
                i.IadeTarihi,
                i.IadeSebebi,
                i.IadeDurumu,
                i.IadeTutari,
                i.SiparisDetayId,
                i.UrunId,
                i.PersonelId,
                UrunAdi = i.Urun != null ? i.Urun.UrunAdi : null
            })
            .ToListAsync();

        return Ok(iadeler);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var iade = await _context.Iades
            .Where(i => i.IadeId == id)
            .Select(i => new
            {
                i.IadeId,
                i.IadeTarihi,
                i.IadeSebebi,
                i.IadeDurumu,
                i.IadeTutari,
                i.SiparisDetayId,
                i.UrunId,
                i.PersonelId
            })
            .FirstOrDefaultAsync();

        if (iade == null) return NotFound();
        return Ok(iade);
    }

    [HttpPost("siparis-iade")]
    public async Task<IActionResult> IadeAl([FromBody] IadeEkleDto dto)
    {
        if (dto == null)
            return BadRequest(new { Mesaj = "İade verileri boş olamaz." });

        if (string.IsNullOrWhiteSpace(dto.IadeSebebi))
            return BadRequest(new { Mesaj = "İade sebebi boş olamaz." });

        if (dto.IadeTutari <= 0)
            return BadRequest(new { Mesaj = "İade tutarı geçerli olmalı." });

        int? siparisId = null;

        if (dto.SiparisDetayId.HasValue)
        {
            var detay = await _context.SiparisDetays
                .Include(d => d.Siparis)
                .FirstOrDefaultAsync(d => d.SiparisDetayId == dto.SiparisDetayId.Value);

            if (detay == null)
                return NotFound(new { Mesaj = "İlgili sipariş kalemi bulunamadı." });

            siparisId = detay.SiparisId;

            var dahaOnceIadeEdilmis = await _context.Iades
                .AnyAsync(i => i.SiparisDetayId == dto.SiparisDetayId && i.IadeDurumu != "REDDEDILDI");
            if (dahaOnceIadeEdilmis)
                return BadRequest(new { Mesaj = "Bu ürün için zaten bir iade kaydı var." });
        }
        else if (dto.SiparisId.HasValue && dto.UrunId.HasValue)
        {
            var detay = await _context.SiparisDetays
                .FirstOrDefaultAsync(d => d.SiparisId == dto.SiparisId.Value && d.UrunId == dto.UrunId.Value);

            if (detay != null)
            {
                dto.SiparisDetayId = detay.SiparisDetayId;
                siparisId = dto.SiparisId;
            }
            else
            {
                siparisId = dto.SiparisId;
            }
        }

        var gecerliDurumlar = new[] { "BEKLEMEDE", "ONAYLANDI", "REDDEDILDI" };
        var durum = (dto.IadeDurumu ?? "BEKLEMEDE").ToUpper().Trim()
            .Replace('İ', 'I').Replace('Ş', 'S').Replace('Ç', 'C');
        if (!gecerliDurumlar.Contains(durum))
            return BadRequest(new { Mesaj = "Geçersiz iade durumu." });

        var iade = new Iade
        {
            IadeTarihi = DateTime.Now,
            IadeSebebi = dto.IadeSebebi,
            IadeDurumu = durum,
            IadeTutari = dto.IadeTutari,
            SiparisDetayId = dto.SiparisDetayId,
            UrunId = dto.UrunId,
            PersonelId = dto.PersonelId ?? 1
        };

        _context.Iades.Add(iade);

        if (durum == "ONAYLANDI" && (dto.SiparisDetayId.HasValue || siparisId.HasValue))
        {
            if (dto.SiparisDetayId.HasValue)
            {
                var siparisDetay = await _context.SiparisDetays
                    .Include(d => d.Siparis)
                        .ThenInclude(s => s.SiparisDetays)
                    .FirstOrDefaultAsync(d => d.SiparisDetayId == dto.SiparisDetayId);

                if (siparisDetay?.Siparis != null)
                {
                    var eskiTutar = siparisDetay.Siparis.ToplamTutar ?? 0;
                    var yeniTutar = Math.Max(0, eskiTutar - iade.IadeTutari);

                    siparisDetay.Siparis.ToplamTutar = yeniTutar;
                    siparisDetay.IadeEdildi = true;

                    if (yeniTutar <= 0)
                    {
                        siparisDetay.Siparis.SiparisDurumu = "IADE";

                        // ✅ MASA DURUMUNU BOŞ YAP
                        if (siparisDetay.Siparis.MasaId.HasValue)
                        {
                            var masa = await _context.Masas.FindAsync(siparisDetay.Siparis.MasaId.Value);
                            if (masa != null)
                            {
                                masa.MasaDurumu = "BOŞ";
                            }
                        }
                    }
                    else
                    {
                        siparisDetay.Siparis.SiparisDurumu = "KISMI_IADE";
                    }
                }
            }
            else if (siparisId.HasValue)
            {
                var siparis = await _context.Siparislers
                    .Include(s => s.SiparisDetays)
                    .FirstOrDefaultAsync(s => s.SiparisId == siparisId.Value);

                if (siparis != null)
                {
                    var eskiTutar = siparis.ToplamTutar ?? 0;
                    var yeniTutar = Math.Max(0, eskiTutar - iade.IadeTutari);

                    siparis.ToplamTutar = yeniTutar;

                    foreach (var detay in siparis.SiparisDetays)
                    {
                        detay.IadeEdildi = true;
                    }

                    if (yeniTutar <= 0)
                    {
                        siparis.SiparisDurumu = "IADE";

                        // ✅ MASA DURUMUNU BOŞ YAP
                        if (siparis.MasaId.HasValue)
                        {
                            var masa = await _context.Masas.FindAsync(siparis.MasaId.Value);
                            if (masa != null)
                            {
                                masa.MasaDurumu = "BOŞ";
                            }
                        }
                    }
                    else
                    {
                        siparis.SiparisDurumu = "KISMI_IADE";
                    }
                }
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = durum == "ONAYLANDI"
                ? "İade işlemi başarıyla onaylandı ve cirodan düşüldü."
                : "İade işlemi başarıyla kaydedildi, onay bekliyor.",
            iade.IadeId,
            iade.IadeTutari,
            iade.IadeTarihi,
            iade.IadeDurumu
        });
    }

    [HttpPut("{id}/durum")]
    public async Task<IActionResult> DurumGuncelle(int id, [FromBody] IadeDurumGuncelleDto dto)
    {
        if (dto == null) return BadRequest();

        var iade = await _context.Iades
            .Include(i => i.SiparisDetay)
                .ThenInclude(d => d.Siparis)
            .FirstOrDefaultAsync(i => i.IadeId == id);

        if (iade == null) return NotFound(new { Mesaj = "İade kaydı bulunamadı." });

        var gecerliDurumlar = new[] { "BEKLEMEDE", "ONAYLANDI", "REDDEDILDI" };
        var yeniDurum = dto.IadeDurumu?.ToUpper()?.Trim()
            .Replace('İ', 'I').Replace('Ş', 'S').Replace('Ç', 'C');

        if (string.IsNullOrEmpty(yeniDurum) || !gecerliDurumlar.Contains(yeniDurum))
            return BadRequest(new { Mesaj = "Geçersiz iade durumu." });

        if (iade.IadeDurumu == "ONAYLANDI")
            return BadRequest(new { Mesaj = "Zaten onaylanmış bir iade tekrar güncellenemez." });

        if (yeniDurum == "ONAYLANDI" && iade.SiparisDetayId.HasValue)
        {
            var detay = await _context.SiparisDetays
                .Include(d => d.Siparis)
                    .ThenInclude(s => s.SiparisDetays)
                .FirstOrDefaultAsync(d => d.SiparisDetayId == iade.SiparisDetayId);

            if (detay?.Siparis != null)
            {
                var eskiTutar = detay.Siparis.ToplamTutar ?? 0;
                var yeniTutar = Math.Max(0, eskiTutar - iade.IadeTutari);

                detay.Siparis.ToplamTutar = yeniTutar;
                detay.IadeEdildi = true;

                if (yeniTutar <= 0)
                {
                    detay.Siparis.SiparisDurumu = "IADE";

                    // ✅ MASA DURUMUNU BOŞ YAP
                    if (detay.Siparis.MasaId.HasValue)
                    {
                        var masa = await _context.Masas.FindAsync(detay.Siparis.MasaId.Value);
                        if (masa != null)
                        {
                            masa.MasaDurumu = "BOŞ";
                        }
                    }
                }
                else
                {
                    detay.Siparis.SiparisDurumu = "KISMI_IADE";
                }
            }
        }

        iade.IadeDurumu = yeniDurum;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = $"İade durumu başarıyla '{yeniDurum}' olarak güncellendi.",
            YeniToplamTutar = iade.SiparisDetay?.Siparis?.ToplamTutar ?? 0,
            SiparisDurumu = iade.SiparisDetay?.Siparis?.SiparisDurumu,
            IadeTutari = iade.IadeTutari
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var iade = await _context.Iades.FindAsync(id);
        if (iade == null) return NotFound("Silinmek istenen iade kaydı bulunamadı.");

        _context.Iades.Remove(iade);
        await _context.SaveChangesAsync();

        return Ok(new { Mesaj = "İade kaydı sistemden başarıyla silindi." });
    }
}