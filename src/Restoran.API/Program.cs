using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Restoran.Data;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// ✅ CORS - Tüm kaynaklara izin ver (Geliştirme için)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()        // Her yerden gelen isteklere izin ver
                  .AllowAnyMethod()        // Tüm HTTP metodlarına izin ver (GET, POST, PUT, DELETE, vs.)
                  .AllowAnyHeader();       // Tüm header'lara izin ver
        });
});

// OpenAPI (Scalar) JWT Kilit Mekanizması Yapılandırması (.NET 9 Standartlarında)
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        var schemes = new Dictionary<string, OpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT Token değerinizi giriniz."
            }
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = schemes;

        var securityRequirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            }] = Array.Empty<string>()
        };

        document.SecurityRequirements.Add(securityRequirement);

        return Task.CompletedTask;
    });
});

// Veritabanı Bağlantısı ve Geçici Hata Toleransı (Retry Mekanizması)
builder.Services.AddDbContext<DbRestoranContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DbRestoran"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,                          // Bağlantı koptuğunda 5 kez tekrar dener
            maxRetryDelay: TimeSpan.FromSeconds(10),   // Denemeler arası 10 saniye bekler
            errorNumbersToAdd: null
        )
    ));

// JWT Kimlik Doğrulama Servisleri
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

var app = builder.Build();

// Global Exception Middleware
app.UseMiddleware<Restoran.API.Middleware.GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// ⚠️ HttpsRedirection KALDIRILDI: frontend http://localhost:5000 kullanıyor,
// bu yönlendirme aktifken CORS preflight (OPTIONS) istekleri başarısız oluyordu.
// Eğer ileride backend'i HTTPS üzerinden çalıştırmaya karar verirseniz,
// frontend'deki API_URL'i de https://localhost:XXXX olarak güncelleyip
// bu satırı geri açabilirsiniz.
// app.UseHttpsRedirection();
app.UseCors("AllowAll");  // ← "FrontendPolicy" yerine "AllowAll" kullan

app.UseAuthentication();   // Önce: Sen kimsin? (Kimlik Doğrulama)
app.UseAuthorization();    // Sonra: Ne yapabilirsin? (Yetkilendirme)

app.MapControllers();

app.Run();