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

// OpenAPI (Scalar) JWT Kilit Mekanizmasý Yapýlandýrmasý (.NET 9 Standartlarýnda)
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
                Description = "JWT Token deðerinizi giriniz."
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

// Veritabaný Baðlantýsý ve Geçici Hata Toleransý (Retry Mekanizmasý)
builder.Services.AddDbContext<DbRestoranContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DbRestoran"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,                          // Baðlantý koptuðunda 5 kez tekrar dener
            maxRetryDelay: TimeSpan.FromSeconds(10),   // Denemeler arasý 10 saniye bekler
            errorNumbersToAdd: null
        )
    ));

// JWT Kimlik Doðrulama Servisleri
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
app.UseMiddleware<Restoran.API.Middleware.GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();   // Önce: Sen kimsin? (Kimlik Doðrulama)
app.UseAuthorization();    // Sonra: Ne yapabilirsin? (Yetkilendirme)

app.MapControllers();

app.Run();