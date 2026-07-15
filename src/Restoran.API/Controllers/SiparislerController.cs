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
public class SiparislerController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public SiparislerController(DbRestoranContext context)
    {
        _context = context;
    }

    // GET /api/siparisler -> Tüm siparişleri özet halinde listeler
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var siparisler = await _context.Siparislers
            .OrderByDescending(s => s.SiparisTarihi)
            .Select(s => new
            {
                s.SiparisId,
                s.SiparisDurumu,
                s.SiparisTipi,
                s.ToplamTutar,
                s.SiparisTarihi,
                MasaNo = s.Masa != null ? s.Masa.MasaNo : null,
                UyeAdi = s.Uye != null ? s.Uye.UyeAdi + " " + s.Uye.UyeSoyadi : "Ziyaretçi",
                PersonelAdi = s.Personel != null ? s.Personel.PersonelAdi + " " + s.Personel.PersonelSoyadi : null,
                DetaySayisi = s.SiparisDetays.Count
            })
            .ToListAsync();

        return Ok(siparisler);
    }

    // GET /api/siparisler/5 -> Tek siparişi tüm alt ürün detaylarıyla getirir
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
                MasaNo = s.Masa != null ? s.Masa.MasaNo : null,
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

    // POST /api/siparisler -> Yeni sipariş oluşturur
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] SiparisOlusturDto dto)
    {
        if (dto.Detaylar == null || !dto.Detaylar.Any())
            return BadRequest("Sipariş oluşturmak için en az bir ürün eklemelisiniz.");

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

        await _context.Siparislers.AddAsync(siparis);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Sipariş ve detayları başarıyla oluşturuldu.",
            SiparisId = siparis.SiparisId,
            HesaplananToplamTutar = siparis.ToplamTutar
        });
    }

    // PUT /api/siparisler/5 -> Mevcut siparişi ve detaylarını günceller (Tutar otomatik yeniden hesaplanır)
    [HttpPut("{id}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] SiparisGuncelleDto dto)
    {
        if (dto == null) return BadRequest();

        // Detay tablolarıyla (SiparisDetays) birlikte siparişi çekiyoruz
        var siparis = await _context.Siparislers
            .Include(s => s.SiparisDetays)
            .FirstOrDefaultAsync(s => s.SiparisId == id);

        if (siparis == null) return NotFound("Güncellenmek istenen sipariş bulunamadı.");

        // İŞ KURALI GÜVENLİĞİ: "TAMAMLANDI" veya zaten "IPTAL" olmuş siparişin içeriği değiştirilemez!
        if (siparis.SiparisDurumu == "TAMAMLANDI" || siparis.SiparisDurumu == "IPTAL")
        {
            return BadRequest($"'{siparis.SiparisDurumu}' durumundaki bir siparişi güncelleyemezsin.");
        }

        // Genel bilgileri güncelle
        siparis.SiparisTipi = dto.SiparisTipi ?? siparis.SiparisTipi;
        siparis.UyeId = dto.UyeId;
        siparis.MasaId = dto.MasaId;
        siparis.PersonelId = dto.PersonelId;

        // Sipariş detaylarını yenilemek için eğer yeni detaylar gönderildiyse mevcutları temizleyip yenilerini giriyoruz
        if (dto.Detaylar != null && dto.Detaylar.Any())
        {
            // Eski detayları veritabanından tamamen uçuruyoruz
            _context.SiparisDetays.RemoveRange(siparis.SiparisDetays);

            siparis.ToplamTutar = 0; // Toplam tutarı sıfırlayıp baştan toplayacağız

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
        }

        await _context.SaveChangesAsync();
        return Ok(new { Mesaj = "Sipariş detayları ve toplam tutarı başarıyla güncellendi.", SipariId = siparis.SiparisId, YeniToplamTutar = siparis.ToplamTutar });
    }

    // PUT /api/siparisler/5/durum -> Sipariş durumunu günceller (Mutfak & Kasa ekranları için)
    [HttpPut("{id}/durum")]
    public async Task<IActionResult> DurumGuncelle(int id, [FromBody] SiparisDurumGuncelleDto dto)
    {
        if (dto == null) return BadRequest();

        var siparis = await _context.Siparislers.FindAsync(id);
        if (siparis == null) return NotFound("Durumu güncellenecek sipariş bulunamadı.");

        // Gelen durum bilgisini standardize etmek için büyük harfe çevirebiliriz
        siparis.SiparisDurumu = dto.SiparisDurumu.ToUpper();

        await _context.SaveChangesAsync();
        return Ok(new { Mesaj = $"Sipariş durumu '{siparis.SiparisDurumu}' olarak güncellendi.", SiparisId = id });
    }

    // PUT /api/siparisler/5/iptal -> Siparişi tek tıkla hızlıca İPTAL durumuna çeker
    [HttpPut("{id}/iptal")]
    public async Task<IActionResult> SiparisIptal(int id)
    {
        var siparis = await _context.Siparislers.FindAsync(id);
        if (siparis == null) return NotFound("İptal edilecek sipariş bulunamadı.");

        if (siparis.SiparisDurumu == "TAMAMLANDI")
        {
            return BadRequest("Teslim edilip ödemesi alınmış (TAMAMLANDI) bir siparişi iptal edemezsin.");
        }

        siparis.SiparisDurumu = "IPTAL";

        await _context.SaveChangesAsync();
        return Ok(new { Mesaj = "Sipariş başarıyla iptal edildi.", SiparisId = id });
    }

    // DELETE /api/siparisler/5 -> Siparişi ve ilişkili detaylarını veritabanından tamamen siler
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var siparis = await _context.Siparislers
            .Include(s => s.SiparisDetays)
            .FirstOrDefaultAsync(s => s.SiparisId == id);

        if (siparis == null) return NotFound("Silinmek istenen sipariş bulunamadı.");

        try
        {
            // Cascade Delete veya EF ilişki kontrolüyle detaylar da siparişle beraber silinir
            _context.Siparislers.Remove(siparis);
            await _context.SaveChangesAsync();
            return Ok(new { Mesaj = "Sipariş ve ilişkili tüm detayları sistemden tamamen silindi." });
        }
        catch (DbUpdateException)
        {
            return BadRequest("Bu siparişe bağlı fatura veya ödeme kaydı olduğu için fiziksel olarak silinemez, iptal etmeyi (PUT /iptal) deneyin.");
        }
    }
}