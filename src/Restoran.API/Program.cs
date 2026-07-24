using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Restoran.Data;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Restoran.API.Hubs; // 👈 SignalR Hub Namespace'i Eklendi

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// 💡 SignalR Servisi Eklendi
builder.Services.AddSignalR();

// ✅ CORS - SignalR ile Uyumlu Ayarlar (AllowCredentials Şart)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.SetIsOriginAllowed(origin => true) // SignalR'ın canlı bağlantı kurabilmesi için
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();                // WebSocket / SignalR için zorunlu
        });
});

// OpenAPI (Scalar) JWT Kilit Mekanizması
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

// Veritabanı Bağlantısı ve Geçici Hata Toleransı
builder.Services.AddDbContext<DbRestoranContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DbRestoran"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
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

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 💡 SignalR Endpoint Haritası
app.MapHub<SiparisHub>("/hubs/siparis");
app.Run();

// CORS politikasını ekleyin
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// ...

app.UseCors("AllowAll");