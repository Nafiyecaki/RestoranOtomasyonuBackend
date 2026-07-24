using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.API.Dtos.Restoran.API.Dtos;
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
                // Malzeme kontrolü
                var malzeme = await _context.Malzemelers.FindAsync(dto.MalzemeId);
                if (malzeme == null)
                {
                    return NotFound(new { message = "Malzeme bulunamadı." });
                }

                // Talep eden kişiyi bul
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
                    Miktar = (int)dto.Miktar,
                    Birim = dto.Birim ?? "adet",
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
                    success = true,
                    message = "Malzeme talebi başarıyla oluşturuldu.",
                    talepId = talep.TalepId,
                    durum = talep.Durum
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // ============================================================
        // 2. Bekleyen Talepleri Getir (Admin)
        // ============================================================
        [HttpGet("bekleyen-talepler")]
        public async Task<IActionResult> GetBekleyenTalepler()
        {
            try
            {
                var talepler = await _context.MalzemeTalepleri
                    .Where(t => t.Durum == "BEKLIYOR" && t.IsActive == true)
                    .Include(t => t.Malzeme)
                    .OrderByDescending(t => t.TalepTarihi)
                    .Select(t => new
                    {
                        t.TalepId,
                        t.MalzemeId,
                        MalzemeAdi = t.Malzeme.MalzemeAdi,
                        t.Miktar,
                        t.Birim,
                        t.TalepEden,
                        t.PersonelId,
                        t.Durum,
                        t.Aciklama,
                        t.TalepTarihi
                    })
                    .ToListAsync();

                return Ok(talepler);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // ============================================================
        // 3. Tüm Talepleri Getir (Admin)
        // ============================================================
        [HttpGet("tum-talepler")]
        public async Task<IActionResult> GetAllTalepler([FromQuery] string? durum = null)
        {
            try
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
                    .Select(t => new
                    {
                        t.TalepId,
                        t.MalzemeId,
                        MalzemeAdi = t.Malzeme.MalzemeAdi,
                        t.Miktar,
                        t.Birim,
                        t.TalepEden,
                        t.PersonelId,
                        t.Durum,
                        t.Aciklama,
                        t.TalepTarihi,
                        t.CevaplamaTarihi,
                        t.Cevaplayan
                    })
                    .ToListAsync();

                return Ok(talepler);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
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
                    return NotFound(new { message = "Talep bulunamadı." });

                if (talep.Durum != "BEKLIYOR")
                    return BadRequest(new { message = $"Bu talep zaten '{talep.Durum}' durumunda." });

                if (dto.Durum != "ONAYLANDI" && dto.Durum != "REDDEDILDI")
                    return BadRequest(new { message = "Geçersiz durum. ONAYLANDI veya REDDEDILDI olmalı." });

                talep.Durum = dto.Durum;
                talep.CevaplamaTarihi = DateTime.Now;
                talep.Cevaplayan = dto.Cevaplayan ?? "Admin";

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
                    success = true,
                    message = $"Talep #{talepId} {dto.Durum} olarak cevaplandı.",
                    talepId = talep.TalepId,
                    durum = talep.Durum
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // ============================================================
        // 5. Talebi Sil (Admin)
        // ============================================================
        [HttpDelete("talep-sil/{talepId}")]
        public async Task<IActionResult> TalepSil(int talepId)
        {
            try
            {
                var talep = await _context.MalzemeTalepleri.FindAsync(talepId);
                if (talep == null)
                    return NotFound(new { message = "Talep bulunamadı." });

                talep.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"Talep #{talepId} silindi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }
    }
}