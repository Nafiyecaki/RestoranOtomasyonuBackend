using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.Data;
using Restoran.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Restoran.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MalzemeTalepController : ControllerBase
    {
        private readonly DbRestoranContext _context;

        public MalzemeTalepController(DbRestoranContext context)
        {
            _context = context;
        }

        // ============================================================
        // 1. Yeni Talep Oluştur (Aşçı)
        // ============================================================
        [HttpPost("talep-olustur")]
        public async Task<IActionResult> TalepOlustur([FromBody] MalzemeTalepOlusturDto dto)
        {
            try
            {
                // ✅ DÜZELTİLDİ: Malzemelers kullan
                var malzeme = await _context.Malzemelers.FindAsync(dto.MalzemeId);
                if (malzeme == null)
                    return NotFound("Malzeme bulunamadı.");

                var talepEden = "Aşçı";
                if (dto.PersonelId.HasValue)
                {
                    var personel = await _context.Personels
                        .Where(p => p.PersonelId == dto.PersonelId.Value)
                        .Select(p => p.PersonelAdi + " " + p.PersonelSoyadi)
                        .FirstOrDefaultAsync();
                    if (!string.IsNullOrEmpty(personel))
                        talepEden = personel;
                }

                var talep = new MalzemeTalep
                {
                    MalzemeId = dto.MalzemeId,
                    Miktar = dto.Miktar,
                    Birim = dto.Birim,
                    TalepEden = talepEden,
                    PersonelId = dto.PersonelId,
                    Durum = "BEKLIYOR",
                    Aciklama = dto.Aciklama,
                    TalepTarihi = DateTime.Now,
                    IsActive = true
                };

                await _context.MalzemeTalepleri.AddAsync(talep);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Malzeme talebi başarıyla oluşturuldu.",
                    talepId = talep.TalepId,
                    durum = talep.Durum
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Sunucu hatası: {ex.Message}");
            }
        }

        // ============================================================
        // 2. Bekleyen Talepleri Getir (Admin)
        // ============================================================
        [HttpGet("bekleyen-talepler")]
        public async Task<IActionResult> GetBekleyenTalepler()
        {
            var talepler = await _context.MalzemeTalepleri
                .Where(t => t.Durum == "BEKLIYOR" && t.IsActive == true)
                .Include(t => t.Malzeme)
                .OrderByDescending(t => t.TalepTarihi)
                .Select(t => new MalzemeTalepDto
                {
                    TalepId = t.TalepId,
                    MalzemeId = t.MalzemeId,
                    MalzemeAdi = t.Malzeme.MalzemeAdi,
                    Miktar = t.Miktar,
                    Birim = t.Birim,
                    TalepEden = t.TalepEden,
                    PersonelId = t.PersonelId,
                    Durum = t.Durum,
                    Aciklama = t.Aciklama,
                    TalepTarihi = t.TalepTarihi
                })
                .ToListAsync();

            return Ok(talepler);
        }

        // ============================================================
        // 3. Tüm Talepleri Getir (Admin)
        // ============================================================
        [HttpGet("tum-talepler")]
        public async Task<IActionResult> GetAllTalepler([FromQuery] string? durum = null)
        {
            var query = _context.MalzemeTalepleri
                .Include(t => t.Malzeme)
                .Where(t => t.IsActive == true);

            if (!string.IsNullOrEmpty(durum))
            {
                query = query.Where(t => t.Durum == durum);
            }

            var talepler = await query
                .OrderByDescending(t => t.TalepTarihi)
                .Select(t => new MalzemeTalepDto
                {
                    TalepId = t.TalepId,
                    MalzemeId = t.MalzemeId,
                    MalzemeAdi = t.Malzeme.MalzemeAdi,
                    Miktar = t.Miktar,
                    Birim = t.Birim,
                    TalepEden = t.TalepEden,
                    PersonelId = t.PersonelId,
                    Durum = t.Durum,
                    Aciklama = t.Aciklama,
                    TalepTarihi = t.TalepTarihi,
                    CevaplamaTarihi = t.CevaplamaTarihi,
                    Cevaplayan = t.Cevaplayan
                })
                .ToListAsync();

            return Ok(talepler);
        }

        // ============================================================
        // 4. Talebi Cevapla (Admin - Onayla/Reddet)
        // ============================================================
        [HttpPut("talep-cevapla/{talepId}")]
        public async Task<IActionResult> TalepCevapla(int talepId, [FromBody] MalzemeTalepCevaplaDto dto)
        {
            try
            {
                var talep = await _context.MalzemeTalepleri.FindAsync(talepId);
                if (talep == null)
                    return NotFound("Talep bulunamadı.");

                if (talep.Durum != "BEKLIYOR")
                    return BadRequest($"Bu talep zaten '{talep.Durum}' durumunda.");

                if (dto.Durum != "ONAYLANDI" && dto.Durum != "REDDEDILDI")
                    return BadRequest("Geçersiz durum. ONAYLANDI veya REDDEDILDI olmalı.");

                talep.Durum = dto.Durum;
                talep.CevaplamaTarihi = DateTime.Now;
                talep.Cevaplayan = dto.Cevaplayan ?? "Admin";

                // ✅ DÜZELTİLDİ: Stok işlemini kaldırdım (Stok tablosu yok)
                // Onaylandıysa Malzeme stok miktarını güncelle
                if (dto.Durum == "ONAYLANDI")
                {
                    var malzeme = await _context.Malzemelers.FindAsync(talep.MalzemeId);
                    if (malzeme != null)
                    {
                        malzeme.StokMiktari += talep.Miktar;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Talep #{talepId} {dto.Durum} olarak cevaplandı.",
                    talepId = talep.TalepId,
                    durum = talep.Durum
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Sunucu hatası: {ex.Message}");
            }
        }

        // ============================================================
        // 5. Talebi Sil (Admin)
        // ============================================================
        [HttpDelete("talep-sil/{talepId}")]
        public async Task<IActionResult> TalepSil(int talepId)
        {
            var talep = await _context.MalzemeTalepleri.FindAsync(talepId);
            if (talep == null)
                return NotFound("Talep bulunamadı.");

            talep.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Talep #{talepId} silindi." });
        }
    }
}