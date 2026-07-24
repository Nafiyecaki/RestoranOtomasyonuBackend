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

        var stokHataMesajlari = new List<string>();
        var stokHataDetaylari = new List<object>();

        foreach (var d in dto.Detaylar)
        {
            var urun = await _context.Urunlers
                .Include(u => u.UrunRecetesis)
                .ThenInclude(r => r.Malzeme)
                .FirstOrDefaultAsync(u => u.UrunId == d.UrunId);

            if (urun == null)
            {
                return NotFound(new { Mesaj = $"ID'si {d.UrunId} olan ürün sistemde bulunamadı." });
            }

            // Ürünün reçetesi var mı kontrol et
            if (urun.UrunRecetesis == null || !urun.UrunRecetesis.Any())
            {
                // Reçetesi olmayan ürünler için stok kontrolü yapma (isteğe bağlı)
                // Not: Reçetesiz ürünler stoktan düşmez, sadece satılır
                continue;
            }

            int adet = d.Adet <= 0 ? 1 : d.Adet;

            foreach (var recete in urun.UrunRecetesis)
            {
                var malzeme = recete.Malzeme;
                if (malzeme == null) continue;

                var gerekliMiktar = recete.KullanimMiktari * adet;

                if (malzeme.StokMiktari < gerekliMiktar)
                {
                    var hataMesaji = $"❌ '{urun.UrunAdi}' ürünü için '{malzeme.MalzemeAdi}' yetersiz! " +
                        $"Gerekli: {gerekliMiktar:F2} {malzeme.Birim}, " +
                        $"Mevcut: {malzeme.StokMiktari:F2} {malzeme.Birim}";

                    stokHataMesajlari.Add(hataMesaji);
                    stokHataDetaylari.Add(new
                    {
                        UrunAdi = urun.UrunAdi,
                        MalzemeAdi = malzeme.MalzemeAdi,
                        GerekliMiktar = gerekliMiktar,
                        MevcutStok = malzeme.StokMiktari,
                        Birim = malzeme.Birim,
                        Adet = adet
                    });
                }
            }
        }

        // Stok hatası varsa siparişi oluşturma
        if (stokHataMesajlari.Any())
        {
            return BadRequest(new
            {
                Mesaj = "❌ Stok yetersiz! Sipariş oluşturulamadı.",
                HataKodu = "STOK_YETERSIZ",
                Hatalar = stokHataMesajlari,
                Detaylar = stokHataDetaylari
            });
        }

        // ============================================================
        //  ADIM 2: MASA KONTROLÜ
        // ============================================================
        if (dto.MasaId.HasValue)
        {
            var masa = await _context.Masas.FindAsync(dto.MasaId.Value);
            if (masa == null)
                return NotFound(new { Mesaj = $"ID'si {dto.MasaId} olan masa bulunamadı." });

            // Masa zaten dolu mu kontrol et
            if (masa.MasaDurumu == "DOLU")
            {
                return BadRequest(new { Mesaj = $"Masa '{masa.MasaNo}' zaten dolu!" });
            }

            masa.MasaDurumu = "DOLU";
        }

        // ============================================================
        //  ADIM 3: SİPARİŞ OLUŞTUR
        // ============================================================
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

        decimal toplamTutar = 0;
        var siparisDetaylari = new List<SiparisDetay>();

        foreach (var d in dto.Detaylar)
        {
            var urun = await _context.Urunlers.FindAsync(d.UrunId);
            if (urun == null)
                return NotFound(new { Mesaj = $"ID'si {d.UrunId} olan ürün sistemde bulunamadı." });

            int adet = d.Adet <= 0 ? 1 : d.Adet;
            decimal birimFiyat = urun.Fiyat;
            decimal satirToplami = adet * birimFiyat;

            var detay = new SiparisDetay
            {
                UrunId = d.UrunId,
                Adet = adet,
                BirimFiyat = birimFiyat,
                DetayNot = d.DetayNot ?? ""
            };

            siparisDetaylari.Add(detay);
            toplamTutar += satirToplami;
        }

        siparis.SiparisDetays = siparisDetaylari;
        siparis.ToplamTutar = toplamTutar;

        await _context.Siparislers.AddAsync(siparis);
        await _context.SaveChangesAsync();

        // Oluşturulan siparişi detaylarıyla birlikte geri döndür
        var createdOrder = await _context.Siparislers
            .Where(s => s.SiparisId == siparis.SiparisId)
            .Select(s => new
            {
                s.SiparisId,
                s.MasaId,
                s.SiparisDurumu,
                s.SiparisTipi,
                s.ToplamTutar,
                s.SiparisTarihi,
                siparisUrunleri = s.SiparisDetays.Select(d => new
                {
                    d.SiparisDetayId,
                    d.UrunId,
                    urunAdi = d.Urun != null ? d.Urun.UrunAdi : "Ürün",
                    d.Adet,
                    d.BirimFiyat,
                    satirToplami = d.Adet * d.BirimFiyat,
                    d.DetayNot
                }).ToList()
            })
            .FirstOrDefaultAsync();

        return Ok(new
        {
            Mesaj = " Sipariş başarıyla oluşturuldu ve stok kontrolü geçti.",
            SiparisId = siparis.SiparisId,
            HesaplananToplamTutar = siparis.ToplamTutar,
            Siparis = createdOrder
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

    // Restoran.API/Controllers/SiparislerController.cs

    [HttpPut("{id}/durum")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] SiparisDurumGuncelleDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.SiparisDurumu))
            return BadRequest(new { Mesaj = "Durum bilgisi gerekli." });

        var siparis = await _context.Siparislers.FindAsync(id);
        if (siparis == null)
            return NotFound(new { Mesaj = $"Sipariş #{id} bulunamadı." });

        // Güncellenmiş durum listesi
        var gecerliDurumlar = new[] {
        "BEKLEMEDE", "HAZIRLANIYOR", "HAZIR",
        "TESLIM EDILDI", "TAMAMLANDI",
        "IPTAL", "ODENDI",
        "IADE",
        "KISMI_IADE" 
    };

        var yeniDurum = dto.SiparisDurumu.ToUpper().Trim()
            .Replace('İ', 'I').Replace('Ş', 'S').Replace('Ç', 'C')
            .Replace('Ğ', 'G').Replace('Ü', 'U').Replace('Ö', 'O');

        if (!gecerliDurumlar.Contains(yeniDurum))
            return BadRequest(new { Mesaj = $"Geçersiz durum: {dto.SiparisDurumu}" });

        siparis.SiparisDurumu = yeniDurum;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = $"Sipariş #{id} durumu '{yeniDurum}' olarak güncellendi.",
            SiparisId = id,
            YeniDurum = yeniDurum
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