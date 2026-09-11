using Microsoft.EntityFrameworkCore;
using DonationService.Models;

namespace DonationService.Data;

public class DonationDbContext : DbContext
{
    public DonationDbContext(DbContextOptions<DonationDbContext> options) : base(options)
    {
    }

    public DbSet<Donation> Donations => Set<Donation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Donation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FoodTitle).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Unit).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CollectionMode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DonorName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.DonorEmail).HasMaxLength(150);
            entity.Property(e => e.Location).HasMaxLength(255);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.DietaryTags).HasMaxLength(255);

            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DonorId);
        });
    }
}
