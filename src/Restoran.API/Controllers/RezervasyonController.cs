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
public class RezervasyonController : ControllerBase
{
    private readonly DbRestoranContext _context;

    public RezervasyonController(DbRestoranContext context)
    {
        _context = context;
    }

    // GET /api/Rezervasyon
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var rezervasyonlar = await _context.Rezervasyons
            .OrderByDescending(r => r.TarihSaat)
            .Select(r => new
            {
                r.RezervasyonId,
                r.MusteriAdi,
                r.MusteriSoyadi,
                r.Telefon,
                r.KisiSayisi,
                r.TarihSaat,
                r.Durum,
                r.OlusturulmaTarihi,
                r.Aciklama,
                r.MasaId,
                r.RezervasyonTipi,
                r.UyeId,
                MasaNo = r.Masa != null ? r.Masa.MasaNo : null,
                UyeAdi = r.Uye != null ? r.Uye.UyeAdi + " " + r.Uye.UyeSoyadi : null
            })
            .ToListAsync();

        return Ok(rezervasyonlar);
    }

    // GET /api/Rezervasyon/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var rezervasyon = await _context.Rezervasyons
            .Where(r => r.RezervasyonId == id)
            .Select(r => new
            {
                r.RezervasyonId,
                r.MusteriAdi,
                r.MusteriSoyadi,
                r.Telefon,
                r.KisiSayisi,
                r.TarihSaat,
                r.Durum,
                r.OlusturulmaTarihi,
                r.Aciklama,
                r.MasaId,
                r.RezervasyonTipi,
                r.UyeId,
                MasaNo = r.Masa != null ? r.Masa.MasaNo : null,
                UyeAdi = r.Uye != null ? r.Uye.UyeAdi + " " + r.Uye.UyeSoyadi : null
            })
            .FirstOrDefaultAsync();

        if (rezervasyon == null)
            return NotFound(new { Mesaj = "Rezervasyon bulunamadı." });

        return Ok(rezervasyon);
    }

    // POST /api/Rezervasyon
    [HttpPost]
    public async Task<IActionResult> RezervasyonEkle([FromBody] RezervasyonEkleDto dto)
    {
        if (dto == null)
            return BadRequest(new { Mesaj = "Veri boş olamaz." });

        // ✅ 1. TARİH SAAT BİRLEŞTİRME
        DateTime tarihSaat;
        if (dto.TarihSaat.HasValue)
        {
            tarihSaat = dto.TarihSaat.Value;
        }
        else if (!string.IsNullOrEmpty(dto.Tarih) && !string.IsNullOrEmpty(dto.Saat))
        {
            if (!DateTime.TryParse($"{dto.Tarih} {dto.Saat}", out tarihSaat))
            {
                return BadRequest(new { Mesaj = "Geçersiz tarih veya saat formatı." });
            }
        }
        else
        {
            return BadRequest(new { Mesaj = "Tarih ve saat bilgisi zorunludur." });
        }

        // ✅ 2. ZAMAN KONTROLÜ
        if (tarihSaat < DateTime.Now)
        {
            return BadRequest(new { Mesaj = "Geçmiş bir tarihe veya saate rezervasyon oluşturulamaz." });
        }

        // ✅ 3. MASA ZORUNLULUK KONTROLÜ
        if (!dto.MasaId.HasValue)
        {
            return BadRequest(new { Mesaj = "Rezervasyon işlemi için bir masa seçilmesi zorunludur." });
        }

        var masa = await _context.Masas.FindAsync(dto.MasaId.Value);
        if (masa == null)
        {
            return BadRequest(new { Mesaj = $"ID'si {dto.MasaId.Value} olan masa sistemde bulunamadı." });
        }

        // ✅ 4. MASA DURUMU KONTROLÜ
        if (masa.MasaDurumu == "ARIZALI" || masa.MasaDurumu == "KULLANIM DIŞI")
        {
            return BadRequest(new { Mesaj = $"{masa.MasaNo} numaralı masa şu anda kullanılamıyor. Durum: {masa.MasaDurumu}" });
        }

        // ✅ 5. ÇAKIŞMA KONTROLÜ (2 saat aralık)
        var cakismaVarMi = await _context.Rezervasyons.AnyAsync(r =>
            r.MasaId == dto.MasaId.Value &&
            r.Durum != "IPTAL" &&
            r.Durum != "REDDEDILDI" &&
            r.Durum != "TAMAMLANDI" &&
            r.TarihSaat >= tarihSaat.AddHours(-2) &&
            r.TarihSaat <= tarihSaat.AddHours(2)
        );

        if (cakismaVarMi)
        {
            return BadRequest(new
            {
                Mesaj = $"{masa.MasaNo} numaralı masa, belirtilen saat aralığında başka bir müşteriye rezerve edilmiş durumda."
            });
        }

        // ✅ 6. ÜYE KONTROLÜ (opsiyonel)
        if (dto.UyeId.HasValue)
        {
            var uyeVarMi = await _context.Uyelers.AnyAsync(u => u.UyeId == dto.UyeId.Value);
            if (!uyeVarMi)
                return NotFound(new { Mesaj = $"ID'si {dto.UyeId.Value} olan üye bulunamadı." });
        }

        // ✅ 7. REZERVASYON OLUŞTUR
        var rezervasyon = new Rezervasyon
        {
            MusteriAdi = dto.MusteriAdi ?? "Misafir",
            MusteriSoyadi = dto.MusteriSoyadi ?? "",
            Telefon = dto.Telefon ?? "0",
            KisiSayisi = dto.KisiSayisi ?? 2,
            TarihSaat = tarihSaat,
            Durum = dto.Durum ?? "BEKLEMEDE",
            OlusturulmaTarihi = DateTime.Now,
            Aciklama = dto.Aciklama,
            MasaId = dto.MasaId.Value,
            RezervasyonTipi = dto.RezervasyonTipi ?? "WEB",
            UyeId = dto.UyeId
        };

        _context.Rezervasyons.Add(rezervasyon);

        // ✅ 8. MASA DURUMUNU REZERVE YAP
        // Masa zaten aktif bir siparişle DOLU ise dokunma; sadece boştaysa REZERVE'e çek.
        if (masa.MasaDurumu != "DOLU" &&
            masa.MasaDurumu != "ARIZALI" &&
            masa.MasaDurumu != "KULLANIM DIŞI")
        {
            masa.MasaDurumu = "REZERVE";
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = "Rezervasyon başarıyla oluşturuldu.",
            rezervasyon.RezervasyonId,
            rezervasyon.MusteriAdi,
            rezervasyon.TarihSaat,
            rezervasyon.Durum,
            MasaNo = masa.MasaNo
        });
    }

    // PUT /api/Rezervasyon/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] RezervasyonGuncelleDto dto)
    {
        // ✅ LOG - Gelen veriyi kontrol et
        Console.WriteLine("=== REZERVASYON GÜNCELLEME ===");
        Console.WriteLine($"ID: {id}");
        Console.WriteLine($"DTO MasaId: {dto?.MasaId}");
        Console.WriteLine($"DTO MusteriAdi: {dto?.MusteriAdi}");
        Console.WriteLine($"DTO KisiSayisi: {dto?.KisiSayisi}");
        Console.WriteLine($"DTO TarihSaat: {dto?.TarihSaat}");

        if (dto == null)
            return BadRequest(new { Mesaj = "Veri boş olamaz." });

        var rezervasyon = await _context.Rezervasyons.FindAsync(id);
        if (rezervasyon == null)
            return NotFound(new { Mesaj = "Güncellenmek istenen rezervasyon kaydı bulunamadı." });

        // ✅ 1. TARİH SAAT İŞLEME
        DateTime? yeniTarihSaat = null;

        if (!string.IsNullOrEmpty(dto.TarihSaat))
        {
            if (DateTime.TryParse(dto.TarihSaat, out var parsedDate))
            {
                yeniTarihSaat = parsedDate;
                Console.WriteLine($"✅ TarihSaat parse edildi: {parsedDate}");
            }
            else
            {
                return BadRequest(new { Mesaj = $"Geçersiz tarih formatı: {dto.TarihSaat}" });
            }
        }
        else if (!string.IsNullOrEmpty(dto.Tarih) && !string.IsNullOrEmpty(dto.Saat))
        {
            if (DateTime.TryParse($"{dto.Tarih} {dto.Saat}", out var parsedDate))
            {
                yeniTarihSaat = parsedDate;
                Console.WriteLine($"✅ Tarih+Saat birleştirildi: {parsedDate}");
            }
            else
            {
                return BadRequest(new { Mesaj = $"Geçersiz tarih veya saat formatı: {dto.Tarih} {dto.Saat}" });
            }
        }

        // ✅ 2. ZAMAN KONTROLÜ (sadece yeni tarih varsa)
        if (yeniTarihSaat.HasValue && yeniTarihSaat.Value < DateTime.Now)
        {
            return BadRequest(new { Mesaj = "Geçmiş bir tarihe güncelleme yapılamaz." });
        }

        // ✅ 3. MASA KONTROLÜ (değiştiyse)
        if (dto.MasaId.HasValue && dto.MasaId.Value > 0)
        {
            var masa = await _context.Masas.FindAsync(dto.MasaId.Value);
            if (masa == null)
            {
                return BadRequest(new { Mesaj = $"ID'si {dto.MasaId.Value} olan masa sistemde bulunamadı." });
            }

            if (masa.MasaDurumu == "ARIZALI" || masa.MasaDurumu == "KULLANIM DIŞI")
            {
                return BadRequest(new { Mesaj = $"{masa.MasaNo} numaralı masa şu anda kullanılamıyor." });
            }

            // ✅ Masa değiştiyse: eski masayı boşalt, yeni masayı rezerve et
            if (rezervasyon.MasaId != dto.MasaId.Value)
            {
                var eskiMasaId = rezervasyon.MasaId;
                var eskiMasa = await _context.Masas.FindAsync(eskiMasaId);
                if (eskiMasa != null && eskiMasa.MasaDurumu == "REZERVE")
                {
                    var eskiMasadaBaskaRezervasyonVar = await _context.Rezervasyons.AnyAsync(r =>
                        r.MasaId == eskiMasaId &&
                        r.RezervasyonId != id &&
                        r.Durum != "IPTAL" &&
                        r.Durum != "REDDEDILDI" &&
                        r.Durum != "TAMAMLANDI"
                    );

                    if (!eskiMasadaBaskaRezervasyonVar)
                    {
                        eskiMasa.MasaDurumu = "BOŞ";
                    }
                }

                if (masa.MasaDurumu != "DOLU")
                {
                    masa.MasaDurumu = "REZERVE";
                }
            }

            rezervasyon.MasaId = dto.MasaId.Value;
            Console.WriteLine($"✅ MasaId güncellendi: {dto.MasaId.Value}");
        }

        // ✅ 4. ÇAKIŞMA KONTROLÜ (masa ve tarih değiştiyse)
        if (dto.MasaId.HasValue && dto.MasaId.Value > 0 && yeniTarihSaat.HasValue)
        {
            var cakismaVarMi = await _context.Rezervasyons.AnyAsync(r =>
                r.MasaId == dto.MasaId.Value &&
                r.RezervasyonId != id &&
                r.Durum != "IPTAL" &&
                r.Durum != "REDDEDILDI" &&
                r.Durum != "TAMAMLANDI" &&
                r.TarihSaat >= yeniTarihSaat.Value.AddHours(-2) &&
                r.TarihSaat <= yeniTarihSaat.Value.AddHours(2)
            );

            if (cakismaVarMi)
            {
                var masa = await _context.Masas.FindAsync(dto.MasaId.Value);
                return BadRequest(new
                {
                    Mesaj = $"{masa?.MasaNo} numaralı masa güncellemek istediğiniz saat diliminde doludur."
                });
            }
        }

        // ✅ 5. GÜNCELLE - SADECE GELEN ALANLARI GÜNCELLE
        if (!string.IsNullOrEmpty(dto.MusteriAdi))
        {
            rezervasyon.MusteriAdi = dto.MusteriAdi;
            Console.WriteLine($"✅ MusteriAdi güncellendi: {dto.MusteriAdi}");
        }

        if (!string.IsNullOrEmpty(dto.MusteriSoyadi))
        {
            rezervasyon.MusteriSoyadi = dto.MusteriSoyadi;
            Console.WriteLine($"✅ MusteriSoyadi güncellendi: {dto.MusteriSoyadi}");
        }

        if (!string.IsNullOrEmpty(dto.Telefon))
        {
            rezervasyon.Telefon = dto.Telefon;
            Console.WriteLine($"✅ Telefon güncellendi: {dto.Telefon}");
        }

        if (dto.KisiSayisi.HasValue && dto.KisiSayisi.Value > 0)
        {
            rezervasyon.KisiSayisi = dto.KisiSayisi.Value;
            Console.WriteLine($"✅ KisiSayisi güncellendi: {dto.KisiSayisi.Value}");
        }

        if (!string.IsNullOrEmpty(dto.Aciklama))
        {
            rezervasyon.Aciklama = dto.Aciklama;
            Console.WriteLine($"✅ Aciklama güncellendi: {dto.Aciklama}");
        }

        if (!string.IsNullOrEmpty(dto.RezervasyonTipi))
        {
            rezervasyon.RezervasyonTipi = dto.RezervasyonTipi;
            Console.WriteLine($"✅ RezervasyonTipi güncellendi: {dto.RezervasyonTipi}");
        }

        if (!string.IsNullOrEmpty(dto.Durum))
        {
            rezervasyon.Durum = dto.Durum;
            Console.WriteLine($"✅ Durum güncellendi: {dto.Durum}");
        }

        // ✅ TarihSaat'i güncelle (eğer değiştiyse)
        if (yeniTarihSaat.HasValue)
        {
            rezervasyon.TarihSaat = yeniTarihSaat.Value;
            Console.WriteLine($"✅ TarihSaat güncellendi: {yeniTarihSaat.Value}");
        }

        // ✅ 6. KAYDET
        try
        {
            int affectedRows = await _context.SaveChangesAsync();
            Console.WriteLine($"✅ {affectedRows} satır güncellendi!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Kaydetme hatası: {ex.Message}");
            Console.WriteLine($"📚 StackTrace: {ex.StackTrace}");
            return StatusCode(500, new { Mesaj = $"Veritabanı hatası: {ex.Message}" });
        }

        // ✅ Güncel rezervasyonu getir ve dön
        var guncel = await _context.Rezervasyons.FindAsync(id);

        return Ok(new
        {
            Mesaj = "Rezervasyon bilgileri başarıyla güncellendi.",
            RezervasyonId = guncel?.RezervasyonId,
            KisiSayisi = guncel?.KisiSayisi,
            TarihSaat = guncel?.TarihSaat,
            Durum = guncel?.Durum,
            MusteriAdi = guncel?.MusteriAdi,
            MasaId = guncel?.MasaId
        });
    }

    // PUT /api/Rezervasyon/{id}/durum
    [HttpPut("{id}/durum")]
    public async Task<IActionResult> DurumGuncelle(int id, [FromBody] RezervasyonDurumGuncelleDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Durum))
            return BadRequest(new { Mesaj = "Durum bilgisi boş olamaz." });

        var rezervasyon = await _context.Rezervasyons.FindAsync(id);
        if (rezervasyon == null)
            return NotFound(new { Mesaj = "Durumu güncellenmek istenen rezervasyon bulunamadı." });

        // ✅ Geçerli durumlar
        var gecerliDurumlar = new[] { "BEKLEMEDE", "ONAYLANDI", "IPTAL", "REDDEDILDI", "TAMAMLANDI" };

        // ✅ Türkçe karakter dönüşümü
        var yeniDurum = dto.Durum
            .ToUpperInvariant()
            .Trim()
            .Replace('İ', 'I')
            .Replace('Ö', 'O')
            .Replace('Ü', 'U')
            .Replace('Ş', 'S')
            .Replace('Ç', 'C')
            .Replace('Ğ', 'G');

        if (!gecerliDurumlar.Contains(yeniDurum))
        {
            return BadRequest(new
            {
                Mesaj = $"Geçersiz rezervasyon durumu. Geçerli değerler: {string.Join(", ", gecerliDurumlar)}"
            });
        }

        // ✅ Rezervasyon sona erdiyse (tamamlandı/iptal/reddedildi), masayı boşalt
        var sonaErenDurumlar = new[] { "TAMAMLANDI", "IPTAL", "REDDEDILDI" };
        if (sonaErenDurumlar.Contains(yeniDurum) && !sonaErenDurumlar.Contains(rezervasyon.Durum))
        {
            var masa = await _context.Masas.FindAsync(rezervasyon.MasaId);
            if (masa != null && masa.MasaDurumu == "REZERVE")
            {
                var baskaRezervasyonVar = await _context.Rezervasyons.AnyAsync(r =>
                    r.MasaId == rezervasyon.MasaId &&
                    r.RezervasyonId != id &&
                    r.Durum != "IPTAL" &&
                    r.Durum != "REDDEDILDI" &&
                    r.Durum != "TAMAMLANDI"
                );

                if (!baskaRezervasyonVar)
                {
                    masa.MasaDurumu = "BOŞ";
                }
            }
        }

        rezervasyon.Durum = yeniDurum;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mesaj = $"Rezervasyon durumu başarıyla '{yeniDurum}' olarak güncellendi.",
            RezervasyonId = id,
            YeniDurum = yeniDurum
        });
    }

    // DELETE /api/Rezervasyon/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var rezervasyon = await _context.Rezervasyons
            .Include(r => r.Masa)
            .FirstOrDefaultAsync(r => r.RezervasyonId == id);

        if (rezervasyon == null)
            return NotFound(new { Mesaj = "Silinmek istenen rezervasyon kaydı bulunamadı." });

        // ✅ ONAYLANDI veya TAMAMLANDI rezervasyon silinemez
        if (rezervasyon.Durum == "ONAYLANDI" || rezervasyon.Durum == "TAMAMLANDI")
        {
            return BadRequest(new
            {
                Mesaj = $"'{rezervasyon.Durum}' durumundaki bir rezervasyon silinemez. Önce iptal edin."
            });
        }

        // ✅ Eğer masa rezerveyse ve başka rezervasyon yoksa masayı boşalt
        if (rezervasyon.Masa != null && rezervasyon.Masa.MasaDurumu == "REZERVE")
        {
            var baskaRezervasyonVar = await _context.Rezervasyons.AnyAsync(r =>
                r.MasaId == rezervasyon.MasaId &&
                r.RezervasyonId != id &&
                r.Durum != "IPTAL" &&
                r.Durum != "REDDEDILDI" &&
                r.Durum != "TAMAMLANDI"
            );

            if (!baskaRezervasyonVar)
            {
                rezervasyon.Masa.MasaDurumu = "BOŞ";
            }
        }

        _context.Rezervasyons.Remove(rezervasyon);
        await _context.SaveChangesAsync();

        return Ok(new { Mesaj = "Rezervasyon sistemden başarıyla kaldırıldı.", RezervasyonId = id });
    }
}