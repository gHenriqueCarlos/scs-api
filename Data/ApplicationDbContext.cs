using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ScspApi.Models;

namespace ScspApi.Data
{
    public class ApplicationDbContext : IdentityDbContext<User> 
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<OtpCode> OtpCodes { get; set; } = default!;
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public DbSet<Problem> Problems { get; set; }

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<OtpCode>(e =>
            {
                e.HasIndex(x => new { x.UserId, x.Purpose, x.ExpiresAt });
                e.Property(x => x.CodeHash).HasMaxLength(128);
                e.Property(x => x.Salt).HasMaxLength(64);
            });

            b.Entity<RefreshToken>(e =>
            {
                e.HasIndex(x => x.Token).IsUnique();
                e.HasIndex(x => new { x.UserId, x.DeviceId });
            });
        }
    }
}
