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
public class StokHareketleriController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public StokHareketleriController(DbRestoranContext context)
    {
        _context = context;
    }

    // GET /api/StokHareketleri -> Tüm stok hareketleri (en yeni üstte)
    [HttpGet]
    // [Authorize(Roles = "Yönetici,Aşçı")]
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

    // GET /api/StokHareketleri/5 -> Tek bir stok hareketinin detayı
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var hareket = await _context.StokHarekets
            .Where(s => s.StokHareketId == id)
            .Select(s => new
            {
                s.StokHareketId,
                s.UrunId,
                s.StokIslemTipi,
                s.StokMiktari,
                s.IsleminTarihSaati,
                s.IsleminAciklamasi,
                s.PersonelId,
                UrunAdi = s.Urun != null ? s.Urun.UrunAdi : "Bilinmeyen Ürün"
            })
            .FirstOrDefaultAsync();

        if (hareket == null) return NotFound("Stok hareketi bulunamadı.");
        return Ok(hareket);
    }

    // POST /api/StokHareketleri/Ekle -> Manuel stok giriş/çıkış/fire
    [HttpPost("Ekle")]
    public async Task<IActionResult> AddStokHareket([FromBody] StokHareketEkleDto dto)
    {
        if (dto == null) return BadRequest("Veri boş olamaz.");
        if (dto.StokMiktari <= 0) return BadRequest("Miktar 0'dan büyük olmalı.");

        var islemTipi = NormalizeIslemTipi(dto.StokIslemTipi);

        var urun = await _context.Urunlers.FindAsync(dto.UrunId);
        if (urun == null) return NotFound("Ürün bulunamadı.");

        var personelVarMi = await _context.Personels.AnyAsync(p => p.PersonelId == dto.PersonelId && p.IsActive == true);
        if (!personelVarMi) return BadRequest("Geçersiz personel ID.");

        // Stoğu güncelle
        if (islemTipi == "GIRIS")
        {
            urun.StokMiktari = (urun.StokMiktari ?? 0) + dto.StokMiktari;
        }
        else if (islemTipi == "CIKIS" || islemTipi == "FIRE")
        {
            if ((urun.StokMiktari ?? 0) < dto.StokMiktari)
                return BadRequest("Stokta yeterli ürün yok, işlem reddedildi!");

            urun.StokMiktari = (urun.StokMiktari ?? 0) - dto.StokMiktari;
        }
        else
        {
            return BadRequest("Geçersiz işlem tipi! (GIRIS, CIKIS veya FIRE olmalı)");
        }

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
            Mesaj = "Stok hareketi işlendi ve ürün stoğu güncellendi.",
            UrunAdi = urun.UrunAdi,
            YeniStok = urun.StokMiktari
        });
    }

    // PUT /api/StokHareketleri/{id} -> Stok hareketini ve ürün stoğunu günceller
    [HttpPut("{id}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] StokHareketGuncelleDto dto)
    {
        if (dto == null) return BadRequest("Veri boş olamaz.");
        if (dto.StokMiktari <= 0) return BadRequest("Miktar 0'dan büyük olmalı.");

        var hareket = await _context.StokHarekets.FindAsync(id);
        if (hareket == null) return NotFound("Güncellenmek istenen stok hareketi bulunamadı.");

        var eskiUrun = await _context.Urunlers.FindAsync(hareket.UrunId);
        if (eskiUrun == null) return NotFound("İlişkili orijinal ürün bulunamadı.");

        var personelVarMi = await _context.Personels.AnyAsync(p => p.PersonelId == dto.PersonelId && p.IsActive == true);
        if (!personelVarMi) return BadRequest("Geçersiz personel ID.");

        // 1. ADIM: Eski stok hareketinin etkisini geri alalım (Rollback)
        if (hareket.StokIslemTipi == "GIRIS")
        {
            eskiUrun.StokMiktari = (eskiUrun.StokMiktari ?? 0) - hareket.StokMiktari;
        }
        else if (hareket.StokIslemTipi == "CIKIS" || hareket.StokIslemTipi == "FIRE")
        {
            eskiUrun.StokMiktari = (eskiUrun.StokMiktari ?? 0) + hareket.StokMiktari;
        }

        // 2. ADIM: Eğer ürün değiştiyse hedef ürünü yükle, değişmediyse eski ürün üzerinden devam et
        var hedefUrun = eskiUrun;
        if (dto.UrunId != hareket.UrunId)
        {
            var yeniUrun = await _context.Urunlers.FindAsync(dto.UrunId);
            if (yeniUrun == null)
            {
                // Değişikliği geri almak için eski haline kaydedip hata dönüyoruz
                _context.Entry(eskiUrun).State = EntityState.Unchanged;
                return NotFound("Yeni seçilen ürün bulunamadı.");
            }
            hedefUrun = yeniUrun;
        }

        // 3. ADIM: Yeni hareket tipine göre stoğu güncelle
        var yeniIslemTipi = NormalizeIslemTipi(dto.StokIslemTipi);
        if (yeniIslemTipi == "GIRIS")
        {
            hedefUrun.StokMiktari = (hedefUrun.StokMiktari ?? 0) + dto.StokMiktari;
        }
        else if (yeniIslemTipi == "CIKIS" || yeniIslemTipi == "FIRE")
        {
            if ((hedefUrun.StokMiktari ?? 0) < dto.StokMiktari)
            {
                // Hata durumunda değişiklikleri iptal etmek için DB Context'i resetliyoruz
                _context.Entry(eskiUrun).State = EntityState.Unchanged;
                if (dto.UrunId != hareket.UrunId) _context.Entry(hedefUrun).State = EntityState.Unchanged;
                return BadRequest("Yetersiz stok! Bu güncelleme yapıldığında stok negatife düşüyor.");
            }
            hedefUrun.StokMiktari = (hedefUrun.StokMiktari ?? 0) - dto.StokMiktari;
        }
        else
        {
            _context.Entry(eskiUrun).State = EntityState.Unchanged;
            return BadRequest("Geçersiz işlem tipi!");
        }

        // 4. ADIM: Stok hareketi bilgilerini güncelle
        hareket.UrunId = dto.UrunId;
        hareket.StokIslemTipi = yeniIslemTipi;
        hareket.StokMiktari = dto.StokMiktari;
        hareket.IsleminAciklamasi = dto.IsleminAciklamasi;
        hareket.PersonelId = dto.PersonelId;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Stok hareketi başarıyla güncellendi ve ürün stokları düzeltildi.",
            UrunAdi = hedefUrun.UrunAdi,
            YeniStok = hedefUrun.StokMiktari
        });
    }

    // DELETE /api/StokHareketleri/{id} -> Hareketi siler ve ürün stoğunu eski haline getirir
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var hareket = await _context.StokHarekets.FindAsync(id);
        if (hareket == null) return NotFound("Silinecek stok hareketi bulunamadı.");

        var urun = await _context.Urunlers.FindAsync(hareket.UrunId);
        if (urun != null)
        {
            // Silinen harekete göre stoğu eski haline getiriyoruz (Rollback)
            if (hareket.StokIslemTipi == "GIRIS")
            {
                if ((urun.StokMiktari ?? 0) < hareket.StokMiktari)
                {
                    return BadRequest("Bu stok girişini silemeyiz! Çünkü silersek ürünün stoğu eksiye düşüyor.");
                }
                urun.StokMiktari = (urun.StokMiktari ?? 0) - hareket.StokMiktari;
            }
            else if (hareket.StokIslemTipi == "CIKIS" || hareket.StokIslemTipi == "FIRE")
            {
                urun.StokMiktari = (urun.StokMiktari ?? 0) + hareket.StokMiktari;
            }
        }

        _context.StokHarekets.Remove(hareket);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Stok hareketi silindi, ürün stoğu eski durumuna döndürüldü.",
            UrunAdi = urun?.UrunAdi ?? "Bilinmeyen Ürün",
            MevcutStok = urun?.StokMiktari
        });
    }

    private static string NormalizeIslemTipi(string? tip)
    {
        if (string.IsNullOrWhiteSpace(tip)) return "";

        var t = tip.Trim().ToUpperInvariant();
        t = t.Replace('İ', 'I').Replace('Ş', 'S').Replace('Ç', 'C');
        return t;
    }
}