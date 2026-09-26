using Microsoft.EntityFrameworkCore;
using RequestWorkflowService.Models;

namespace RequestWorkflowService.Data;

public class RequestDbContext : DbContext
{
    public RequestDbContext(DbContextOptions<RequestDbContext> options) : base(options)
    {
    }

    public DbSet<FoodRequest> Requests => Set<FoodRequest>();
    public DbSet<OrgFoodNeedRequest> OrgNeedRequests => Set<OrgFoodNeedRequest>();
    public DbSet<DonorFoodOffer> DonorOffers => Set<DonorFoodOffer>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FoodRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DonationTitle).IsRequired().HasMaxLength(200);
            entity.Property(e => e.OrganizationId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.OrganizationName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.DonorId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.RejectionReason).HasMaxLength(500);

            entity.HasIndex(e => e.DonationId);
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.DonorId);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<OrgFoodNeedRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrganizationId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.OrganizationName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);

            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.NeededByDate);
        });

        modelBuilder.Entity<DonorFoodOffer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrgFoodNeedRequestTitle).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DonorId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DonorName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.FoodType).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);

            entity.HasIndex(e => e.OrgFoodNeedRequestId);
            entity.HasIndex(e => e.DonorId);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsRead);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
