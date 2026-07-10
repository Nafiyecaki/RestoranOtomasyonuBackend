using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.Data;
using Restoran.Data.Entities;
using Restoran.API.Dtos;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SiparislerController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public SiparislerController(DbRestoranContext context)
    {
        _context = context;
    }

    // POST /api/siparisler -> yeni sipariş oluşturur
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] SiparisOlusturDto dto)
    {
        if (dto.Detaylar == null || !dto.Detaylar.Any())
            return BadRequest("Sipariş oluşturmak için en az bir ürün eklemelisiniz.");

        // Sipariş başlığını oluştur
        var siparis = new Siparisler
        {
            SiparisTarihi = DateTime.Now,
            SiparisDurumu = "BEKLEMEDE",
            SiparisTipi = dto.SiparisTipi ?? "SALON",
            UyeId = dto.UyeId,
            MasaId = dto.MasaId,
            PersonelId = dto.PersonelId,
            ToplamTutar = 0
        };

        // Detayları ekle, fiyatı veritabanından al, toplamı hesapla
        foreach (var d in dto.Detaylar)
        {
            var urun = await _context.Urunlers.FindAsync(d.UrunId);
            if (urun == null)
                return NotFound($"ID'si {d.UrunId} olan ürün sistemde bulunamadı.");

            int adet = d.Adet <= 0 ? 1 : d.Adet;

            siparis.SiparisDetays.Add(new SiparisDetay
            {
                UrunId = d.UrunId,
                Adet = adet,
                BirimFiyat = urun.Fiyat,
                DetayNot = d.DetayNot
            });

            siparis.ToplamTutar += adet * urun.Fiyat;
        }

        // Sipariş + detaylar tek SaveChanges ile kaydedilir (EF ilişkiyi kendisi kurar)
        await _context.Siparislers.AddAsync(siparis);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Sipariş ve detayları başarıyla oluşturuldu.",
            SiparisId = siparis.SiparisId,
            HesaplananToplamTutar = siparis.ToplamTutar
        });
    }

    // GET /api/siparisler/5 -> tek siparişi detaylarıyla getirir
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var siparis = await _context.Siparislers
            .Where(s => s.SiparisId == id)
            .Select(s => new
            {
                s.SiparisId,
                s.SiparisDurumu,
                s.SiparisTipi,
                s.ToplamTutar,
                s.SiparisTarihi,
                Masa = s.Masa != null ? s.Masa.MasaNo : null,
                Detaylar = s.SiparisDetays.Select(d => new
                {
                    d.UrunId,
                    UrunAdi = d.Urun.UrunAdi,
                    d.Adet,
                    d.BirimFiyat,
                    SatirToplami = d.Adet * d.BirimFiyat,
                    d.DetayNot
                })
            })
            .FirstOrDefaultAsync();

        if (siparis == null) return NotFound();
        return Ok(siparis);
    }
}