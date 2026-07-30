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
using System.Threading.Tasks;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MasaController : ControllerBase
{
    private readonly DbRestoranContext _context;
    private readonly IHubContext<SiparisHub> _hubContext;

    public MasaController(DbRestoranContext context, IHubContext<SiparisHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // 🔔 Tüm bağlı istemcilere (Admin, Garson, vs.) "masa verisi değişti,
    // kendi fetchAllData/verileriYukle'ni tekrar çalıştır" sinyali gönderir.
    private async Task MasaDegisikligiBildir(string islem)
    {
        await _hubContext.Clients.All.SendAsync("VeriGuncellendi", new
        {
            tip = "masa",
            islem,
            zaman = DateTime.Now
        });
    }

    // ✅ GET /api/masa - Tüm masaları ve aktif sipariş özetlerini getirir
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var masalar = await _context.Masas
                .Select(m => new
                {
                    m.MasaId,
                    m.MasaNo,
                    m.MasaDurumu,
                    m.Kapasite,

                    // 🔑 Ödeme alınana (ODENDI) veya iptal edilene (IPTAL) kadar sipariş masa üzerinde kalır
                    aktifSiparis = _context.Siparislers
                        .Where(s =>
                            s.MasaId == m.MasaId &&
                            s.SiparisDurumu != "IPTAL" &&
                            s.SiparisDurumu != "ODENDI")
                        .OrderByDescending(s => s.SiparisTarihi)
                        .Select(s => new
                        {
                            siparisId = s.SiparisId,
                            toplam = s.ToplamTutar,
                            siparisDurumu = s.SiparisDurumu,
                            siparisTarihi = s.SiparisTarihi,
                            siparisTipi = s.SiparisTipi,

                            siparisUrunleri = s.SiparisDetays.Select(d => new
                            {
                                urunId = d.UrunId,
                                urunAdi = d.Urun != null ? d.Urun.UrunAdi : "Ürün",
                                adet = d.Adet,
                                fiyat = d.BirimFiyat,
                                detayNot = d.DetayNot,
                                satirToplami = d.Adet * d.BirimFiyat
                            }).ToList()
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(masalar);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Masa listesi alınırken hata oluştu: {ex.Message}");
            return Ok(new List<object>());
        }
    }

    // ✅ GET /api/masa/{id} - Tek bir masayı ve aktif siparişini detaylı getirir
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var masa = await _context.Masas
                .Where(m => m.MasaId == id)
                .Select(m => new
                {
                    m.MasaId,
                    m.MasaNo,
                    m.MasaDurumu,
                    m.Kapasite,

                    aktifSiparisVarMi = _context.Siparislers
                        .Any(s => s.MasaId == m.MasaId &&
                                 s.SiparisDurumu != "IPTAL" &&
                                 s.SiparisDurumu != "ODENDI"),

                    aktifSiparis = _context.Siparislers
                        .Where(s =>
                            s.MasaId == m.MasaId &&
                            s.SiparisDurumu != "IPTAL" &&
                            s.SiparisDurumu != "ODENDI")
                        .OrderByDescending(s => s.SiparisTarihi)
                        .Select(s => new
                        {
                            siparisId = s.SiparisId,
                            toplam = s.ToplamTutar,
                            siparisDurumu = s.SiparisDurumu,
                            siparisTarihi = s.SiparisTarihi,
                            siparisTipi = s.SiparisTipi,

                            siparisUrunleri = s.SiparisDetays.Select(d => new
                            {
                                urunId = d.UrunId,
                                urunAdi = d.Urun != null ? d.Urun.UrunAdi : "Ürün",
                                adet = d.Adet,
                                fiyat = d.BirimFiyat,
                                detayNot = d.DetayNot,
                                satirToplami = d.Adet * d.BirimFiyat
                            }).ToList()
                        })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (masa == null)
                return NotFound(new { Mesaj = "Masa bulunamadı." });

            return Ok(masa);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Masa bulunurken hata oluştu: {ex.Message}");
            return NotFound(new { Mesaj = "Masa bulunamadı." });
        }
    }

    // ✅ POST /api/masa - Yeni Masa Ekleme
    [HttpPost]
    public async Task<IActionResult> MasaEkle([FromBody] MasaEkleDto dto)
    {
        if (dto == null)
            return BadRequest(new { Mesaj = "Masa verileri boş olamaz." });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var masaNoVarMi = await _context.Masas.AnyAsync(m => m.MasaNo == dto.MasaNo);
        if (masaNoVarMi)
            return Conflict(new { Mesaj = $"{dto.MasaNo} numaralı masa zaten tanımlı." });

        var gecerliDurumlar = new[] { "BOŞ", "DOLU", "REZERVE", "ARIZALI", "KULLANIM DIŞI" };
        var durum = dto.MasaDurumu?.Trim().ToUpper(new System.Globalization.CultureInfo("tr-TR"));

        if (string.IsNullOrEmpty(durum) || !gecerliDurumlar.Contains(durum))
            return BadRequest(new { Mesaj = "Geçersiz masa durumu." });

        var masa = new Masa
        {
            MasaNo = dto.MasaNo,
            MasaDurumu = durum,
            Kapasite = dto.Kapasite ?? 4
        };

        _context.Masas.Add(masa);
        await _context.SaveChangesAsync();
        await MasaDegisikligiBildir("ekle");

        return Ok(new
        {
            Mesaj = "Masa başarıyla eklendi.",
            masa.MasaId,
            masa.MasaNo,
            masa.MasaDurumu,
            masa.Kapasite
        });
    }

    // ✅ PUT /api/masa/{id} - Masa Bilgilerini Genel Güncelleme
    [HttpPut("{id}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] MasaGuncelleDto dto)
    {
        if (dto == null)
            return BadRequest(new { Mesaj = "Güncelleme verileri boş olamaz." });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var masa = await _context.Masas.FindAsync(id);
        if (masa == null)
            return NotFound(new { Mesaj = "Güncellenmek istenen masa bulunamadı." });

        if (masa.MasaNo != dto.MasaNo)
        {
            var masaNoVarMi = await _context.Masas
                .AnyAsync(m => m.MasaNo == dto.MasaNo && m.MasaId != id);
            if (masaNoVarMi)
                return Conflict(new { Mesaj = $"{dto.MasaNo} numaralı masa zaten tanımlı." });
        }

        var gecerliDurumlar = new[] { "BOŞ", "DOLU", "REZERVE", "ARIZALI", "KULLANIM DIŞI" };
        var durum = dto.MasaDurumu?.Trim().ToUpper(new System.Globalization.CultureInfo("tr-TR"));

        if (string.IsNullOrEmpty(durum) || !gecerliDurumlar.Contains(durum))
            return BadRequest(new { Mesaj = "Geçersiz masa durumu." });

        masa.MasaNo = dto.MasaNo;
        masa.MasaDurumu = durum;
        if (dto.Kapasite > 0) masa.Kapasite = dto.Kapasite;

        await _context.SaveChangesAsync();
        await MasaDegisikligiBildir("guncelle");

        return Ok(new
        {
            Mesaj = "Masa başarıyla güncellendi.",
            masa.MasaId,
            masa.MasaNo,
            masa.MasaDurumu,
            masa.Kapasite
        });
    }

    // ✅ PUT /api/masa/{id}/durum - Anlık Masa Durumu Güncelleme (Açık Sipariş Güvenlik Kontrollü)
    // ✅ PUT /api/masa/{id}/durum - Anlık Masa Durumu Güncelleme
    [HttpPut("{id}/durum")]
    public async Task<IActionResult> DurumGuncelle(int id, [FromBody] MasaDurumGuncelleDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.MasaDurumu))
            return BadRequest(new { Mesaj = "Masa durumu boş olamaz." });

        var masa = await _context.Masas.FindAsync(id);
        if (masa == null)
            return NotFound(new { Mesaj = "Masa bulunamadı." });

        var durum = dto.MasaDurumu.Trim().ToUpper(new System.Globalization.CultureInfo("tr-TR"));

        var gecerliDurumlar = new[] { "BOŞ", "DOLU", "REZERVE", "ARIZALI", "KULLANIM DIŞI" };
        if (!gecerliDurumlar.Contains(durum))
            return BadRequest(new { Mesaj = "Geçersiz masa durumu." });

        // 🔒 Güvenlik Kontrolü: Açık siparişi olan masa ödeme alınmadan doğrudan "BOŞ" yapılamaz!
        if (durum == "BOŞ")
        {
            var aktifSiparisVarMi = await _context.Siparislers
                .AnyAsync(s => s.MasaId == id &&
                               s.SiparisDurumu != "ODENDI" &&
                               s.SiparisDurumu != "IPTAL");

            if (aktifSiparisVarMi)
                return BadRequest(new { Mesaj = "Bu masada ödenmemiş sipariş var! Masayı boşaltmak için önce ödeme almalı veya siparişi iptal etmelisiniz." });
        }

        masa.MasaDurumu = durum;
        await _context.SaveChangesAsync();
        await MasaDegisikligiBildir("durum");

        return Ok(new
        {
            Mesaj = $"Masa durumu başarıyla '{durum}' olarak güncellendi.",
            masa.MasaId,
            masa.MasaDurumu,
            RezervasyonSaati = dto?.RezervasyonSaati
        });
    }
    // ✅ POST /api/Masa/tasi - Masa Taşıma
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

        if (hedefMasa.MasaDurumu == "ARIZALI" || hedefMasa.MasaDurumu == "KULLANIM DIŞI")
            return BadRequest(new { Mesaj = "Hedef masa kullanılamaz durumda." });

        var aktifSiparis = await _context.Siparislers
            .FirstOrDefaultAsync(s => s.MasaId == dto.KaynakMasaId &&
                                     s.SiparisDurumu != "ODENDI" &&
                                     s.SiparisDurumu != "IPTAL");

        if (aktifSiparis != null)
        {
            aktifSiparis.MasaId = dto.HedefMasaId;
        }

        kaynakMasa.MasaDurumu = "BOŞ";
        hedefMasa.MasaDurumu = "DOLU";

        await _context.SaveChangesAsync();
        await MasaDegisikligiBildir("tasi");

        return Ok(new { Mesaj = $"{kaynakMasa.MasaNo} masası başarıyla {hedefMasa.MasaNo} masasına taşındı." });
    }

    // ✅ DELETE /api/masa/{id} - Masa Silme
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var masa = await _context.Masas.FindAsync(id);
        if (masa == null)
            return NotFound(new { Mesaj = "Masa bulunamadı." });

        if (masa.MasaDurumu == "DOLU")
            return BadRequest(new { Mesaj = "Dolu bir masa silinemez." });

        if (masa.MasaDurumu == "REZERVE")
            return BadRequest(new { Mesaj = "Rezerve bir masa silinemez." });

        var siparisVarMi = await _context.Siparislers.AnyAsync(s => s.MasaId == id);
        if (siparisVarMi)
            return BadRequest(new { Mesaj = "Bu masaya ait geçmiş sipariş kayıtları var, silinemez." });

        _context.Masas.Remove(masa);
        await _context.SaveChangesAsync();
        await MasaDegisikligiBildir("sil");

        return Ok(new { Mesaj = "Masa sistemden başarıyla silindi.", MasaId = id });
    }
}