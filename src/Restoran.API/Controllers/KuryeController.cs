using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.API.Dtos;
using Restoran.Data;
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

        // 1. Tüm Aktif Kuryeleri Listeleme
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

        // 2. Kuryenin üzerindeki aktif siparişler
        [HttpGet("{personelId}/aktif-siparisler")]
        public async Task<IActionResult> GetAktifSiparisler(int personelId)
        {
            var siparisler = await _context.Siparislers
                .Where(s => s.PersonelId == personelId && s.SiparisDurumu != "Teslim Edildi" && s.SiparisDurumu != "İptal")
                .Select(s => new KuryeSiparisDto
                {
                    SiparisId = s.SiparisId,
                    SiparisDurumu = s.SiparisDurumu,
                    ToplamTutar = s.ToplamTutar,
                    SiparisTarihi = s.SiparisTarihi,
                    MusteriAdSoyad = s.Uye != null ? (s.Uye.UyeAdi + " " + s.Uye.UyeSoyadi) : "Müşteri Bilgisi Yok",
                    MusteriTelefon = s.Uye != null ? s.Uye.UyeTelefon : "",
                    AcikAdres = s.Uye != null && s.Uye.Adres.Any()
                        ? s.Uye.Adres.FirstOrDefault().AcikAdres
                        : "Adres Bilgisi Yok"
                })
                .ToListAsync();

            return Ok(siparisler);
        }

        // 3. Havuzdaki online siparişler (Tümünü getirebilir veya ?siparisId=5 şeklinde filtrelenebilir)
        [HttpGet("havuzdaki-siparisler")]
        public async Task<IActionResult> GetHavuzdakiSiparisler([FromQuery] int? siparisId = null)
        {
            // Sadece kurye atanmamış, masa siparişi olmayan (MasaId == null) ve aktif online siparişler
            var query = _context.Siparislers
                .Where(s => s.PersonelId == null
                         && s.MasaId == null
                         && s.SiparisDurumu != "Teslim Edildi"
                         && s.SiparisDurumu != "İptal");

            if (siparisId.HasValue)
            {
                query = query.Where(s => s.SiparisId == siparisId.Value);
            }

            var siparisler = await query
                .Select(s => new KuryeSiparisDto
                {
                    SiparisId = s.SiparisId,
                    SiparisDurumu = s.SiparisDurumu,
                    ToplamTutar = s.ToplamTutar,
                    SiparisTarihi = s.SiparisTarihi,
                    MusteriAdSoyad = s.Uye != null ? (s.Uye.UyeAdi + " " + s.Uye.UyeSoyadi) : "Müşteri Bilgisi Yok",
                    MusteriTelefon = s.Uye != null ? s.Uye.UyeTelefon : "",
                    AcikAdres = s.Uye != null && s.Uye.Adres.Any()
                        ? s.Uye.Adres.FirstOrDefault().AcikAdres
                        : "Adres Bilgisi Yok"
                })
                .ToListAsync();

            return Ok(siparisler);
        }

        // 4. Tek bir online siparişi ID'ye göre getirme
        [HttpGet("online-siparis/{siparisId}")]
        public async Task<IActionResult> GetOnlineSiparisById(int siparisId)
        {
            var siparis = await _context.Siparislers
                .Where(s => s.SiparisId == siparisId && s.MasaId == null)
                .Select(s => new KuryeSiparisDto
                {
                    SiparisId = s.SiparisId,
                    SiparisDurumu = s.SiparisDurumu,
                    ToplamTutar = s.ToplamTutar,
                    SiparisTarihi = s.SiparisTarihi,
                    MusteriAdSoyad = s.Uye != null ? (s.Uye.UyeAdi + " " + s.Uye.UyeSoyadi) : "Müşteri Bilgisi Yok",
                    MusteriTelefon = s.Uye != null ? s.Uye.UyeTelefon : "",
                    AcikAdres = s.Uye != null && s.Uye.Adres.Any()
                        ? s.Uye.Adres.FirstOrDefault().AcikAdres
                        : "Adres Bilgisi Yok"
                })
                .FirstOrDefaultAsync();

            if (siparis == null)
                return NotFound("Online sipariş bulunamadı.");

            return Ok(siparis);
        }

        // 5. Siparişi kuryenin üzerine alması
        [HttpPost("siparis-kabul-et")]
        public async Task<IActionResult> SiparisKabulEt([FromBody] KuryeAtaDto dto)
        {
            var siparis = await _context.Siparislers.FindAsync(dto.SiparisId);
            if (siparis == null) return NotFound("Sipariş bulunamadı.");

            if (siparis.PersonelId != null && siparis.PersonelId != dto.PersonelId)
            {
                return BadRequest("Bu sipariş başka bir kurye tarafından zaten alındı.");
            }

            siparis.PersonelId = dto.PersonelId;
            siparis.SiparisDurumu = "Yolda";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Sipariş üzerinize alındı ve durum güncellendi." });
        }

        // 6. Güvenli teslimat onaylama
        // 6. Güvenli teslimat onaylama (URL'den siparisId alır)
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

            siparis.SiparisDurumu = "Teslim Edildi";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Sipariş başarıyla teslim edildi." });
        }
    }
}