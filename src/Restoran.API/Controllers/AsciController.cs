using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.Data;
using Restoran.Data.Entities;
using Restoran.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Restoran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AsciController : ControllerBase
{
    private readonly DbRestoranContext _context;
    private readonly IHubContext<SiparisHub> _hubContext;

    public AsciController(DbRestoranContext context, IHubContext<SiparisHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // ============================================================
    // GET /api/Asci/siparisler - Aşçı paneli siparişleri
    // ============================================================
    [HttpGet("siparisler")]
    public async Task<IActionResult> GetAsciSiparisleri()
    {
        var siparisler = await _context.Siparislers
            .Where(s => s.SiparisDurumu == "BEKLEMEDE" ||
                        s.SiparisDurumu == "HAZIRLANIYOR" ||
                        s.SiparisDurumu == "HAZIR")
            .OrderBy(s => s.SiparisTarihi)
            .Select(s => new
            {
                s.SiparisId,
                s.SiparisDurumu,
                s.SiparisTarihi,
                s.ToplamTutar,
                MasaNo = s.Masa != null ? s.Masa.MasaNo : null,
                UyeAdi = s.Uye != null ? s.Uye.UyeAdi + " " + s.Uye.UyeSoyadi : "Ziyaretçi",
                DetaySayisi = s.SiparisDetays.Count,
                PersonelAdi = s.Personel != null ? s.Personel.PersonelAdi + " " + s.Personel.PersonelSoyadi : null,
                SiparisTipi = s.SiparisTipi,
                Detaylar = s.SiparisDetays.Select(d => new
                {
                    d.UrunId,
                    UrunAdi = d.Urun != null ? d.Urun.UrunAdi : "Ürün",
                    d.Adet,
                    d.DetayNot,
                    d.BirimFiyat
                }).ToList()
            })
            .ToListAsync();

        return Ok(siparisler);
    }

    // ============================================================
    // PUT /api/Asci/siparis/{id}/durum - Sipariş durumu güncelle
    // ============================================================
    [HttpPut("siparis/{id}/durum")]
    public async Task<IActionResult> UpdateSiparisDurum(int id, [FromBody] string durum)
    {
        var siparis = await _context.Siparislers.FindAsync(id);
        if (siparis == null)
            return NotFound(new { success = false, message = "Sipariş bulunamadı." });

        var gecerliDurumlar = new[] { "BEKLEMEDE", "HAZIRLANIYOR", "HAZIR" };
        var yeniDurum = durum?.ToUpper()?.Trim();

        if (string.IsNullOrEmpty(yeniDurum) || !gecerliDurumlar.Contains(yeniDurum))
            return BadRequest(new { success = false, message = "Geçersiz durum." });

        var eskiDurum = siparis.SiparisDurumu;
        siparis.SiparisDurumu = yeniDurum;
        await _context.SaveChangesAsync();

        // SignalR bildirimi
        try
        {
            await _hubContext.Clients.All.SendAsync("SiparisDurumGuncellendi", new
            {
                siparisId = id,
                mesaj = $"Sipariş #{id} durumu: {eskiDurum} → {yeniDurum}"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR bildirimi gönderilemedi: {ex.Message}");
        }

        return Ok(new
        {
            success = true,
            message = $"Sipariş #{id} durumu '{yeniDurum}' olarak güncellendi.",
            siparisId = id,
            yeniDurum = yeniDurum
        });
    }

    // ============================================================
    // 🔥🔥🔥 SİPARİŞ HAZIR VE KURYE ATA - DÜZENLENDİ 🔥🔥🔥
    // ============================================================
    [HttpPost("siparis/{id}/hazir-ve-kurye-ata")]
    public async Task<IActionResult> SiparisHazirVeKuryeAta(int id)
    {
        Console.WriteLine($"📦 Sipariş #{id} hazır ve kurye atanıyor...");

        var siparis = await _context.Siparislers
            .Include(s => s.SiparisDetays)
                .ThenInclude(sd => sd.Urun)
            .Include(s => s.Uye)
                .ThenInclude(u => u.Adres)
            .Include(s => s.Masa)
            .FirstOrDefaultAsync(s => s.SiparisId == id);

        if (siparis == null)
            return NotFound(new { success = false, message = "Sipariş bulunamadı." });

        Console.WriteLine($"📦 Sipariş #{id} - Tip: {siparis.SiparisTipi}, Durum: {siparis.SiparisDurumu}");

        // 🔥🔥🔥 PAKET SERVİS / ONLINE / GEL-AL SİPARİŞLERİ İÇİN KURYE ATA
        // PAKET SERVİS = ONLINE olarak kabul ediliyor
        if (siparis.SiparisTipi == "ONLINE" ||
            siparis.SiparisTipi == "PAKET_SERVIS" ||
            siparis.SiparisTipi == "PAKET" ||
            siparis.SiparisTipi == "GEL_AL")
        {
            Console.WriteLine($"🔍 Teslimat siparişi, kurye aranıyor...");

            // ✅ Müsait kurye bul
            var kurye = await _context.Personels
                .Include(p => p.Rol)
                .Where(p => p.IsActive == true &&
                           p.Rol != null &&
                           p.Rol.RolAdi.ToUpper() == "KURYE")
                .FirstOrDefaultAsync();

            Console.WriteLine($"🔍 Bulunan kurye: {(kurye != null ? kurye.PersonelAdi + " " + kurye.PersonelSoyadi : "Kurye yok!")}");

            if (kurye != null)
            {
                // Kurye ata ve durumu KURYEDE yap
                siparis.PersonelId = kurye.PersonelId;
                siparis.SiparisDurumu = "KURYEDE";
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Sipariş #{id} - Kurye #{kurye.PersonelId} atandı, Durum: KURYEDE");

                // 📍 Adres bilgisini al
                string adresBilgisi = "Adres bilinmiyor";
                if (siparis.Uye != null && siparis.Uye.Adres != null && siparis.Uye.Adres.Any())
                {
                    var teslimatAdresi = siparis.Uye.Adres.FirstOrDefault(a => a.TeslimatBolgesindeMi == true);
                    if (teslimatAdresi != null && !string.IsNullOrEmpty(teslimatAdresi.AcikAdres))
                        adresBilgisi = teslimatAdresi.AcikAdres;
                    else
                    {
                        var ilkAdres = siparis.Uye.Adres.FirstOrDefault();
                        if (ilkAdres != null && !string.IsNullOrEmpty(ilkAdres.AcikAdres))
                            adresBilgisi = ilkAdres.AcikAdres;
                    }
                }

                // 📣 SignalR ile kuryeye bildirim gönder
                try
                {
                    await _hubContext.Clients.Group("Kuryeler").SendAsync("SiparisHazirKurye", new
                    {
                        siparisId = id,
                        adres = adresBilgisi,
                        mesaj = $"Yeni teslimat siparişi #{id} hazır! Adres: {adresBilgisi}"
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SignalR bildirimi gönderilemedi: {ex.Message}");
                }

                return Ok(new
                {
                    success = true,
                    message = $"Sipariş #{id} hazır ve kurye {kurye.PersonelAdi} {kurye.PersonelSoyadi} atandı.",
                    siparisId = id,
                    kuryeId = kurye.PersonelId,
                    kuryeAdi = kurye.PersonelAdi + " " + kurye.PersonelSoyadi
                });
            }

            // Kurye yoksa HAZIR olarak kal
            siparis.SiparisDurumu = "HAZIR";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"Sipariş #{id} hazır ancak uygun kurye bulunamadı. Sipariş kurye havuzunda bekliyor.",
                siparisId = id,
                kuryeBulunamadi = true
            });
        }

        // ============================================================
        // SALON SİPARİŞİ (Masa siparişi) - Kurye atanmaz, garsona bildirim gider
        // ============================================================
        siparis.SiparisDurumu = "HAZIR";
        await _context.SaveChangesAsync();

        // 📣 Garsonlara bildirim gönder
        try
        {
            await _hubContext.Clients.Group("Garsonlar").SendAsync("SiparisHazir", new
            {
                siparisId = id,
                masaNo = siparis.Masa?.MasaNo ?? "Paket Servis",
                mesaj = $"Sipariş #{id} - Masa {siparis.Masa?.MasaNo ?? "Paket Servis"} hazır!"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR bildirimi gönderilemedi: {ex.Message}");
        }

        return Ok(new
        {
            success = true,
            message = $"Sipariş #{id} hazır!",
            siparisId = id,
            salonSiparisi = true
        });
    }

    // ============================================================
    // POST /api/Asci/siparis/{id}/garson-bildirim - Garsona bildirim
    // ============================================================
    [HttpPost("siparis/{id}/garson-bildirim")]
    public async Task<IActionResult> GarsonaBildirimGonder(int id)
    {
        var siparis = await _context.Siparislers
            .Include(s => s.Masa)
            .FirstOrDefaultAsync(s => s.SiparisId == id);

        if (siparis == null)
            return NotFound(new { success = false, message = "Sipariş bulunamadı." });

        if (siparis.SiparisDurumu != "HAZIR")
            return BadRequest(new { success = false, message = "Sipariş henüz hazır değil!" });

        try
        {
            await _hubContext.Clients.Group("Garsonlar").SendAsync("SiparisHazir", new
            {
                siparisId = id,
                masaNo = siparis.Masa?.MasaNo ?? "Paket Servis",
                mesaj = $"Sipariş #{id} - Masa {siparis.Masa?.MasaNo ?? "Paket Servis"} hazır!"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR bildirimi gönderilemedi: {ex.Message}");
        }

        return Ok(new
        {
            success = true,
            message = $"Garsona bildirim gönderildi: Sipariş #{id} hazır.",
            siparisId = id,
            masaNo = siparis.Masa?.MasaNo
        });
    }

    // ============================================================
    // PUT /api/Asci/siparis/{id}/tamamla - Sipariş tamamla
    // ============================================================
    [HttpPut("siparis/{id}/tamamla")]
    public async Task<IActionResult> SiparisTamamla(int id)
    {
        var siparis = await _context.Siparislers
            .Include(s => s.SiparisDetays)
            .ThenInclude(sd => sd.Urun)
            .ThenInclude(u => u.UrunRecetesis)
            .ThenInclude(ur => ur.Malzeme)
            .FirstOrDefaultAsync(s => s.SiparisId == id);

        if (siparis == null)
            return NotFound(new { success = false, message = "Sipariş bulunamadı." });

        if (siparis.SiparisDurumu == "TAMAMLANDI")
            return BadRequest(new { success = false, message = "Sipariş zaten tamamlanmış." });

        if (siparis.SiparisDurumu == "IPTAL")
            return BadRequest(new { success = false, message = "İptal edilen sipariş tamamlanamaz." });

        // Stok kontrolü ve düşüşü
        var stokHataMesajlari = new List<string>();
        var stokHareketleri = new List<StokHareket>();
        int personelId = siparis.PersonelId ?? 1;

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
                    stokHataMesajlari.Add($"{malzeme.MalzemeAdi} yetersiz! Gerekli: {kullanilacakMiktar} {malzeme.Birim}, Mevcut: {malzeme.StokMiktari} {malzeme.Birim}");
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
                success = false,
                message = "Stok yetersiz! Sipariş tamamlanamadı.",
                errors = stokHataMesajlari
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

        try
        {
            await _hubContext.Clients.All.SendAsync("SiparisDurumGuncellendi", new
            {
                siparisId = id,
                mesaj = $"Sipariş #{id} tamamlandı!"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR bildirimi gönderilemedi: {ex.Message}");
        }

        return Ok(new
        {
            success = true,
            message = $"Sipariş #{id} başarıyla tamamlandı!",
            siparisId = id,
            stokHareketSayisi = stokHareketleri.Count
        });
    }
}