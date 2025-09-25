using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ScspApi.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var cs = config.GetConnectionString("DefaultConnection")
                     ?? "Server=localhost;Database=scsapp;User=root;Password=&dbobdp3633.@&18/5”4#84*%;SslMode=None;AllowPublicKeyRetrieval=true";

            var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseMySql(cs, ServerVersion.AutoDetect(cs))
                .Options;

            return new ApplicationDbContext(opts);
        }
    }
}
