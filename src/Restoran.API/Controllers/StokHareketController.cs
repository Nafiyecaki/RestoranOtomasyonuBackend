using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.Data;
using Restoran.Data.Entities;
using Restoran.API.Dtos;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StokHareketleriController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public StokHareketleriController(DbRestoranContext context)
    {
        _context = context;
    }

    // GET /api/StokHareketleri -> tüm stok hareketleri (en yeni üstte)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var hareketler = await _context.StokHarekets
            .Select(s => new
            {
                s.StokHareketId,
                s.StokIslemTipi,
                s.StokMiktari,
                s.IsleminTarihSaati,
                s.IsleminAciklamasi,
                UrunAdi = s.Urun != null ? s.Urun.UrunAdi : "Bilinmeyen Ürün",
                PersonelIsim = s.Personel != null
                    ? s.Personel.PersonelAdi + " " + s.Personel.PersonelSoyadi
                    : "Sistem"
            })
            .OrderByDescending(s => s.IsleminTarihSaati)
            .ToListAsync();

        return Ok(hareketler);
    }

    // POST /api/StokHareketleri/Ekle -> manuel stok giriş/çıkış/fire
    [HttpPost("Ekle")]
    public async Task<IActionResult> AddStokHareket([FromBody] StokHareketEkleDto dto)
    {
        if (dto == null) return BadRequest("Veri boş olamaz.");

        // Miktar validasyonu
        if (dto.StokMiktari <= 0)
            return BadRequest("Miktar 0'dan büyük olmalı.");

        // İşlem tipini normalize et ("giriş" -> "GIRIS" gibi)
        var islemTipi = NormalizeIslemTipi(dto.StokIslemTipi);

        // Ürünü kontrol et
        var urun = await _context.Urunlers.FindAsync(dto.UrunId);
        if (urun == null) return NotFound("Ürün bulunamadı.");

        // Ürünün stok miktarını güncelle
        if (islemTipi == "GIRIS")
        {
            urun.StokMiktari = (urun.StokMiktari ?? 0) + dto.StokMiktari;
        }
        else if (islemTipi == "CIKIS" || islemTipi == "FIRE")
        {
            if ((urun.StokMiktari ?? 0) < dto.StokMiktari)
                return BadRequest("Stokta bu kadar ürün yok, yetersiz stok!");

            urun.StokMiktari = (urun.StokMiktari ?? 0) - dto.StokMiktari;
        }
        else
        {
            return BadRequest("Geçersiz işlem tipi! (GIRIS, CIKIS veya FIRE olmalı)");
        }

        // Hareket kaydını oluştur
        var yeniHareket = new StokHareket
        {
            UrunId = dto.UrunId,
            StokIslemTipi = islemTipi,
            StokMiktari = dto.StokMiktari,
            IsleminTarihSaati = DateTime.Now,
            IsleminAciklamasi = dto.IsleminAciklamasi,
            PersonelId = dto.PersonelId
        };

        _context.StokHarekets.Add(yeniHareket);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Stok hareketi başarıyla işlendi ve ürün stoğu güncellendi.",
            UrunAdi = urun.UrunAdi,
            YeniStok = urun.StokMiktari
        });
    }

    // "giriş", "Çıkış" gibi girdileri veritabanı standardına çevirir
    private static string NormalizeIslemTipi(string? tip)
    {
        if (string.IsNullOrWhiteSpace(tip)) return "";

        var t = tip.Trim().ToUpperInvariant();
        t = t.Replace('İ', 'I').Replace('Ş', 'S').Replace('Ç', 'C');
        return t;
    }
}