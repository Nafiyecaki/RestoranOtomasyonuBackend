using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.Data;
using Restoran.Data.Entities;
using System.Threading.Tasks;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MasaController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public MasaController(DbRestoranContext context)
    {
        _context = context;
    }

    // GET /api/masa
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
       
            var masalar = await _context.Masas
            .Select(m => new
            {
                m.MasaId,
                m.MasaNo,
                m.MasaDurumu,
                m.Kapasite,

                aktifSiparis = _context.Siparislers
                    .Where(s =>
                        s.MasaId == m.MasaId &&
                        s.SiparisDurumu != "IPTAL" &&
                        s.SiparisDurumu != "ODENDI")
                    .Select(s => new
                    {
                        siparisId = s.SiparisId,
                        toplam = s.ToplamTutar,
                        siparisDurumu = s.SiparisDurumu,
                        siparisTarihi = s.SiparisTarihi,

                        siparisUrunleri = s.SiparisDetays.Select(d => new
                        {
                            urunId = d.UrunId,
                            urunAdi = d.Urun.UrunAdi,
                            adet = d.Adet,
                            fiyat = d.BirimFiyat,
                            detayNot = d.DetayNot
                        }).ToList()

                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(masalar);
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var masa = await _context.Masas
            .Where(m => m.MasaId == id)
            .Select(m => new
            {
                m.MasaId,
                m.MasaNo,
                m.MasaDurumu,
                m.Kapasite,

                aktifSiparis = _context.Siparislers
                    .Where(s =>
                        s.MasaId == m.MasaId &&
                        s.SiparisDurumu != "IPTAL" &&
                        s.SiparisDurumu != "ODENDI")
                    .Select(s => new
                    {
                        siparisId = s.SiparisId,
                        toplam = s.ToplamTutar,
                        siparisDurumu = s.SiparisDurumu,
                        siparisTarihi = s.SiparisTarihi,

                        siparisUrunleri = s.SiparisDetays.Select(d => new
                        {
                            urunId = d.UrunId,
                            urunAdi = d.Urun.UrunAdi,
                            adet = d.Adet,
                            fiyat = d.BirimFiyat,
                            detayNot = d.DetayNot
                        }).ToList()
                    })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (masa == null)
            return NotFound();

        return Ok(masa);
    }
    // POST /api/masa
    [HttpPost]
    public async Task<IActionResult> MasaEkle([FromBody] MasaEkleDto dto)
    {
        if (dto == null) return BadRequest("Masa verileri boş olamaz.");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var masaNoVarMi = await _context.Masas.AnyAsync(m => m.MasaNo == dto.MasaNo);
        if (masaNoVarMi)
            return Conflict(new { Mesaj = $"{dto.MasaNo} numaralı masa zaten tanımlı." });

        var gecerliDurumlar = new[] { "BOŞ", "DOLU", "REZERVE", "ARIZALI", "KULLANIM DIŞI" };
        var durum = dto.MasaDurumu?.Trim()
            .ToUpper(new System.Globalization.CultureInfo("tr-TR"));

        if (string.IsNullOrEmpty(durum) || !gecerliDurumlar.Contains(durum))
            return BadRequest(new { Mesaj = "Geçersiz masa durumu." });

        var masa = new Masa
        {
            MasaNo = dto.MasaNo,
            MasaDurumu = durum,
            Kapasite = dto.Kapasite
        };

        _context.Masas.Add(masa);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Masa başarıyla eklendi.",
            masa.MasaId,
            masa.MasaNo,
            masa.MasaDurumu,
            masa.Kapasite
        });
    }

    // PUT /api/masa/{id} -> Genel Güncelleme
    [HttpPut("{id}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] MasaGuncelleDto dto)
    {
        if (dto == null) return BadRequest("Güncelleme verileri boş olamaz.");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var masa = await _context.Masas.FindAsync(id);
        if (masa == null) return NotFound(new { Mesaj = "Güncellenmek istenen masa bulunamadı." });

        if (masa.MasaNo != dto.MasaNo)
        {
            var masaNoVarMi = await _context.Masas
                .AnyAsync(m => m.MasaNo == dto.MasaNo && m.MasaId != id);
            if (masaNoVarMi)
                return Conflict(new { Mesaj = $"{dto.MasaNo} numaralı masa zaten tanımlı." });
        }

        var gecerliDurumlar = new[] { "BOŞ", "DOLU", "REZERVE", "ARIZALI", "KULLANIM DIŞI" };
        var durum = dto.MasaDurumu?.Trim()
            .ToUpper(new System.Globalization.CultureInfo("tr-TR"));

        if (string.IsNullOrEmpty(durum) || !gecerliDurumlar.Contains(durum))
            return BadRequest(new { Mesaj = "Geçersiz masa durumu." });

        masa.MasaNo = dto.MasaNo;
        masa.MasaDurumu = durum;
        if (dto.Kapasite > 0) masa.Kapasite = dto.Kapasite;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Masa başarıyla güncellendi.",
            masa.MasaId,
            masa.MasaNo,
            masa.MasaDurumu,
            masa.Kapasite
        });
    }

    // PUT /api/masa/{id}/durum -> Anlık Durum Güncelleme
    [HttpPut("{id}/durum")]
    public async Task<IActionResult> DurumGuncelle(int id, [FromBody] MasaGuncelleDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.MasaDurumu))
            return BadRequest(new { Mesaj = "Masa durumu boş olamaz." });

        var masa = await _context.Masas.FindAsync(id);
        if (masa == null) return NotFound(new { Mesaj = "Masa bulunamadı." });

        var durum = dto.MasaDurumu.Trim().ToUpper(new System.Globalization.CultureInfo("tr-TR"));

        var gecerliDurumlar = new[] { "BOŞ", "DOLU", "REZERVE", "ARIZALI", "KULLANIM DIŞI" };
        if (!gecerliDurumlar.Contains(durum))
        {
            return BadRequest(new { Mesaj = "Geçersiz masa durumu." });
        }

        masa.MasaDurumu = durum;

        // Masa "BOŞ" yapılıyorsa masanın arka planda açık kalmış siparişlerini IPTAL yapıyoruz
        if (durum == "BOŞ")
        {
            var acikSiparisler = await _context.Siparislers
                .Where(s => s.MasaId == id && s.SiparisDurumu != "ODENDI" && s.SiparisDurumu != "IPTAL")
                .ToListAsync();

            foreach (var siparis in acikSiparisler)
            {
                siparis.SiparisDurumu = "IPTAL";
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = $"Masa durumu başarıyla '{durum}' olarak güncellendi.",
            masa.MasaId,
            masa.MasaDurumu
        });
    }

    // POST /api/Masa/tasi
    [HttpPost("tasi")]
    public async Task<IActionResult> MasaTasi([FromBody] MasaTasiDto dto)
    {
        if (dto == null || dto.KaynakMasaId <= 0 || dto.HedefMasaId <= 0)
            return BadRequest(new { Mesaj = "Kaynak ve hedef masa seçimi geçersiz." });

        if (dto.KaynakMasaId == dto.HedefMasaId)
            return BadRequest(new { Mesaj = "Kaynak masa ile hedef masa aynı olamaz." });

        var kaynakMasa = await _context.Masas.FindAsync(dto.KaynakMasaId);
        var hedefMasa = await _context.Masas.FindAsync(dto.HedefMasaId);

        if (kaynakMasa == null || hedefMasa == null)
            return NotFound(new { Mesaj = "Masalardan biri bulunamadı." });

        if (kaynakMasa.MasaDurumu != "DOLU")
            return BadRequest(new { Mesaj = "Kaynak masa dolu değil." });

        if (hedefMasa.MasaDurumu == "DOLU")
            return BadRequest(new { Mesaj = "Hedef masa zaten dolu." });

        var aktifSiparis = await _context.Siparislers
            .FirstOrDefaultAsync(s => s.MasaId == dto.KaynakMasaId &&
                                     s.SiparisDurumu != "ODENDI" &&
                                     s.SiparisDurumu != "IPTAL" &&
                                     s.SiparisDurumu != "TAMAMLANDI");

        if (aktifSiparis != null)
        {
            aktifSiparis.MasaId = dto.HedefMasaId;
        }

        kaynakMasa.MasaDurumu = "BOŞ";
        hedefMasa.MasaDurumu = "DOLU";

        await _context.SaveChangesAsync();

        return Ok(new { Mesaj = $"{kaynakMasa.MasaNo} masası başarıyla {hedefMasa.MasaNo} masasına taşındı." });
    }

    // DELETE /api/masa/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var masa = await _context.Masas.FindAsync(id);
        if (masa == null) return NotFound(new { Mesaj = "Masa bulunamadı." });

        if (masa.MasaDurumu == "DOLU")
            return BadRequest(new { Mesaj = "Dolu bir masa silinemez." });

        var siparisVarMi = await _context.Siparislers.AnyAsync(s => s.MasaId == id);
        if (siparisVarMi)
            return BadRequest(new { Mesaj = "Bu masaya ait geçmiş sipariş kayıtları var, silinemez." });

        _context.Masas.Remove(masa);
        await _context.SaveChangesAsync();

        return Ok(new { Mesaj = "Masa sistemden başarıyla silindi.", MasaId = id });
    }
}