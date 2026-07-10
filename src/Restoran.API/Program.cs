using Microsoft.EntityFrameworkCore;
using Restoran.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi(); // .NET Yerleþik OpenAPI geldi mi

builder.Services.AddDbContext<DbRestoranContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("deneme123")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // JSON çýktýsý üretir
    app.MapScalarApiReference(); // Modern API arayüzü (/scalar/v22222)
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();