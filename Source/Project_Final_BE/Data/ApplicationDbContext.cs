using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Project_Final_BE.Models;

namespace Project_Final_BE.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Book> Books => Set<Book>();
        public DbSet<BorrowRecord> BorrowRecords => Set<BorrowRecord>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Category configuration
            builder.Entity<Category>(entity =>
            {
                entity.HasIndex(c => c.Name).IsUnique();
                entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            });

            // Book configuration
            builder.Entity<Book>(entity =>
            {
                entity.Property(b => b.Title).IsRequired().HasMaxLength(200);
                entity.Property(b => b.Author).IsRequired().HasMaxLength(100);
                entity.Property(b => b.Price).HasPrecision(18, 2);

                entity.Property(b => b.RowVersion)
                      .IsRowVersion();

                entity.HasOne(b => b.Category)
                      .WithMany(c => c.Books)
                      .HasForeignKey(b => b.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // BorrowRecord configuration
            builder.Entity<BorrowRecord>(entity =>
            {
                entity.Property(br => br.Fine).HasPrecision(18, 2);
                entity.Property(br => br.CompensationFee).HasPrecision(18, 2);
                entity.Property(br => br.Status).IsRequired().HasMaxLength(20);
                entity.HasIndex(br => new { br.Status, br.ReturnRequestedAt });

                entity.HasOne(br => br.User)
                      .WithMany(u => u.BorrowRecords)
                      .HasForeignKey(br => br.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(br => br.Book)
                      .WithMany(b => b.BorrowRecords)
                      .HasForeignKey(br => br.BookId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Notification configuration
            builder.Entity<Notification>(entity =>
            {
                entity.Property(n => n.Title).IsRequired().HasMaxLength(200);
                entity.Property(n => n.Message).IsRequired().HasMaxLength(1000);
                entity.Property(n => n.Type).IsRequired().HasMaxLength(50);

                entity.HasOne(n => n.User)
                      .WithMany(u => u.Notifications)
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
