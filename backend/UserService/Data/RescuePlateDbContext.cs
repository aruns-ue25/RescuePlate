using Microsoft.EntityFrameworkCore;
using UserService.Models;

namespace UserService.Data;

public class RescuePlateDbContext : DbContext
{
    public RescuePlateDbContext(DbContextOptions<RescuePlateDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<DonorProfile> DonorProfiles => Set<DonorProfile>();
    public DbSet<OrganizationProfile> OrganizationProfiles => Set<OrganizationProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Unique Email Constraint (SRS Unique Email Validation)
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Enum string conversion
        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();

        // 1-to-1 User <-> DonorProfile
        modelBuilder.Entity<User>()
            .HasOne(u => u.DonorProfile)
            .WithOne(p => p.User)
            .HasForeignKey<DonorProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // 1-to-1 User <-> OrganizationProfile
        modelBuilder.Entity<User>()
            .HasOne(u => u.OrganizationProfile)
            .WithOne(p => p.User)
            .HasForeignKey<OrganizationProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
