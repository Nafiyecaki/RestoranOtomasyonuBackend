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

    // GET /api/Iade
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

    // GET /api/Iade/{id}
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

    // POST /api/Iade veya POST /api/Iade/siparis-iade
    [HttpPost]
    [HttpPost("siparis-iade")]
    public async Task<IActionResult> IadeAl([FromBody] IadeEkleDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.IadeSebebi))
            return BadRequest(new { Mesaj = "İade sebebi boş olamaz." });

        if (dto.IadeTutari <= 0)
            return BadRequest(new { Mesaj = "İade tutarı geçerli olmalı." });

        // SiparisDetayId verilmişse gerçekten var mı kontrol et
        SiparisDetay? detay = null;
        if (dto.SiparisDetayId.HasValue)
        {
            detay = await _context.SiparisDetays.FindAsync(dto.SiparisDetayId.Value);
            if (detay == null)
                return NotFound(new { Mesaj = "İlgili sipariş kalemi bulunamadı." });

            // Aynı kalem daha önce iade edilmiş mi?
            var dahaOnceIadeEdilmis = await _context.Iades
                .AnyAsync(i => i.SiparisDetayId == dto.SiparisDetayId && i.IadeDurumu != "REDDEDILDI");
            if (dahaOnceIadeEdilmis)
                return BadRequest(new { Mesaj = "Bu ürün için zaten bir iade kaydı var." });
        }

        // Durum sabit listeyle doğrulanıyor (DurumGuncelle ile tutarlı)
        var gecerliDurumlar = new[] { "BEKLEMEDE", "ONAYLANDI", "REDDEDILDI" };
        var durum = (dto.IadeDurumu ?? "BEKLEMEDE").ToUpper().Trim()
            .Replace('İ', 'I').Replace('Ş', 'S').Replace('Ç', 'C');
        if (!gecerliDurumlar.Contains(durum))
            return BadRequest(new { Mesaj = "Geçersiz iade durumu." });

        var iade = new Iade
        {
            IadeTarihi = DateTime.Now,
            IadeSebebi = dto.IadeSebebi,
            IadeDurumu = "BEKLEMEDE",
            IadeTutari = dto.IadeTutari,
            SiparisDetayId = dto.SiparisDetayId,
            UrunId = dto.UrunId,
            PersonelId = dto.PersonelId
        };

        _context.Iades.Add(iade);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "İade işlemi başarıyla kaydedildi.",
            iade.IadeId,
            iade.IadeTutari,
            iade.IadeTarihi
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
                // SADECE iade edilen ürünün tutarını düş (TÜM SİPARİŞİ DEĞİL!)
                var eskiTutar = detay.Siparis.ToplamTutar ?? 0;
                var yeniTutar = Math.Max(0, eskiTutar - iade.IadeTutari);

                detay.Siparis.ToplamTutar = yeniTutar; // SADECE iade edilen ürünü düş
                detay.IadeEdildi = true;

                Console.WriteLine($"📊 Sipariş #{detay.SiparisId} - Eski: {eskiTutar} - İade: {iade.IadeTutari} - Yeni: {yeniTutar}");

                // Sipariş durumunu güncelle
                if (yeniTutar <= 0)
                {
                    detay.Siparis.SiparisDurumu = "IADE";
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

    

    // DELETE /api/Iade/{id}
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