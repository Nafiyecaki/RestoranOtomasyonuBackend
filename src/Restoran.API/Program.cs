using Microsoft.EntityFrameworkCore;
using Restoran.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi(); // .NET Yerleþik OpenAPI

builder.Services.AddDbContext<DbRestoranContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // JSON çýktýsý üretir
    app.MapScalarApiReference(); // Modern API arayüzü (/scalar/v1)
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();