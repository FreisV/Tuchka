using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Tuchka.IdentityAuth;
using Tuchka.Models;

namespace Tuchka.DbContext
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        public DbSet<Document> Documents { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Document>(entity =>
            {
                entity.ToTable("Document");

                entity.Property(e => e.FileName)
                .HasMaxLength(500)
                .IsUnicode(true);

                entity.Property(e => e.ContentType)
                .HasMaxLength(100)
                .IsUnicode(false);

            });

            base.OnModelCreating(builder);
        }
    }
}
