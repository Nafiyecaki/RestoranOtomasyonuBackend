using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.Data;
using Restoran.Data.Entities;
using System;
using System.Collections.Generic;
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

    // GET /api/siparisler -> Tüm siparişleri Admin ve Garson panelleri için eksiksiz getirir
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var siparisler = await _context.Siparislers
            .OrderByDescending(s => s.SiparisTarihi)
            .Select(s => new
            {
                s.SiparisId,
                s.MasaId, // 🔑 Admin panelinin masayı eşleştirmesi için eklendi
                s.SiparisDurumu,
                s.SiparisTipi,
                s.ToplamTutar,
                s.SiparisTarihi,
                MasaNo = s.Masa != null ? s.Masa.MasaNo : null,
                UyeAdi = s.Uye != null ? s.Uye.UyeAdi + " " + s.Uye.UyeSoyadi : "Ziyaretçi",
                PersonelAdi = s.Personel != null ? s.Personel.PersonelAdi + " " + s.Personel.PersonelSoyadi : null,
                DetaySayisi = s.SiparisDetays.Count,
                // 🔑 Admin tarafında sipariş ürünlerinin detaylarının görünmesini sağlar
                SiparisDetays = s.SiparisDetays.Select(d => new
                {
                    d.SiparisDetayId,
                    d.UrunId,
                    UrunAdi = d.Urun != null ? d.Urun.UrunAdi : "Ürün",
                    d.Adet,
                    d.BirimFiyat,
                    SatirToplami = d.Adet * d.BirimFiyat,
                    d.DetayNot
                }).ToList()
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
                s.MasaId,
                s.SiparisDurumu,
                s.SiparisTipi,
                s.ToplamTutar,
                s.SiparisTarihi,
                MasaNo = s.Masa != null ? s.Masa.MasaNo : null,
                Detaylar = s.SiparisDetays.Select(d => new
                {
                    d.SiparisDetayId,
                    d.UrunId,
                    UrunAdi = d.Urun != null ? d.Urun.UrunAdi : "Ürün",
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

        if (dto.MasaId.HasValue)
        {
            var masa = await _context.Masas.FindAsync(dto.MasaId.Value);
            if (masa == null)
                return NotFound($"ID'si {dto.MasaId} olan masa bulunamadı.");
            masa.MasaDurumu = "DOLU";
        }

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

    // PUT /api/siparisler/5 -> Mevcut siparişi ve detaylarını günceller (Veritabanına tam kaydeder)
    [HttpPut("{id}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] SiparisGuncelleDto dto)
    {
        if (dto == null) return BadRequest("Güncelleme verileri boş olamaz.");

        var siparis = await _context.Siparislers
            .Include(s => s.SiparisDetays)
            .FirstOrDefaultAsync(s => s.SiparisId == id);

        if (siparis == null) return NotFound("Güncellenmek istenen sipariş bulunamadı.");

        if (siparis.SiparisDurumu == "TAMAMLANDI" || siparis.SiparisDurumu == "IPTAL" || siparis.SiparisDurumu == "ODENDI")
        {
            return BadRequest($"'{siparis.SiparisDurumu}' durumundaki bir sipariş güncellenemez.");
        }

        siparis.SiparisTipi = dto.SiparisTipi ?? siparis.SiparisTipi;
        if (dto.UyeId.HasValue) siparis.UyeId = dto.UyeId;
        if (dto.MasaId.HasValue) siparis.MasaId = dto.MasaId;
        if (dto.PersonelId.HasValue) siparis.PersonelId = dto.PersonelId;

        if (dto.Detaylar != null && dto.Detaylar.Any())
        {
            // 1. Eski detayları sil
            _context.SiparisDetays.RemoveRange(siparis.SiparisDetays);

            // 2. Yeni ürün kalemlerini oluştur ve tutarı hesapla
            siparis.ToplamTutar = 0;
            var yeniDetaylar = new List<SiparisDetay>();

            foreach (var d in dto.Detaylar)
            {
                var urun = await _context.Urunlers.FindAsync(d.UrunId);
                if (urun == null)
                    return NotFound($"ID'si {d.UrunId} olan ürün sistemde bulunamadı.");

                int adet = d.Adet <= 0 ? 1 : d.Adet;

                yeniDetaylar.Add(new SiparisDetay
                {
                    SiparisId = id,
                    UrunId = d.UrunId,
                    Adet = adet,
                    BirimFiyat = urun.Fiyat,
                    DetayNot = d.DetayNot
                });

                siparis.ToplamTutar += adet * urun.Fiyat;
            }

            // 3. Veritabanına yeni detayları ekle
            await _context.SiparisDetays.AddRangeAsync(yeniDetaylar);
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Sipariş detayları ve toplam tutarı başarıyla güncellendi.",
            SiparisId = siparis.SiparisId,
            YeniToplamTutar = siparis.ToplamTutar
        });
    }

    // PUT /api/siparisler/{id}/tamamla -> Siparişi tamamlar ve stoktan düşer
    [HttpPut("{id}/tamamla")]
    public async Task<IActionResult> SiparisTamamla(int id)
    {
        var siparis = await _context.Siparislers
            .Include(s => s.SiparisDetays)
            .ThenInclude(sd => sd.Urun)
            .ThenInclude(u => u.UrunRecetesis)
            .ThenInclude(ur => ur.Malzeme)
            .FirstOrDefaultAsync(s => s.SiparisId == id);

        if (siparis == null)
            return NotFound("Sipariş bulunamadı.");

        if (siparis.SiparisDurumu == "TAMAMLANDI")
            return BadRequest("Sipariş zaten tamamlanmış.");

        if (siparis.SiparisDurumu == "IPTAL")
            return BadRequest("İptal edilen sipariş tamamlanamaz.");

        if (siparis.SiparisDurumu == "ODENDI")
            return BadRequest("Ödemesi alınmış sipariş tamamlanamaz.");

        int personelId = siparis.PersonelId ?? 1;

        var stokHataMesajlari = new List<string>();
        var stokHareketleri = new List<StokHareket>();

        foreach (var detay in siparis.SiparisDetays)
        {
            var urun = detay.Urun;
            if (urun == null) continue;

            var receteler = urun.UrunRecetesis;
            if (receteler == null || !receteler.Any())
            {
                stokHataMesajlari.Add($"{urun.UrunAdi} için reçete tanımlı değil!");
                continue;
            }

            foreach (var recete in receteler)
            {
                var malzeme = recete.Malzeme;
                if (malzeme == null) continue;

                var kullanilacakMiktar = recete.KullanimMiktari * detay.Adet;

                if (malzeme.StokMiktari < kullanilacakMiktar)
                {
                    stokHataMesajlari.Add($"{malzeme.MalzemeAdi} yetersiz! " +
                        $"Gerekli: {kullanilacakMiktar} {malzeme.Birim}, " +
                        $"Mevcut: {malzeme.StokMiktari} {malzeme.Birim}");
                    continue;
                }

                malzeme.StokMiktari -= kullanilacakMiktar;

                stokHareketleri.Add(new StokHareket
                {
                    UrunId = urun.UrunId,
                    PersonelId = personelId,
                    StokIslemTipi = "CIKIS",
                    StokMiktari = (int)kullanilacakMiktar,
                    IsleminTarihSaati = DateTime.Now,
                    IsleminAciklamasi = $"Sipariş #{siparis.SiparisId} - {urun.UrunAdi} ({detay.Adet} adet)"
                });
            }
        }

        if (stokHataMesajlari.Any())
        {
            return BadRequest(new
            {
                Mesaj = "Stok yetersiz! Sipariş tamamlanamadı.",
                Hatalar = stokHataMesajlari
            });
        }

        foreach (var hareket in stokHareketleri)
        {
            _context.StokHarekets.Add(hareket);
        }

        siparis.SiparisDurumu = "TAMAMLANDI";

        if (siparis.MasaId.HasValue)
        {
            var baskaAktifVarMi = await _context.Siparislers.AnyAsync(s =>
                s.MasaId == siparis.MasaId &&
                s.SiparisId != id &&
                s.SiparisDurumu != "IPTAL" &&
                s.SiparisDurumu != "TAMAMLANDI" &&
                s.SiparisDurumu != "ODENDI");

            if (!baskaAktifVarMi)
            {
                var masa = await _context.Masas.FindAsync(siparis.MasaId.Value);
                if (masa != null)
                {
                    masa.MasaDurumu = "BOŞ";
                }
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Sipariş başarıyla tamamlandı ve stoklar güncellendi.",
            SiparisId = siparis.SiparisId,
            StokHareketSayisi = stokHareketleri.Count
        });
    }

    // PUT /api/siparisler/{id}/durum -> Sipariş durumunu günceller
    [HttpPut("{id}/durum")]
    public async Task<IActionResult> DurumGuncelle(int id, [FromBody] SiparisDurumGuncelleDto dto)
    {
        if (dto == null) return BadRequest();

        var siparis = await _context.Siparislers.FindAsync(id);
        if (siparis == null) return NotFound("Durumu güncellenecek sipariş bulunamadı.");

        var gecerliDurumlar = new[] {
            "BEKLEMEDE",
            "HAZIRLANIYOR",
            "HAZIR",
            "TESLIM EDILDI",
            "TAMAMLANDI",
            "IPTAL",
            "IADE",
            "ODENDI"
        };

        var yeniDurum = dto.SiparisDurumu?.ToUpper()?.Trim()
            .Replace('İ', 'I').Replace('Ş', 'S').Replace('Ç', 'C');

        if (string.IsNullOrEmpty(yeniDurum) || !gecerliDurumlar.Contains(yeniDurum))
            return BadRequest(new
            {
                Mesaj = "Geçersiz sipariş durumu. Geçerli değerler: " + string.Join(", ", gecerliDurumlar)
            });

        siparis.SiparisDurumu = yeniDurum;

        await _context.SaveChangesAsync();
        return Ok(new
        {
            Mesaj = $"Sipariş durumu '{yeniDurum}' olarak güncellendi.",
            SiparisId = id
        });
    }

    // PUT /api/siparisler/5/iptal -> Siparişi iptal eder
    [HttpPut("{id}/iptal")]
    public async Task<IActionResult> SiparisIptal(int id)
    {
        var siparis = await _context.Siparislers.FindAsync(id);
        if (siparis == null) return NotFound("İptal edilecek sipariş bulunamadı.");

        if (siparis.SiparisDurumu == "TAMAMLANDI" || siparis.SiparisDurumu == "ODENDI")
        {
            return BadRequest("Ödemesi alınmış veya tamamlanmış bir sipariş iptal edilemez.");
        }

        siparis.SiparisDurumu = "IPTAL";

        if (siparis.MasaId.HasValue)
        {
            var baskaAktifVarMi = await _context.Siparislers.AnyAsync(s =>
                s.MasaId == siparis.MasaId &&
                s.SiparisId != id &&
                s.SiparisDurumu != "IPTAL" && s.SiparisDurumu != "TAMAMLANDI" && s.SiparisDurumu != "ODENDI");
            if (!baskaAktifVarMi)
            {
                var masa = await _context.Masas.FindAsync(siparis.MasaId.Value);
                if (masa != null) masa.MasaDurumu = "BOŞ";
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { Mesaj = "Sipariş başarıyla iptal edildi.", SiparisId = id });
    }

    // DELETE /api/siparisler/5 -> Siparişi siler
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var siparis = await _context.Siparislers
            .Include(s => s.SiparisDetays)
            .FirstOrDefaultAsync(s => s.SiparisId == id);

        if (siparis == null) return NotFound("Silinmek istenen sipariş bulunamadı.");

        try
        {
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