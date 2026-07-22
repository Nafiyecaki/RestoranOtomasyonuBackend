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
    public class KuryeController : ControllerBase
    {
        private readonly DbRestoranContext _context;

        public KuryeController(DbRestoranContext context)
        {
            _context = context;
        }

        // ============================================================
        // 1. Tüm Aktif Kuryeleri Listeleme
        // ============================================================
        [HttpGet("kuryeler")]
        public async Task<IActionResult> GetKuryeler()
        {
            var kuryeler = await _context.Personels
                .Where(p => p.Rol != null && p.Rol.RolAdi.ToLower().Contains("kurye") && (p.IsActive == null || p.IsActive == true))
                .Select(p => new KuryeDto
                {
                    PersonelId = p.PersonelId,
                    AdSoyad = (p.PersonelAdi + " " + p.PersonelSoyadi).Trim(),
                    Telefon = p.PersonelTelefon,
                    IsActive = p.IsActive ?? true
                })
                .ToListAsync();

            return Ok(kuryeler);
        }

        // ============================================================
        // 2. Kuryenin üzerindeki aktif siparişler
        // ============================================================
        [HttpGet("{personelId}/aktif-siparisler")]
        public async Task<IActionResult> GetAktifSiparisler(int personelId)
        {
            Console.WriteLine($"📦 Kurye #{personelId} için aktif siparişler aranıyor...");

            var siparisler = await _context.Siparislers
                .Where(s => s.PersonelId == personelId &&
                           (s.SiparisDurumu == "KURYEDE" || s.SiparisDurumu == "YOLDA"))
                .OrderByDescending(s => s.SiparisTarihi)
                .Select(s => new KuryeSiparisDto
                {
                    SiparisId = s.SiparisId,
                    SiparisDurumu = s.SiparisDurumu,
                    ToplamTutar = s.ToplamTutar ?? 0,
                    SiparisTarihi = s.SiparisTarihi ?? DateTime.Now,
                    MusteriAdSoyad = s.Uye != null ? (s.Uye.UyeAdi + " " + s.Uye.UyeSoyadi) : "Müşteri Bilgisi Yok",
                    MusteriTelefon = s.Uye != null ? s.Uye.UyeTelefon : "",
                    AcikAdres = s.Uye != null && s.Uye.Adres != null && s.Uye.Adres.Any()
                        ? s.Uye.Adres.FirstOrDefault().AcikAdres
                        : "Adres Bilgisi Yok"
                })
                .ToListAsync();

            Console.WriteLine($"📦 Kurye #{personelId} için {siparisler.Count} aktif sipariş bulundu.");

            return Ok(siparisler);
        }

        // ============================================================
        // 3. Havuzdaki online siparişler (HAZIR durumundakiler)
        // ============================================================
        [HttpGet("havuzdaki-siparisler")]
        public async Task<IActionResult> GetHavuzdakiSiparisler([FromQuery] int? siparisId = null)
        {
            var query = _context.Siparislers
                .Where(s => s.PersonelId == null
                         && s.MasaId == null
                         && s.SiparisDurumu == "HAZIR"
                         && s.SiparisDurumu != "TESLIM EDILDI"
                         && s.SiparisDurumu != "IPTAL");

            if (siparisId.HasValue)
            {
                query = query.Where(s => s.SiparisId == siparisId.Value);
            }

            var siparisler = await query
                .Select(s => new KuryeSiparisDto
                {
                    SiparisId = s.SiparisId,
                    SiparisDurumu = s.SiparisDurumu,
                    ToplamTutar = s.ToplamTutar ?? 0,
                    SiparisTarihi = s.SiparisTarihi ?? DateTime.Now,
                    MusteriAdSoyad = s.Uye != null ? (s.Uye.UyeAdi + " " + s.Uye.UyeSoyadi) : "Müşteri Bilgisi Yok",
                    MusteriTelefon = s.Uye != null ? s.Uye.UyeTelefon : "",
                    AcikAdres = s.Uye != null && s.Uye.Adres != null && s.Uye.Adres.Any()
                        ? s.Uye.Adres.FirstOrDefault().AcikAdres
                        : "Adres Bilgisi Yok"
                })
                .ToListAsync();

            return Ok(siparisler);
        }

        // ============================================================
        // 4. Tek bir online siparişi ID'ye göre getirme
        // ============================================================
        [HttpGet("online-siparis/{siparisId}")]
        public async Task<IActionResult> GetOnlineSiparisById(int siparisId)
        {
            var siparis = await _context.Siparislers
                .Where(s => s.SiparisId == siparisId && s.MasaId == null)
                .Select(s => new KuryeSiparisDto
                {
                    SiparisId = s.SiparisId,
                    SiparisDurumu = s.SiparisDurumu,
                    ToplamTutar = s.ToplamTutar ?? 0,
                    SiparisTarihi = s.SiparisTarihi ?? DateTime.Now,
                    MusteriAdSoyad = s.Uye != null ? (s.Uye.UyeAdi + " " + s.Uye.UyeSoyadi) : "Müşteri Bilgisi Yok",
                    MusteriTelefon = s.Uye != null ? s.Uye.UyeTelefon : "",
                    AcikAdres = s.Uye != null && s.Uye.Adres != null && s.Uye.Adres.Any()
                        ? s.Uye.Adres.FirstOrDefault().AcikAdres
                        : "Adres Bilgisi Yok"
                })
                .FirstOrDefaultAsync();

            if (siparis == null)
                return NotFound("Online sipariş bulunamadı.");

            return Ok(siparis);
        }

        // ============================================================
        // 5. Siparişi kuryenin üzerine alması (Havuzdan kabul)
        // ============================================================
        [HttpPost("siparis-kabul-et")]
        public async Task<IActionResult> SiparisKabulEt([FromBody] KuryeAtaDto dto)
        {
            var siparis = await _context.Siparislers.FindAsync(dto.SiparisId);
            if (siparis == null) return NotFound("Sipariş bulunamadı.");

            if (siparis.SiparisDurumu != "HAZIR")
                return BadRequest($"Sipariş durumu '{siparis.SiparisDurumu}' olduğu için kabul edilemez. (HAZIR olmalı)");

            if (siparis.PersonelId != null && siparis.PersonelId != dto.PersonelId)
            {
                return BadRequest("Bu sipariş başka bir kurye tarafından zaten alındı.");
            }

            var aktifSiparisSayisi = await _context.Siparislers
                .CountAsync(s => s.PersonelId == dto.PersonelId &&
                                (s.SiparisDurumu == "KURYEDE" || s.SiparisDurumu == "YOLDA"));

            if (aktifSiparisSayisi >= 3)
                return BadRequest($"Kurye zaten {aktifSiparisSayisi} aktif siparişe sahip. (Max 3)");

            siparis.PersonelId = dto.PersonelId;
            siparis.SiparisDurumu = "KURYEDE";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Sipariş üzerinize alındı ve durum güncellendi.",
                siparisId = siparis.SiparisId,
                durum = siparis.SiparisDurumu
            });
        }

        // ============================================================
        // 6. Güvenli teslimat onaylama
        // ============================================================
        [HttpPut("teslim-et/{siparisId}")]
        public async Task<IActionResult> TeslimEt(int siparisId, [FromBody] KuryeAtaDto dto)
        {
            var siparis = await _context.Siparislers.FindAsync(siparisId);
            if (siparis == null)
                return NotFound("Sipariş bulunamadı.");

            if (siparis.PersonelId != dto.PersonelId)
            {
                return BadRequest("Bu sipariş sizin üzerinizde tanımlı değil.");
            }

            if (siparis.SiparisDurumu != "KURYEDE" && siparis.SiparisDurumu != "YOLDA")
                return BadRequest($"Sipariş durumu '{siparis.SiparisDurumu}' olduğu için teslim edilemez.");

            siparis.SiparisDurumu = "TESLIM EDILDI";
            siparis.SiparisTarihi = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Sipariş başarıyla teslim edildi.",
                siparisId = siparis.SiparisId,
                durum = siparis.SiparisDurumu
            });
        }

        // ============================================================
        // 7. Müsait kuryeleri getir
        // ============================================================
        [HttpGet("musait-kuryeler")]
        public async Task<IActionResult> GetMusaitKuryeler()
        {
            var kuryeler = await _context.Personels
                .Where(p => p.Rol != null && p.Rol.RolAdi.ToLower().Contains("kurye") && (p.IsActive == null || p.IsActive == true))
                .Select(p => new
                {
                    p.PersonelId,
                    p.PersonelAdi,
                    p.PersonelSoyadi,
                    p.KullaniciAdi,
                    p.PersonelTelefon,
                    AktifSiparisSayisi = _context.Siparislers
                        .Count(s => s.PersonelId == p.PersonelId &&
                                   (s.SiparisDurumu == "KURYEDE" || s.SiparisDurumu == "YOLDA"))
                })
                .ToListAsync();

            var musaitKuryeler = kuryeler
                .Where(k => k.AktifSiparisSayisi < 3)
                .OrderBy(k => k.AktifSiparisSayisi)
                .Select(k => new
                {
                    k.PersonelId,
                    k.PersonelAdi,
                    k.PersonelSoyadi,
                    k.KullaniciAdi,
                    k.PersonelTelefon,
                    k.AktifSiparisSayisi,
                    MusaitMi = k.AktifSiparisSayisi < 3
                })
                .ToList();

            return Ok(musaitKuryeler);
        }

        // ============================================================
        // 8. Siparişi kuryeye ata
        // ============================================================
        [HttpPost("siparis-ata/{siparisId}")]
        public async Task<IActionResult> SiparisKuryeyeAta(int siparisId, [FromBody] KuryeAtaDto dto)
        {
            try
            {
                Console.WriteLine($"📡 Sipariş #{siparisId} kurye #{dto.PersonelId}'a atanıyor...");

                if (dto == null || dto.PersonelId <= 0)
                    return BadRequest("Geçersiz kurye ID.");

                var siparis = await _context.Siparislers
                    .Include(s => s.Uye)
                    .FirstOrDefaultAsync(s => s.SiparisId == siparisId);

                if (siparis == null)
                    return NotFound($"Sipariş #{siparisId} bulunamadı.");

                if (siparis.SiparisDurumu != "HAZIR")
                    return BadRequest($"Sipariş #{siparisId} durumu '{siparis.SiparisDurumu}' olduğu için kuryeye atanamaz. (HAZIR olmalı)");

                var kurye = await _context.Personels
                    .FirstOrDefaultAsync(p => p.PersonelId == dto.PersonelId &&
                                             p.Rol != null &&
                                             p.Rol.RolAdi.ToLower().Contains("kurye") &&
                                             (p.IsActive == null || p.IsActive == true));

                if (kurye == null)
                    return NotFound($"Kurye ID {dto.PersonelId} bulunamadı veya aktif değil.");

                var aktifSiparisSayisi = await _context.Siparislers
                    .CountAsync(s => s.PersonelId == dto.PersonelId &&
                                    (s.SiparisDurumu == "KURYEDE" || s.SiparisDurumu == "YOLDA"));

                if (aktifSiparisSayisi >= 3)
                    return BadRequest($"Kurye {kurye.PersonelAdi} {kurye.PersonelSoyadi} zaten {aktifSiparisSayisi} aktif siparişe sahip. (Max 3)");

                siparis.PersonelId = dto.PersonelId;
                siparis.SiparisDurumu = "KURYEDE";

                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Sipariş #{siparisId} kurye #{dto.PersonelId}'a atandı. Yeni durum: KURYEDE");

                return Ok(new
                {
                    Mesaj = $"Sipariş #{siparisId} kurye {kurye.PersonelAdi} {kurye.PersonelSoyadi}'a atandı.",
                    SiparisId = siparisId,
                    KuryeId = dto.PersonelId,
                    KuryeAdi = kurye.PersonelAdi + " " + kurye.PersonelSoyadi,
                    SiparisDurumu = siparis.SiparisDurumu
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Hata: {ex.Message}");
                return StatusCode(500, $"Sunucu hatası: {ex.Message}");
            }
        }

        // ============================================================
        // 9. Kurye teslim geçmişi
        // ============================================================
        [HttpGet("{personelId}/gecmis")]
        public async Task<IActionResult> GetTeslimGecmisi(int personelId)
        {
            var gecmis = await _context.Siparislers
                .Where(s => s.PersonelId == personelId && s.SiparisDurumu == "TESLIM EDILDI")
                .OrderByDescending(s => s.SiparisTarihi)
                .Select(s => new
                {
                    s.SiparisId,
                    SiparisTarihi = s.SiparisTarihi ?? DateTime.Now,
                    ToplamTutar = s.ToplamTutar ?? 0,
                    MusteriAdSoyad = s.Uye != null ? (s.Uye.UyeAdi + " " + s.Uye.UyeSoyadi) : "Ziyaretçi",
                    MasaNo = s.Masa != null ? s.Masa.MasaNo : null,
                    s.SiparisDurumu
                })
                .ToListAsync();

            return Ok(gecmis);
        }

        // ============================================================
        // 10. Siparişi iptal et
        // ============================================================
        [HttpPut("siparis-iptal/{siparisId}")]
        public async Task<IActionResult> SiparisIptal(int siparisId, [FromBody] KuryeAtaDto dto)
        {
            var siparis = await _context.Siparislers.FindAsync(siparisId);
            if (siparis == null)
                return NotFound("Sipariş bulunamadı.");

            if (siparis.PersonelId != dto.PersonelId)
                return BadRequest("Bu sipariş sizin üzerinizde tanımlı değil.");

            if (siparis.SiparisDurumu == "TESLIM EDILDI" || siparis.SiparisDurumu == "TAMAMLANDI")
                return BadRequest("Teslim edilmiş sipariş iptal edilemez.");

            siparis.SiparisDurumu = "IPTAL";
            siparis.PersonelId = null;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Sipariş başarıyla iptal edildi.",
                siparisId = siparis.SiparisId,
                durum = siparis.SiparisDurumu
            });
        }

        // ============================================================
        // 🆕 11. Günlük Toplam Taşınan Tutar
        // ============================================================
        [HttpGet("{personelId}/gunluk-tutar")]
        public async Task<IActionResult> GetGunlukTutar(int personelId, [FromQuery] DateTime? tarih = null)
        {
            var hedefTarih = tarih ?? DateTime.Today;

            var toplamTutar = await _context.Siparislers
                .Where(s => s.PersonelId == personelId &&
                           s.SiparisDurumu == "TESLIM EDILDI" &&
                           s.SiparisTarihi.HasValue &&
                           s.SiparisTarihi.Value.Date == hedefTarih.Date)
                .SumAsync(s => s.ToplamTutar ?? 0);

            var siparisSayisi = await _context.Siparislers
                .CountAsync(s => s.PersonelId == personelId &&
                                s.SiparisDurumu == "TESLIM EDILDI" &&
                                s.SiparisTarihi.HasValue &&
                                s.SiparisTarihi.Value.Date == hedefTarih.Date);

            return Ok(new
            {
                Tarih = hedefTarih.ToString("yyyy-MM-dd"),
                ToplamTutar = toplamTutar,
                SiparisSayisi = siparisSayisi
            });
        }

        // ============================================================
        // 🆕 12. Aylık Toplam Taşınan Tutar (Gün Gün Detaylı)
        // ============================================================
        [HttpGet("{personelId}/aylik-tutar")]
        public async Task<IActionResult> GetAylikTutar(int personelId, [FromQuery] int? yil = null, [FromQuery] int? ay = null)
        {
            var hedefYil = yil ?? DateTime.Now.Year;
            var hedefAy = ay ?? DateTime.Now.Month;

            var toplamTutar = await _context.Siparislers
                .Where(s => s.PersonelId == personelId &&
                           s.SiparisDurumu == "TESLIM EDILDI" &&
                           s.SiparisTarihi.HasValue &&
                           s.SiparisTarihi.Value.Year == hedefYil &&
                           s.SiparisTarihi.Value.Month == hedefAy)
                .SumAsync(s => s.ToplamTutar ?? 0);

            var siparisSayisi = await _context.Siparislers
                .CountAsync(s => s.PersonelId == personelId &&
                                s.SiparisDurumu == "TESLIM EDILDI" &&
                                s.SiparisTarihi.HasValue &&
                                s.SiparisTarihi.Value.Year == hedefYil &&
                                s.SiparisTarihi.Value.Month == hedefAy);

            // Gün gün detay
            var gunlukDetay = await _context.Siparislers
                .Where(s => s.PersonelId == personelId &&
                           s.SiparisDurumu == "TESLIM EDILDI" &&
                           s.SiparisTarihi.HasValue &&
                           s.SiparisTarihi.Value.Year == hedefYil &&
                           s.SiparisTarihi.Value.Month == hedefAy)
                .GroupBy(s => s.SiparisTarihi.Value.Date)
                .Select(g => new
                {
                    Tarih = g.Key.ToString("yyyy-MM-dd"),
                    GunlukTutar = g.Sum(s => s.ToplamTutar ?? 0),
                    GunlukSiparis = g.Count()
                })
                .OrderBy(g => g.Tarih)
                .ToListAsync();

            return Ok(new
            {
                Yil = hedefYil,
                Ay = hedefAy,
                ToplamTutar = toplamTutar,
                ToplamSiparis = siparisSayisi,
                GunlukDetay = gunlukDetay
            });
        }

        // ============================================================
        // 🆕 13. Kapıda Ödeme Alındı Bildirimi
        // ============================================================
        [HttpPost("{siparisId}/kapida-odeme")]
        public async Task<IActionResult> KapidaOdemeAlindi(int siparisId, [FromBody] KapidaOdemeDto dto)
        {
            var siparis = await _context.Siparislers
                .Include(s => s.Uye)
                .FirstOrDefaultAsync(s => s.SiparisId == siparisId);

            if (siparis == null)
                return NotFound("Sipariş bulunamadı.");

            if (siparis.PersonelId != dto.KuryeId)
                return BadRequest("Bu sipariş size ait değil.");

            if (siparis.SiparisDurumu != "KURYEDE" && siparis.SiparisDurumu != "YOLDA")
                return BadRequest($"Sipariş durumu '{siparis.SiparisDurumu}' olduğu için ödeme alınamaz.");

            siparis.SiparisDurumu = "ODENDI";
            siparis.SiparisTarihi = DateTime.Now;

            // Ödeme kaydı oluştur
            var odeme = new Odeme
            {
                SiparisId = siparisId,
                OdemeTipi = dto.OdemeTipi ?? "NAKIT",
                OdemeTutari = siparis.ToplamTutar ?? 0,
                OdemeTarihi = DateTime.Now,
                PersonelId = dto.KuryeId
            };

            _context.Odemes.Add(odeme);
            await _context.SaveChangesAsync();

            // Bildirim oluştur
            try
            {
                var bildirim = new Bildirim
                {
                    KullaniciId = siparis.UyeId,
                    Baslik = "Kapıda Ödeme Alındı",
                    Mesaj = $"Sipariş #{siparisId} için kapıda ödeme başarıyla alındı. Tutar: ₺{siparis.ToplamTutar}",
                    OlusturmaTarihi = DateTime.Now,
                    OkunduMu = false,
                    Tip = "ODEME"
                };

                _context.Bildirims.Add(bildirim);
                await _context.SaveChangesAsync();
            }
            catch
            {
                Console.WriteLine("Bildirim tablosu bulunamadı, bildirim gönderilemedi.");
            }

            return Ok(new
            {
                Mesaj = "Kapıda ödeme başarıyla alındı.",
                SiparisId = siparisId,
                OdemeTutari = siparis.ToplamTutar
            });
        }

        // ============================================================
        // 🆕 14. Vardiya Kontrolü - Kurye aktif mi?
        // ============================================================
        [HttpGet("{personelId}/vardiya-kontrol")]
        public async Task<IActionResult> VardiyaKontrol(int personelId)
        {
            var personel = await _context.Personels.FindAsync(personelId);
            if (personel == null)
                return NotFound("Personel bulunamadı.");

            var now = DateTime.Now;
            var saat = now.ToString("HH:mm");
            var gun = ((int)now.DayOfWeek == 0) ? 7 : (int)now.DayOfWeek;

            var calismaGunleri = personel.CalismaGunleri?.Split(',').Select(int.Parse).ToList() ?? new List<int>();
            var vardiyaBaslangic = personel.VardiyaBaslangic ?? "00:00";
            var vardiyaBitis = personel.VardiyaBitis ?? "23:59";

            var gunKontrol = calismaGunleri.Contains(gun);
            var saatKontrol = string.Compare(saat, vardiyaBaslangic) >= 0 && string.Compare(saat, vardiyaBitis) <= 0;
            var vardiyaAktif = personel.VardiyaAktifMi ?? true;

            return Ok(new
            {
                PersonelId = personelId,
                AktifMi = gunKontrol && saatKontrol && vardiyaAktif,
                GunKontrol = gunKontrol,
                SaatKontrol = saatKontrol,
                VardiyaAktif = vardiyaAktif,
                CalismaGunleri = calismaGunleri,
                VardiyaBaslangic = vardiyaBaslangic,
                VardiyaBitis = vardiyaBitis,
                SuankiSaat = saat,
                SuankiGun = gun
            });
        }

        // ============================================================
        // 🆕 15. Sipariş Teklifi Al
        // ============================================================
        [HttpGet("siparis-teklif/{siparisId}")]
        public async Task<IActionResult> GetSiparisTeklif(int siparisId)
        {
            var siparis = await _context.Siparislers
                .Include(s => s.Uye)
                .Include(s => s.SiparisDetays)
                .ThenInclude(d => d.Urun)
                .FirstOrDefaultAsync(s => s.SiparisId == siparisId);

            if (siparis == null)
                return NotFound("Sipariş bulunamadı.");

            if (siparis.SiparisDurumu != "HAZIR")
                return BadRequest($"Sipariş durumu '{siparis.SiparisDurumu}' olduğu için teklif alınamaz.");

            var urunler = siparis.SiparisDetays.Select(d => new
            {
                d.UrunId,
                UrunAdi = d.Urun != null ? d.Urun.UrunAdi : "Ürün",
                d.Adet,
                d.BirimFiyat,
                Toplam = d.Adet * d.BirimFiyat
            }).ToList();

            var tahminiMesafe = new Random().Next(1, 15);

            return Ok(new
            {
                SiparisId = siparisId,
                Musteri = siparis.Uye != null ? siparis.Uye.UyeAdi + " " + siparis.Uye.UyeSoyadi : "Misafir",
                Adres = siparis.Uye != null && siparis.Uye.Adres != null && siparis.Uye.Adres.Any()
                    ? siparis.Uye.Adres.FirstOrDefault().AcikAdres
                    : "Adres bilgisi yok",
                ToplamTutar = siparis.ToplamTutar ?? 0,
                Urunler = urunler,
                TahminiMesafe = tahminiMesafe,
                TahminiTeslimSuresi = tahminiMesafe * 2 + 5,
                SiparisDurumu = siparis.SiparisDurumu
            });
        }

        // ============================================================
        // 🆕 16. Sipariş Teklifini Kabul Et
        // ============================================================
        [HttpPost("siparis-teklif-kabul")]
        public async Task<IActionResult> SiparisTeklifKabul([FromBody] KuryeAtaDto dto)
        {
            var siparis = await _context.Siparislers.FindAsync(dto.SiparisId);
            if (siparis == null)
                return NotFound("Sipariş bulunamadı.");

            if (siparis.SiparisDurumu != "HAZIR")
                return BadRequest($"Sipariş durumu '{siparis.SiparisDurumu}' olduğu için kabul edilemez.");

            if (siparis.PersonelId != null)
                return BadRequest("Bu sipariş başka bir kurye tarafından zaten alındı.");

            var aktifSiparisSayisi = await _context.Siparislers
                .CountAsync(s => s.PersonelId == dto.PersonelId &&
                                (s.SiparisDurumu == "KURYEDE" || s.SiparisDurumu == "YOLDA"));

            if (aktifSiparisSayisi >= 3)
                return BadRequest($"Kurye zaten {aktifSiparisSayisi} aktif siparişe sahip. (Max 3)");

            siparis.PersonelId = dto.PersonelId;
            siparis.SiparisDurumu = "KURYEDE";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Mesaj = "Sipariş teklifi kabul edildi.",
                SiparisId = dto.SiparisId,
                KuryeId = dto.PersonelId,
                Durum = siparis.SiparisDurumu
            });
        }
    }

    // ============================================================
    // 📦 DTO'LAR
    // ============================================================

    public class KuryeDto
    {
        public int PersonelId { get; set; }
        public string AdSoyad { get; set; }
        public string Telefon { get; set; }
        public bool IsActive { get; set; }
    }

    public class KuryeSiparisDto
    {
        public int SiparisId { get; set; }
        public string SiparisDurumu { get; set; }
        public decimal ToplamTutar { get; set; }
        public DateTime SiparisTarihi { get; set; }
        public string MusteriAdSoyad { get; set; }
        public string MusteriTelefon { get; set; }
        public string AcikAdres { get; set; }
    }

    public class KuryeAtaDto
    {
        public int SiparisId { get; set; }
        public int PersonelId { get; set; }
    }

    public class KapidaOdemeDto
    {
        public int KuryeId { get; set; }
        public string OdemeTipi { get; set; } = "NAKIT";
    }
}