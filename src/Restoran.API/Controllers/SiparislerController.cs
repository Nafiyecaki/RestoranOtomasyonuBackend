using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.API.Hubs;
using Restoran.Data;
using Restoran.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SiparislerController : ControllerBase
{
    private readonly DbRestoranContext _context;
    private readonly IHubContext<SiparisHub> _hubContext;

    public SiparislerController(DbRestoranContext context, IHubContext<SiparisHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // 🔔 Sipariş verisiyle ilgili bir değişiklik olduğunda tüm bağlı istemcilere haber verir
    private async Task SiparisDegisikligiBildir(string islem)
    {
        await _hubContext.Clients.All.SendAsync("VeriGuncellendi", new
        {
            tip = "siparis",
            islem,
            zaman = DateTime.Now
        });
    }

    // GET /api/siparisler -> Tüm siparişleri Admin ve Garson panelleri için eksiksiz getirir
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var siparisler = await _context.Siparislers
            .Include(s => s.Uye)
            .Include(s => s.Masa)
            .Include(s => s.Personel)
            .OrderByDescending(s => s.SiparisTarihi)
            .Select(s => new
            {
                s.SiparisId,
                s.MasaId,
                s.SiparisDurumu,
                s.SiparisTipi,
                s.ToplamTutar,
                s.SiparisTarihi,
                MasaNo = s.Masa != null ? s.Masa.MasaNo : null,
                UyeAdi = s.Uye != null ? s.Uye.UyeAdi + " " + s.Uye.UyeSoyadi :
                         (s.SiparisTipi == "ONLINE" || s.SiparisTipi == "GEL-AL" ? "Online Müşteri" : "Ziyaretçi"),
                UyeId = s.UyeId,
                PersonelAdi = s.Personel != null ? s.Personel.PersonelAdi + " " + s.Personel.PersonelSoyadi : null,
                DetaySayisi = s.SiparisDetays.Count,
                SiparisDetays = s.SiparisDetays.Select(d => new
                {
                    d.SiparisDetayId,
                    d.UrunId,
                    UrunAdi = d.Urun != null ? d.Urun.UrunAdi : "Ürün",
                    d.Adet,
                    d.BirimFiyat,
                    SatirToplami = d.Adet * d.BirimFiyat,
                    d.DetayNot,
                    IadeEdildi = d.IadeEdildi
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
            .Include(s => s.Uye)
            .Include(s => s.Masa)
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
                UyeAdi = s.Uye != null ? s.Uye.UyeAdi + " " + s.Uye.UyeSoyadi :
                         (s.SiparisTipi == "ONLINE" || s.SiparisTipi == "GEL-AL" ? "Online Müşteri" : "Ziyaretçi"),
                UyeId = s.UyeId,
                Detaylar = s.SiparisDetays.Select(d => new
                {
                    d.SiparisDetayId,
                    d.UrunId,
                    UrunAdi = d.Urun != null ? d.Urun.UrunAdi : "Ürün",
                    d.Adet,
                    d.BirimFiyat,
                    SatirToplami = d.Adet * d.BirimFiyat,
                    d.DetayNot,
                    IadeEdildi = d.IadeEdildi
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
        // ✅ DTO validasyonu
        if (dto == null)
            return BadRequest(new { Mesaj = "Sipariş verileri boş olamaz." });

        if (dto.Detaylar == null || !dto.Detaylar.Any())
            return BadRequest(new { Mesaj = "Sipariş oluşturmak için en az bir ürün eklemelisiniz." });

        // ✅ PersonelId kontrolü
        int personelId = dto.PersonelId ?? 1;

        // ✅ Stok kontrolü
        var stokHataMesajlari = new List<string>();
        var stokHataDetaylari = new List<object>();
        var dusulecekMalzemeler = new List<(Malzemeler Malzeme, decimal Miktar, string UrunAdi, int Adet, int UrunId)>();

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

            if (urun.UrunRecetesis == null || !urun.UrunRecetesis.Any())
                continue;

            int adet = d.Adet <= 0 ? 1 : d.Adet;

            foreach (var recete in urun.UrunRecetesis)
            {
                var malzeme = recete.Malzeme;
                if (malzeme == null) continue;

                var gerekliMiktar = recete.KullanimMiktari * adet;

                if (malzeme.StokMiktari < gerekliMiktar)
                {
                    stokHataMesajlari.Add($"'{urun.UrunAdi}' için '{malzeme.MalzemeAdi}' yetersiz! Gerekli: {gerekliMiktar:F2} {malzeme.Birim}, Mevcut: {malzeme.StokMiktari:F2} {malzeme.Birim}");
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
                else
                {
                    dusulecekMalzemeler.Add((malzeme, gerekliMiktar, urun.UrunAdi, adet, urun.UrunId));
                }
            }
        }

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

        // ✅ Stok düşümü
        foreach (var kalem in dusulecekMalzemeler)
        {
            kalem.Malzeme.StokMiktari -= kalem.Miktar;
            _context.StokHarekets.Add(new StokHareket
            {
                UrunId = kalem.UrunId,
                PersonelId = personelId,
                StokIslemTipi = "CIKIS",
                StokMiktari = (int)kalem.Miktar,
                IsleminTarihSaati = DateTime.Now,
                IsleminAciklamasi = $"Sipariş alındı - {kalem.UrunAdi} ({kalem.Adet} adet)"
            });
        }

        // ✅ Siparişi oluştur
        var siparis = new Siparisler
        {
            SiparisTarihi = DateTime.Now,
            SiparisDurumu = "HAZIRLANIYOR",
            SiparisTipi = dto.SiparisTipi ?? "SALON",
            UyeId = dto.UyeId,
            MasaId = dto.MasaId,
            PersonelId = personelId,
            ToplamTutar = 0,
            SiparisDetays = new List<SiparisDetay>()
        };

        // ✅ Ürünleri ekle
        foreach (var d in dto.Detaylar)
        {
            var urun = await _context.Urunlers.FindAsync(d.UrunId);
            if (urun == null)
                return NotFound(new { Mesaj = $"ID'si {d.UrunId} olan ürün sistemde bulunamadı." });

            int adet = d.Adet <= 0 ? 1 : d.Adet;
            string detayNot = d.DetayNot ?? "";

            siparis.SiparisDetays.Add(new SiparisDetay
            {
                UrunId = d.UrunId,
                Adet = adet,
                BirimFiyat = urun.Fiyat,
                DetayNot = detayNot
            });
        }

        // ✅ Toplam tutarı hesapla
        siparis.ToplamTutar = siparis.SiparisDetays.Sum(x => x.Adet * x.BirimFiyat);

        // ✅ Masa durumunu güncelle
        if (dto.MasaId.HasValue)
        {
            var masa = await _context.Masas.FindAsync(dto.MasaId.Value);
            if (masa == null)
                return NotFound(new { Mesaj = $"ID'si {dto.MasaId} olan masa bulunamadı." });
            masa.MasaDurumu = "DOLU";
        }

        await _context.Siparislers.AddAsync(siparis);
        await _context.SaveChangesAsync();

        // ✅ SignalR ile bildir
        await SiparisDegisikligiBildir("olustur");

        // ✅ Oluşturulan siparişi detaylarıyla döndür
        var createdOrder = await _context.Siparislers
            .Include(s => s.Uye)
            .Include(s => s.Masa)
            .Where(s => s.SiparisId == siparis.SiparisId)
            .Select(s => new
            {
                s.SiparisId,
                s.MasaId,
                s.SiparisDurumu,
                s.SiparisTipi,
                s.ToplamTutar,
                s.SiparisTarihi,
                MasaNo = s.Masa != null ? s.Masa.MasaNo : null,
                UyeAdi = s.Uye != null ? s.Uye.UyeAdi + " " + s.Uye.UyeSoyadi :
                         (s.SiparisTipi == "ONLINE" || s.SiparisTipi == "GEL-AL" ? "Online Müşteri" : "Ziyaretçi"),
                UyeId = s.UyeId,
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
            Mesaj = "Sipariş ve detayları başarıyla oluşturuldu ve stok kontrolü geçti.",
            SiparisId = siparis.SiparisId,
            HesaplananToplamTutar = siparis.ToplamTutar,
            Siparis = createdOrder
        });
    }

    // PUT /api/siparisler/{id}/durum -> Sipariş durumunu günceller
    [HttpPut("{id}/durum")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] SiparisDurumGuncelleDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.SiparisDurumu))
            return BadRequest(new { Mesaj = "Durum bilgisi gerekli." });

        var siparis = await _context.Siparislers
            .Include(s => s.Uye)
            .FirstOrDefaultAsync(s => s.SiparisId == id);

        if (siparis == null)
            return NotFound(new { Mesaj = $"Sipariş #{id} bulunamadı." });

        var gecerliDurumlar = new[] {
            "BEKLEMEDE", "HAZIRLANIYOR", "HAZIR",
            "TESLIM EDILDI", "TAMAMLANDI",
            "IPTAL", "ODENDI",
            "IADE", "KISMI_IADE"
        };

        var yeniDurum = dto.SiparisDurumu.ToUpper().Trim()
            .Replace('İ', 'I').Replace('Ş', 'S').Replace('Ç', 'C')
            .Replace('Ğ', 'G').Replace('Ü', 'U').Replace('Ö', 'O');

        if (!gecerliDurumlar.Contains(yeniDurum))
            return BadRequest(new { Mesaj = $"Geçersiz durum: {dto.SiparisDurumu}" });

        var eskiDurum = siparis.SiparisDurumu;
        siparis.SiparisDurumu = yeniDurum;
        await _context.SaveChangesAsync();
        await SiparisDegisikligiBildir("durum");

        // ✅ SignalR ile bildirim gönder
        try
        {
            await _hubContext.Clients.All.SendAsync("SiparisDurumGuncellendi", new
            {
                siparisId = id,
                yeniDurum = yeniDurum,
                eskiDurum = eskiDurum,
                mesaj = $"Sipariş #{id} durumu: {eskiDurum} → {yeniDurum}"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR bildirimi gönderilemedi: {ex.Message}");
        }

        return Ok(new
        {
            Mesaj = $"Sipariş #{id} durumu '{yeniDurum}' olarak güncellendi.",
            SiparisId = id,
            YeniDurum = yeniDurum,
            UyeAdi = siparis.Uye != null ? siparis.Uye.UyeAdi + " " + siparis.Uye.UyeSoyadi : "Ziyaretçi",
            UyeId = siparis.UyeId
        });
    }

    // PUT /api/siparisler/5/iptal -> Siparişi iptal eder
    [HttpPut("{id}/iptal")]
    public async Task<IActionResult> SiparisIptal(int id)
    {
        var siparis = await _context.Siparislers
            .Include(s => s.Uye)
            .Include(s => s.SiparisDetays)
                .ThenInclude(sd => sd.Urun)
                    .ThenInclude(u => u.UrunRecetesis)
                        .ThenInclude(ur => ur.Malzeme)
            .FirstOrDefaultAsync(s => s.SiparisId == id);

        if (siparis == null) return NotFound("İptal edilecek sipariş bulunamadı.");

        if (siparis.SiparisDurumu == "TAMAMLANDI" || siparis.SiparisDurumu == "ODENDI")
        {
            return BadRequest("Ödemesi alınmış veya tamamlanmış bir sipariş iptal edilemez.");
        }

        // 🔑 Stok iadesi
        int personelId = siparis.PersonelId ?? 1;
        foreach (var detay in siparis.SiparisDetays)
        {
            var urun = detay.Urun;
            var receteler = urun?.UrunRecetesis;
            if (receteler == null || !receteler.Any()) continue;

            foreach (var recete in receteler)
            {
                var malzeme = recete.Malzeme;
                if (malzeme == null) continue;

                var iadeMiktar = recete.KullanimMiktari * detay.Adet;
                malzeme.StokMiktari += iadeMiktar;

                _context.StokHarekets.Add(new StokHareket
                {
                    UrunId = urun.UrunId,
                    PersonelId = personelId,
                    StokIslemTipi = "GIRIS",
                    StokMiktari = (int)iadeMiktar,
                    IsleminTarihSaati = DateTime.Now,
                    IsleminAciklamasi = $"Sipariş #{siparis.SiparisId} iptal - {urun.UrunAdi} stoğu iade edildi ({detay.Adet} adet)"
                });
            }
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
        await SiparisDegisikligiBildir("iptal");

        return Ok(new
        {
            Mesaj = "Sipariş başarıyla iptal edildi.",
            SiparisId = id,
            UyeAdi = siparis.Uye != null ? siparis.Uye.UyeAdi + " " + siparis.Uye.UyeSoyadi : "Ziyaretçi"
        });
    }

    // DELETE /api/siparisler/5 -> Siparişi siler
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var siparis = await _context.Siparislers
            .Include(s => s.SiparisDetays)
            .Include(s => s.Uye)
            .FirstOrDefaultAsync(s => s.SiparisId == id);

        if (siparis == null) return NotFound("Silinmek istenen sipariş bulunamadı.");

        try
        {
            _context.Siparislers.Remove(siparis);
            await _context.SaveChangesAsync();
            await SiparisDegisikligiBildir("sil");
            return Ok(new
            {
                Mesaj = "Sipariş ve ilişkili tüm detayları sistemden tamamen silindi.",
                SiparisId = id
            });
        }
        catch (DbUpdateException)
        {
            return BadRequest("Bu siparişe bağlı fatura veya ödeme kaydı olduğu için fiziksel olarak silinemez, iptal etmeyi (PUT /iptal) deneyin.");
        }
    }

    // ============================================================
    // ✅ KULLANICI SİPARİŞLERİ (Mobil için)
    // ============================================================
    private int? GetCurrentUyeId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var id) ? id : null;
    }

    private static readonly string[] AktifDurumlar =
        { "BEKLEMEDE", "HAZIRLANIYOR", "HAZIR", "ODENDI", "KURYEDE", "YOLDA" };

    private static readonly string[] GecmisDurumlar =
        { "TESLIM EDILDI", "TAMAMLANDI", "IPTAL", "IADE", "KISMI_IADE" };

    [Authorize]
    [HttpGet("benim-siparislerim")]
    public async Task<IActionResult> BenimSiparislerim()
    {
        var uyeId = GetCurrentUyeId();
        if (uyeId == null) return Unauthorized(new { Mesaj = "Yetkisiz erişim." });

        var siparisler = await _context.Siparislers
            .Include(s => s.Uye)
                .ThenInclude(u => u!.Adres)
            .Where(s => s.UyeId == uyeId && AktifDurumlar.Contains(s.SiparisDurumu))
            .OrderByDescending(s => s.SiparisTarihi)
            .Select(s => new
            {
                s.SiparisId,
                s.SiparisDurumu,
                s.SiparisTipi,
                s.ToplamTutar,
                s.SiparisTarihi,
                MusteriAdi = s.Uye != null ? (s.Uye.UyeAdi + " " + s.Uye.UyeSoyadi) : null,
                MusteriTelefon = s.Uye != null ? s.Uye.UyeTelefon : null,
                MusteriAdres = s.Uye != null && s.Uye.Adres.Any()
                    ? s.Uye.Adres.First().AcikAdres
                    : null,
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
            .ToListAsync();

        return Ok(siparisler);
    }

    [Authorize]
    [HttpGet("gecmis")]
    public async Task<IActionResult> Gecmis()
    {
        var uyeId = GetCurrentUyeId();
        if (uyeId == null) return Unauthorized(new { Mesaj = "Yetkisiz erişim." });

        var siparisler = await _context.Siparislers
            .Include(s => s.Uye)
                .ThenInclude(u => u!.Adres)
            .Where(s => s.UyeId == uyeId && GecmisDurumlar.Contains(s.SiparisDurumu))
            .OrderByDescending(s => s.SiparisTarihi)
            .Select(s => new
            {
                s.SiparisId,
                s.SiparisDurumu,
                s.SiparisTipi,
                s.ToplamTutar,
                s.SiparisTarihi,
                MusteriAdi = s.Uye != null ? (s.Uye.UyeAdi + " " + s.Uye.UyeSoyadi) : null,
                MusteriTelefon = s.Uye != null ? s.Uye.UyeTelefon : null,
                MusteriAdres = s.Uye != null && s.Uye.Adres.Any()
                    ? s.Uye.Adres.First().AcikAdres
                    : null,
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
            .ToListAsync();

        return Ok(siparisler);
    }
}