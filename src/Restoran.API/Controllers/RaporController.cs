// Restoran.API/Controllers/RaporController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        DateTime referansTarih;

        if (tarih.HasValue)
        {
            // Kullanıcı belirli bir tarih istemişse onu kullan
            referansTarih = tarih.Value;
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

            referansTarih = sonSiparisTarihi ?? DateTime.Today;
        }

        var baslangicTarihi = referansTarih.Date;
        var bitisTarihi = baslangicTarihi.AddDays(1);

        var ciro = await _context.Siparislers
            .Where(s => s.SiparisTarihi >= baslangicTarihi && s.SiparisTarihi < bitisTarihi)
            .SumAsync(s => s.ToplamTutar);

        var siparisSayisi = await _context.Siparislers
            .Where(s => s.SiparisTarihi >= baslangicTarihi && s.SiparisTarihi < bitisTarihi)
            .CountAsync();

        var oncekiGunCiro = await _context.Siparislers
            .Where(s => s.SiparisTarihi >= baslangicTarihi.AddDays(-1) && s.SiparisTarihi < baslangicTarihi)
            .SumAsync(s => s.ToplamTutar);

        return Ok(new
        {
            tarih = referansTarih.ToString("yyyy-MM-dd"),
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
        var baslangicTarihi = referansTarih.AddDays(-gun);

        var result = await _context.SiparisDetays
            .Where(sd => sd.Siparis.SiparisTarihi >= baslangicTarihi)
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
            { "ODENDI", "Ödendi" },
            { "IADE", "İade" }
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

    [HttpGet("gunluk-satis")]
    public async Task<IActionResult> GetGunlukSatis([FromQuery] DateTime? tarih)
    {
        try
        {
            var referansTarih = tarih ?? DateTime.Today;
            var baslangicTarih = referansTarih.Date;
            var bitisTarihi = referansTarih.Date.AddDays(1);


            var siparisler = await _context.Siparislers
                .Where(s => s.SiparisTarihi >= baslangicTarih && s.SiparisTarihi < bitisTarihi)
                .Select(s => new
                {
                    s.SiparisId,
                    s.SiparisTarihi,
                    s.ToplamTutar,
                    s.SiparisDurumu,
                    s.SiparisTipi,
                    MasaNo = s.Masa != null ? s.Masa.MasaNo : null,
                    uyeAdi = s.Uye != null ? s.Uye.UyeAdi + " " + s.Uye.UyeSoyadi : null,

                    UrunSayisi = s.SiparisDetays.Count
                })
                .OrderByDescending(s => s.SiparisTarihi)
            .ToListAsync();

            var toplamCiro = siparisler.Sum(s => s.ToplamTutar);
            var toplamSiparis = siparisler.Count;

            return Ok(new
            {
                tarih = referansTarih.ToString("yyyy-MM-dd"),
                toplamCiro,
                toplamSiparis,
                siparisler
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Mesaj = "Bir hata oluştu: " + ex.Message });
        }
    }

    [HttpGet("urun-satis")]
    public async Task<IActionResult> GetUrunSatis([FromQuery] int gun=30)
    {
        try
        {
            var sonSiparisTarihi = await _context.Siparislers
                .Where(s => s.SiparisTarihi != null)
                .OrderByDescending(s => s.SiparisTarihi)
                .Select(s => s.SiparisTarihi)
                .FirstOrDefaultAsync();

            var referansTarih = (sonSiparisTarihi ?? DateTime.Today).Date;
            var baslangicTarihi = referansTarih.AddDays(-gun);

            var result = await _context.SiparisDetays
                .Where(sd => sd.Siparis.SiparisTarihi >= baslangicTarihi)
                .GroupBy(sd =>new  { sd.Urun.UrunId, sd.Urun.UrunAdi })
                .Select(g => new
                {
                    urunId = g.Key.UrunId,
                    urunAdi = g.Key.UrunAdi,
                    toplamAdet = g.Sum(sd => sd.Adet),
                    toplamCiro = g.Sum(sd => sd.Adet * sd.BirimFiyat),
                    siparisSayisi = g.Select(sd => sd.SiparisId).Distinct().Count()
                })
                .OrderByDescending(x => x.toplamCiro)
                .Take(20)
                .ToListAsync();


            return Ok(new
            {
               gun = gun,
               data = result,
               toplamUrun = result.Count,
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Mesaj = $"Hata: {ex.Message}"  });
        }
    }

    [HttpGet("rezervasyon-raporu")]
    public async Task<IActionResult> GetRezervasyonRaporu([FromQuery] DateTime? baslangic,
        [FromQuery] DateTime? bitis)
    {
        try
        {
            var baslangicTarih = baslangic ?? DateTime.Now.AddDays(-30);
            var bitisTarih = bitis ?? DateTime.Now;

            var rezervasyonlar = await _context.Rezervasyons
                .Where(r => r.TarihSaat >= baslangicTarih && r.TarihSaat < bitisTarih)
                .GroupBy(r => r.TarihSaat.Date)
                .Select(g => new
                {
                    Tarih = g.Key,
                    ToplamRezervasyon = g.Count(),
                    Onaylanan = g.Count(r => r.Durum == "ONAYLANDI"),
                    IptalEdilen = g.Count(r => r.Durum == "IPTAL"),
                    Beklemede = g.Count(r => r.Durum == "BEKLEMEDE"),
                    Reddedilen = g.Count(r => r.Durum == "REDDEDILDI"),
                    Tamamlanan = g.Count(r => r.Durum == "TAMAMLANDI")
                })
                .OrderBy(x => x.Tarih)
                .ToListAsync();


            return Ok(new
            {
                baslangic = baslangicTarih.ToString("yyyy-MM-dd"),
                bitis = bitisTarih.AddDays(-1).ToString("yyyy-MM-dd"),
                data = rezervasyonlar,
                toplamRezervasyon = rezervasyonlar.Sum(x => x.ToplamRezervasyon),
                onaylanan = rezervasyonlar.Sum(x => x.Onaylanan),
                iptalEdilen = rezervasyonlar.Sum(x => x.IptalEdilen),
                beklemede = rezervasyonlar.Sum(x => x.Beklemede),
                reddedilen = rezervasyonlar.Sum(x => x.Reddedilen),
                tamamlanan = rezervasyonlar.Sum(x => x.Tamamlanan)
            });
        }

        catch (Exception ex)
        {
            return BadRequest(new { Mesaj = $"Hata: {ex.Message}" });
        }
    }

    [HttpGet("gelir-istatistikleri")]
    public async Task<IActionResult> GetGelirIstatistikleri([FromQuery] int? yil = null)
    {
        try
        {
            var queryYil = yil ?? DateTime.Now.Year;

            var aylikGelir = await _context.Siparislers
                .Where(s => s.SiparisTarihi != null && s.SiparisTarihi.Value.Year == queryYil)
                .GroupBy(s => s.SiparisTarihi.Value.Month)
                .Select(g => new
                {
                    Ay = g.Key,
                    ToplamGelir = g.Sum(s => s.ToplamTutar),
                    SiparisSayisi = g.Count(),
                    OrtalamaSiparis = g.Average(s => s.ToplamTutar)
                })
                .OrderBy(x => x.Ay)
                .ToListAsync();

            var ayIsimleri = new[] { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
                                     "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };

            var result = aylikGelir.Select(x => new
            {
                x.Ay,
                AyAdi = ayIsimleri[x.Ay - 1],
                x.ToplamGelir,
                x.SiparisSayisi,
                x.OrtalamaSiparis
            }).ToList();

            return Ok(new
            {
                Yil = queryYil,
                Data = result,
                ToplamYillikGelir = result.Sum(x => x.ToplamGelir),
                ToplamSiparis = result.Sum(x => x.SiparisSayisi)
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Mesaj = $"Hata: {ex.Message}" });
        }
    }

    [HttpGet("dashboard-ozet")]
    public async Task<IActionResult> GetDashboardOzet()
    {
        try
        {
            var bugun = DateTime.Today;
            var oncekiGun = bugun.AddDays(-1);
            var ayBaslangic = new DateTime(bugun.Year, bugun.Month, 1);

            var bugunCiro = await _context.Siparislers
                .Where(s => s.SiparisTarihi >= bugun && s.SiparisTarihi < bugun.AddDays(1))
                .SumAsync(s => s.ToplamTutar);

            var oncekiGunCiro = await _context.Siparislers
                .Where(s => s.SiparisTarihi >= oncekiGun && s.SiparisTarihi < bugun)
                .SumAsync(s => s.ToplamTutar);

            var ayCiro = await _context.Siparislers
                .Where(s => s.SiparisTarihi >= ayBaslangic && s.SiparisTarihi < bugun.AddDays(1))
                .SumAsync(s => s.ToplamTutar);

            var aktifSiparis = await _context.Siparislers
                .CountAsync(s => s.SiparisDurumu != "TAMAMLANDI" &&
                                s.SiparisDurumu != "IPTAL" &&
                                s.SiparisDurumu != "ODENDI" &&
                                s.SiparisDurumu != "IADE");

            var bugunSiparis = await _context.Siparislers
                .CountAsync(s => s.SiparisTarihi >= bugun && s.SiparisTarihi < bugun.AddDays(1));

            var toplamSiparis = await _context.Siparislers.CountAsync();

            decimal degisimYuzdesi = 0;
            if (oncekiGunCiro > 0)
            {
                degisimYuzdesi = (decimal)((bugunCiro - oncekiGunCiro) / oncekiGunCiro * 100);
            }

            return Ok(new
            {
                BugunCiro = bugunCiro,
                OncekiGunCiro = oncekiGunCiro,
                AyCiro = ayCiro,
                AktifSiparis = aktifSiparis,
                BugunSiparis = bugunSiparis,
                ToplamSiparis = toplamSiparis,
                DegisimYuzdesi = Math.Round(degisimYuzdesi, 2),
                Tarih = bugun
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Mesaj = $"Hata: {ex.Message}" });
        }
    }

    [HttpGet("kategori-satis")]
    public async Task<IActionResult> GetKategoriSatis([FromQuery] int gun = 30)
    {
        try
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
                .GroupBy(sd => sd.Urun.Kategori.KategoriAdi)
                .Select(g => new
                {
                    kategoriAdi = g.Key ?? "Kategorisiz",
                    toplamAdet = g.Sum(sd => sd.Adet),
                    toplamCiro = g.Sum(sd => sd.Adet * sd.BirimFiyat),
                    urunSayisi = g.Select(sd => sd.UrunId).Distinct().Count()
                })
                .OrderByDescending(x => x.toplamCiro)
                .ToListAsync();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Mesaj = $"Hata: {ex.Message}" });
        }
    }
}