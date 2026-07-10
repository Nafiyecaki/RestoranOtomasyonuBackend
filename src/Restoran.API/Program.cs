using Microsoft.EntityFrameworkCore;
using Restoran.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Servis kayýtlarý
builder.Services.AddControllers();
builder.Services.AddOpenApi();                      // .NET 9 yerleþik OpenAPI (JSON üretir)

// EF Core: DbContext'i DI container'a kaydet
// "DbRestoran" -> appsettings.Development.json'daki ConnectionStrings anahtarý
builder.Services.AddDbContext<DbRestoranContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbRestoran")));

var app = builder.Build();

// Sadece geliþtirme ortamýnda API dokümantasyon arayüzü
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // /openapi/v1.json
    app.MapScalarApiReference();    // /scalar/v1 -> modern test arayüzü
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();