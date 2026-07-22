using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Restoran.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DbRestoranContext>
    {
        public DbRestoranContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DbRestoranContext>();

            // appsettings.json'dan connection string'i oku
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Eğer appsettings bulunamazsa varsayılan connection string kullan
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = "Server=localhost;Database=RestoranDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";
            }

            optionsBuilder.UseSqlServer(connectionString);

            return new DbRestoranContext(optionsBuilder.Options);
        }
    }
}