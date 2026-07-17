// Restoran.API/Controllers/RaporController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.Data;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RaporController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public RaporController(DbRestoranContext context)
    {
        _context = context;
    }

    // GET: api/rapor/gunluk-ciro?tarih=2026-07-16
    [HttpGet("gunluk-ciro")]
    public async Task<IActionResult> GetGunlukCiro([FromQuery] DateTime? tarih)
    {
        DateTime targetDate;

        if (tarih.HasValue)
        {
            // Kullanıcı belirli bir tarih istemişse onu kullan
            targetDate = tarih.Value;
        }
        else
        {
            // Tarih belirtilmemişse, veritabanındaki EN SON kayıtlı siparişin
            // tarihini "bugün" olarak kabul et (gerçek takvim tarihi değil)
            var sonSiparisTarihi = await _context.Siparislers
                .Where(s => s.SiparisTarihi != null)
                .OrderByDescending(s => s.SiparisTarihi)
                .Select(s => s.SiparisTarihi)
                .FirstOrDefaultAsync();

            targetDate = sonSiparisTarihi ?? DateTime.Today;
        }

        var startDate = targetDate.Date;
        var endDate = startDate.AddDays(1);

        var ciro = await _context.Siparislers
            .Where(s => s.SiparisTarihi >= startDate && s.SiparisTarihi < endDate)
            .SumAsync(s => s.ToplamTutar);

        var siparisSayisi = await _context.Siparislers
            .Where(s => s.SiparisTarihi >= startDate && s.SiparisTarihi < endDate)
            .CountAsync();

        var oncekiGunCiro = await _context.Siparislers
            .Where(s => s.SiparisTarihi >= startDate.AddDays(-1) && s.SiparisTarihi < startDate)
            .SumAsync(s => s.ToplamTutar);

        return Ok(new
        {
            tarih = targetDate.ToString("yyyy-MM-dd"),
            ciro = ciro,
            siparisSayisi = siparisSayisi,
            oncekiGunCiro = oncekiGunCiro
        });
    }

    // GET: api/rapor/en-cok-satanlar
    [HttpGet("en-cok-satanlar")]
    public async Task<IActionResult> GetEnCokSatanlar([FromQuery] int gun = 30)
    {
        var sonSiparisTarihi = await _context.Siparislers
            .Where(s => s.SiparisTarihi != null)
            .OrderByDescending(s => s.SiparisTarihi)
            .Select(s => s.SiparisTarihi)
            .FirstOrDefaultAsync();

        var referansTarih = (sonSiparisTarihi ?? DateTime.Today).Date;
        var startDate = referansTarih.AddDays(-gun);

        var result = await _context.SiparisDetays
            .Where(sd => sd.Siparis.SiparisTarihi >= startDate)
            .GroupBy(sd => sd.Urun.UrunAdi)
            .Select(g => new
            {
                urunAdi = g.Key,
                toplamAdet = g.Sum(sd => sd.Adet),
                toplamCiro = g.Sum(sd => sd.Adet * sd.BirimFiyat)
            })
            .OrderByDescending(x => x.toplamCiro)
            .Take(10)
            .ToListAsync();

        return Ok(result);
    }

    // GET: api/rapor/son-siparisler
    [HttpGet("son-siparisler")]
    public async Task<IActionResult> GetSonSiparisler([FromQuery] int adet = 10)
    {
        // 1. ADIM: Ham verileri çek
        var siparisler = await _context.Siparislers
            .Include(s => s.Masa)
            .Include(s => s.SiparisDetays)
                .ThenInclude(sd => sd.Urun)
            .OrderByDescending(s => s.SiparisTarihi)
            .Take(adet)
            .ToListAsync();

        // 2. ADIM: Durum eşleştirme
        var durumMap = new Dictionary<string, string>
        {
            { "BEKLEMEDE", "Bekliyor" },
            { "HAZIRLANIYOR", "Hazırlanıyor" },
            { "HAZIR", "Hazır" },
            { "TESLIM EDILDI", "Teslim Edildi" },
            { "TAMAMLANDI", "Tamamlandı" },
            { "IPTAL", "İptal" },
            { "ODENDI", "Ödendi" }
        };

        // 3. ADIM: Memory'de formatla
        var result = new List<object>();

        foreach (var s in siparisler)
        {
            // ✅ Null kontrolü yap
            var tarih = s.SiparisTarihi ?? DateTime.Now;

            // Detayları formatla
            var detaylar = s.SiparisDetays.ToList();
            var icerik = string.Join(", ", detaylar.Take(2).Select(d => d.Adet + "x " + d.Urun.UrunAdi));
            if (detaylar.Count > 2)
                icerik += "...";

            var item = new
            {
                siparisNo = "#" + s.SiparisId.ToString(),
                masa = s.Masa != null ? "Masa " + s.Masa.MasaNo.ToString() : "Paket",
                icerik = icerik,
                saat = tarih.Hour.ToString("D2") + ":" + tarih.Minute.ToString("D2"),
                tutar = s.ToplamTutar,
                durum = durumMap.ContainsKey(s.SiparisDurumu) ? durumMap[s.SiparisDurumu] : s.SiparisDurumu
            };

            result.Add(item);
        }

        return Ok(result);
    }
}