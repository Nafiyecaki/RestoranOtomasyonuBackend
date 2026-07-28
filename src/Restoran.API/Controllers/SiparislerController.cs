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
                    //  EKLENDI: İade edildi mi bilgisi
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
                    // ✅ EKLENDI: İade edildi mi bilgisi
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
        if (dto.Detaylar == null || !dto.Detaylar.Any())
            return BadRequest("Sipariş oluşturmak için en az bir ürün eklemelisiniz.");

        var stokHataMesajlari = new List<string>();
        var stokHataDetaylari = new List<object>();
        // 🔑 Stok kontrolünü geçen kalemleri burada topluyoruz ki kontrol bittikten
        // hemen sonra (sipariş garson tarafından alınır alınmaz) gerçekten düşebilelim.
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
                else
                {
                    dusulecekMalzemeler.Add((malzeme, gerekliMiktar, urun.UrunAdi, adet, urun.UrunId));
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
        //  STOK DÜŞÜMÜ: Garson siparişi aldığı anda (tamamlama beklemeden) düşülür
        // ============================================================
        int stokPersonelId = dto.PersonelId ?? 1;
        foreach (var kalem in dusulecekMalzemeler)
        {
            kalem.Malzeme.StokMiktari -= kalem.Miktar;

            _context.StokHarekets.Add(new StokHareket
            {
                UrunId = kalem.UrunId,
                PersonelId = stokPersonelId,
                StokIslemTipi = "CIKIS",
                StokMiktari = (int)kalem.Miktar,
                IsleminTarihSaati = DateTime.Now,
                IsleminAciklamasi = $"Sipariş alındı - {kalem.UrunAdi} ({kalem.Adet} adet)"
            });
        }

        // ============================================================
        //  ADIM 2: VAR OLAN AKTİF SİPARİŞE Mİ EKLENİYOR, YOKSA YENİ Mİ AÇILIYOR?
        // ============================================================
        Siparisler siparis = null;
        bool mevcutSiparisEklemesi = false;

        // 🔑 Frontend var olan siparişe ekleme yapmak istiyorsa SiparisId gönderir
        if (dto.SiparisId.HasValue && dto.SiparisId.Value > 0)
        {
            siparis = await _context.Siparislers
                .Include(s => s.SiparisDetays)
                .FirstOrDefaultAsync(s => s.SiparisId == dto.SiparisId.Value);

            if (siparis != null &&
                siparis.SiparisDurumu != "IPTAL" &&
                siparis.SiparisDurumu != "TAMAMLANDI" &&
                siparis.SiparisDurumu != "ODENDI")
            {
                mevcutSiparisEklemesi = true;
            }
        }

        // 🔑 GÜVENLİK KONTROLÜ: Frontend doğru SiparisId göndermemiş olsa bile,
        // aynı masada zaten aktif (kapanmamış) bir sipariş varsa YENİ sipariş açma,
        // var olana ekle. Bu, aynı masada birden fazla paralel "BEKLEMEDE" sipariş
        // oluşmasını ve "Masa zaten dolu!" diye siparişin tamamen reddedilmesini engeller.
        if (!mevcutSiparisEklemesi && dto.MasaId.HasValue)
        {
            var masadakiAktifSiparis = await _context.Siparislers
                .Include(s => s.SiparisDetays)
                .Where(s => s.MasaId == dto.MasaId.Value
                    && s.SiparisDurumu != "IPTAL"
                    && s.SiparisDurumu != "TAMAMLANDI"
                    && s.SiparisDurumu != "ODENDI")
                .OrderByDescending(s => s.SiparisTarihi)
                .FirstOrDefaultAsync();

            if (masadakiAktifSiparis != null)
            {
                siparis = masadakiAktifSiparis;
                mevcutSiparisEklemesi = true;
            }
        }

        // ============================================================
        //  ADIM 3: MASA KONTROLÜ VE SİPARİŞ OLUŞTUR (sadece gerçekten yeniyse)
        // ============================================================
        if (!mevcutSiparisEklemesi)
        {
            if (dto.MasaId.HasValue)
            {
                var masa = await _context.Masas.FindAsync(dto.MasaId.Value);
                if (masa == null)
                    return NotFound(new { Mesaj = $"ID'si {dto.MasaId} olan masa bulunamadı." });

                masa.MasaDurumu = "DOLU";
            }

            siparis = new Siparisler
            {
                SiparisTarihi = DateTime.Now,
                SiparisDurumu = "HAZIRLANIYOR",
                SiparisTipi = dto.SiparisTipi ?? "SALON",
                UyeId = dto.UyeId,
                MasaId = dto.MasaId,
                PersonelId = dto.PersonelId,
                ToplamTutar = 0,
                SiparisDetays = new List<SiparisDetay>()
            };

            await _context.Siparislers.AddAsync(siparis);
        }

        // ============================================================
        //  ADIM 4: ÜRÜNLERİ EKLE (hem yeni sipariş hem mevcut sipariş için ortak akış)
        // ============================================================
        foreach (var d in dto.Detaylar)
        {
            var urun = await _context.Urunlers.FindAsync(d.UrunId);
            if (urun == null)
                return NotFound(new { Mesaj = $"ID'si {d.UrunId} olan ürün sistemde bulunamadı." });

            int adet = d.Adet <= 0 ? 1 : d.Adet;
            string detayNot = d.DetayNot ?? "";

            // 🔑 Aynı üründen, aynı notla siparişte zaten bir satır varsa
            // (örn. çift tıklama sonucu aynı istek iki kez gitmiş olabilir),
            // yeni bir satır daha açmak yerine mevcut satırın miktarını artır.
            var mevcutDetay = siparis.SiparisDetays
                .FirstOrDefault(sd => sd.UrunId == d.UrunId && (sd.DetayNot ?? "") == detayNot);

            if (mevcutDetay != null)
            {
                mevcutDetay.Adet += adet;
            }
            else
            {
                siparis.SiparisDetays.Add(new SiparisDetay
                {
                    UrunId = d.UrunId,
                    Adet = adet,
                    BirimFiyat = urun.Fiyat,
                    DetayNot = detayNot
                });
            }
        }

        // Toplam tutarı tüm kalemlerden (eskiler + yeniler) yeniden hesapla
        siparis.ToplamTutar = siparis.SiparisDetays.Sum(x => x.Adet * x.BirimFiyat);

        // 🔑 Var olan siparişe yeni ürün eklendiyse ve sipariş zaten
        // HAZIR / TESLIM EDILDI gibi ileri bir aşamadaysa, yeni eklenen
        // ürünler henüz hazırlanmadığı için siparişi tekrar "HAZIRLANIYOR"
        // durumuna çekiyoruz ki mutfak akışında yeniden görünsün.
        if (mevcutSiparisEklemesi)
        {
            var ileriDurumlar = new[] { "HAZIR", "TESLIM EDILDI" };
            if (ileriDurumlar.Contains(siparis.SiparisDurumu))
            {
                siparis.SiparisDurumu = "HAZIRLANIYOR";
            }
        }

        // Mevcut siparişe ekleme yapıldıysa masa durumunu garanti altına al
        if (mevcutSiparisEklemesi && siparis.MasaId.HasValue)
        {
            var masa = await _context.Masas.FindAsync(siparis.MasaId.Value);
            if (masa != null) masa.MasaDurumu = "DOLU";
        }

        await _context.SaveChangesAsync();

        // Oluşturulan/güncellenen siparişi detaylarıyla birlikte geri döndür
        var createdOrder = await _context.Siparislers
            .Include(s => s.Uye)        // 🔑 UYE TABLOSUNU DAHİL ET
            .Include(s => s.Masa)       // 🔑 MASA TABLOSUNU DAHİL ET
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
                UyeId = s.UyeId,  // 🔑 ÜYE ID'Yİ DE GÖNDER
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
            Mesaj = mevcutSiparisEklemesi
                ? "Mevcut siparişe ürün(ler) başarıyla eklendi."
                : "Sipariş ve detayları başarıyla oluşturuldu ve stok kontrolü geçti.",
            SiparisId = siparis.SiparisId,
            HesaplananToplamTutar = siparis.ToplamTutar,
            Siparis = createdOrder
        });
    }

    // PUT /api/siparisler/5 -> Mevcut siparişi ve detaylarını günceller
    [HttpPut("{id}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] SiparisGuncelleDto dto)
    {
        if (dto == null) return BadRequest("Güncelleme verileri boş olamaz.");

        var siparis = await _context.Siparislers
            .Include(s => s.SiparisDetays)
            .Include(s => s.Uye)        // 🔑 UYE TABLOSUNU DAHİL ET
            .FirstOrDefaultAsync(s => s.SiparisId == id);

        if (siparis == null) return NotFound("Güncellenmek istenen sipariş bulunamadı.");

        if (siparis.SiparisDurumu == "TAMAMLANDI" || siparis.SiparisDurumu == "IPTAL" || siparis.SiparisDurumu == "ODENDI")
        {
            return BadRequest($"'{siparis.SiparisDurumu}' durumundaki bir sipariş güncellenemez.");
        }

        siparis.SiparisTipi = dto.SiparisTipi ?? siparis.SiparisTipi;
        if (dto.UyeId.HasValue) siparis.UyeId = dto.UyeId;
        if (dto.MasaId.HasValue) siparis.MasaId = dto.MasaId;
        if (dto.PersonelId.HasValue) siparis.PersonelId = dto.PersonelId;

        if (dto.Detaylar != null && dto.Detaylar.Any())
        {
            // 1. Eski detayları sil
            _context.SiparisDetays.RemoveRange(siparis.SiparisDetays);

            // 2. Yeni ürün kalemlerini oluştur ve tutarı hesapla
            siparis.ToplamTutar = 0;
            var yeniDetaylar = new List<SiparisDetay>();

            foreach (var d in dto.Detaylar)
            {
                var urun = await _context.Urunlers.FindAsync(d.UrunId);
                if (urun == null)
                    return NotFound($"ID'si {d.UrunId} olan ürün sistemde bulunamadı.");

                int adet = d.Adet <= 0 ? 1 : d.Adet;

                yeniDetaylar.Add(new SiparisDetay
                {
                    SiparisId = id,
                    UrunId = d.UrunId,
                    Adet = adet,
                    BirimFiyat = urun.Fiyat,
                    DetayNot = d.DetayNot
                });

                siparis.ToplamTutar += adet * urun.Fiyat;
            }

            // 3. Veritabanına yeni detayları ekle
            await _context.SiparisDetays.AddRangeAsync(yeniDetaylar);
        }

        await _context.SaveChangesAsync();

        // Güncellenmiş siparişi detaylarıyla birlikte geri döndür
        var updatedOrder = await _context.Siparislers
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
                    d.DetayNot
                })
            })
            .FirstOrDefaultAsync();

        return Ok(new
        {
            Mesaj = "Sipariş detayları ve toplam tutarı başarıyla güncellendi.",
            SiparisId = siparis.SiparisId,
            YeniToplamTutar = siparis.ToplamTutar,
            Siparis = updatedOrder
        });
    }

    // PUT /api/siparisler/{id}/tamamla -> Siparişi tamamlar
    // NOT: Stok artık sipariş OLUŞTURULDUĞU anda (CreateOrder) düşülüyor.
    // Bu yüzden tamamlama adımında tekrar stok düşümü YAPILMAZ, sadece durum güncellenir.
    [HttpPut("{id}/tamamla")]
    public async Task<IActionResult> SiparisTamamla(int id)
    {
        var siparis = await _context.Siparislers
            .FirstOrDefaultAsync(s => s.SiparisId == id);

        if (siparis == null)
            return NotFound("Sipariş bulunamadı.");

        if (siparis.SiparisDurumu == "TAMAMLANDI")
            return BadRequest("Sipariş zaten tamamlanmış.");

        if (siparis.SiparisDurumu == "IPTAL")
            return BadRequest("İptal edilen sipariş tamamlanamaz.");

        if (siparis.SiparisDurumu == "ODENDI")
            return BadRequest("Ödemesi alınmış sipariş tamamlanamaz.");

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
            Mesaj = "Sipariş başarıyla tamamlandı.",
            SiparisId = siparis.SiparisId
        });
    }

    // PUT /api/siparisler/{id}/durum -> Sipariş durumunu günceller
    [HttpPut("{id}/durum")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] SiparisDurumGuncelleDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.SiparisDurumu))
            return BadRequest(new { Mesaj = "Durum bilgisi gerekli." });

        var siparis = await _context.Siparislers
            .Include(s => s.Uye)  // 🔑 UYE BİLGİSİNİ DE AL
            .FirstOrDefaultAsync(s => s.SiparisId == id);

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
            YeniDurum = yeniDurum,
            UyeAdi = siparis.Uye != null ? siparis.Uye.UyeAdi + " " + siparis.Uye.UyeSoyadi : "Ziyaretçi",
            UyeId = siparis.UyeId
        });
    }

    // PUT /api/siparisler/5/iptal -> Siparişi iptal eder ve düşülen stoğu iade eder
    [HttpPut("{id}/iptal")]
    public async Task<IActionResult> SiparisIptal(int id)
    {
        var siparis = await _context.Siparislers
            .Include(s => s.Uye)  // 🔑 UYE BİLGİSİNİ DE AL
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

        // 🔑 Sipariş oluşturulurken düşülen stok, iptalde geri yükleniyor
        int stokPersonelId = siparis.PersonelId ?? 1;
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
                    PersonelId = stokPersonelId,
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
            .Include(s => s.Uye)  // 🔑 UYE BİLGİSİNİ DE AL
            .FirstOrDefaultAsync(s => s.SiparisId == id);

        if (siparis == null) return NotFound("Silinmek istenen sipariş bulunamadı.");

        try
        {
            _context.Siparislers.Remove(siparis);
            await _context.SaveChangesAsync();
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
}