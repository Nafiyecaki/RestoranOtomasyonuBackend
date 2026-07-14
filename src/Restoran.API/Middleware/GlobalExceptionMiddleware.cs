using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Restoran.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context); // İsteği normal akışına devam ettir
        }
        catch (DbUpdateException ex)
        {
            // Veritabanı kısıt hataları (FK, unique, null vb.)
            _logger.LogError(ex, "Veritabanı güncelleme hatası");
            await YanitYaz(context, StatusCodes.Status400BadRequest,
                "İşlem veritabanı kuralları nedeniyle gerçekleştirilemedi. İlişkili kayıtları kontrol edin.");
        }
        catch (Exception ex)
        {
            // Beklenmeyen tüm diğer hatalar
            _logger.LogError(ex, "Beklenmeyen hata");
            await YanitYaz(context, StatusCodes.Status500InternalServerError,
                "Sunucuda beklenmeyen bir hata oluştu.");
        }
    }

    private static async Task YanitYaz(HttpContext context, int statusCode, string mesaj)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var yanit = JsonSerializer.Serialize(new { Mesaj = mesaj });
        await context.Response.WriteAsync(yanit);
    }
}