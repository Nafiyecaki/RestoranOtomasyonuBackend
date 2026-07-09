using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.Data;

namespace Restoran.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UrunlerController : ControllerBase
    {
        private readonly DbRestoranContext _context;

        // Constructor ile DbContext'i içeriye enjekte ediyoruz (Dependency Injection)
        public UrunlerController(DbRestoranContext context)
        {
            _context = context;
        }

        // GET: /api/urunler
        [HttpGet]
        public async Task<IActionResult> GetUrunler()
        {
            try
            {
                // Test verisi çok büyük olduğu için ilk aşamada sunucuyu yormamak adına ilk 100 ürünü çekelim
                var urunler = await _context.Urunlers
                    .Take(100)
                    .ToListAsync();

                return Ok(urunler);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Veritabanı hatası: {ex.Message}");
            }
        }
    }
}