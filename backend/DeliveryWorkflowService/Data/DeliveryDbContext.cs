using Microsoft.EntityFrameworkCore;
using DeliveryWorkflowService.Models;

namespace DeliveryWorkflowService.Data;

public class DeliveryDbContext : DbContext
{
    public DeliveryDbContext(DbContextOptions<DeliveryDbContext> options) : base(options)
    {
    }

    public DbSet<DeliveryArrangement> DeliveryArrangements => Set<DeliveryArrangement>();
    public DbSet<DeliveryStatusHistory> DeliveryStatusHistories => Set<DeliveryStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DeliveryArrangement>(entity =>
        {
            entity.HasIndex(e => e.RequestId).IsUnique();
            entity.HasIndex(e => e.DonationId);
            entity.HasIndex(e => e.DonorId);
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<DeliveryStatusHistory>(entity =>
        {
            entity.HasIndex(e => e.DeliveryArrangementId);
            entity.HasIndex(e => e.Timestamp);
        });
    }
}
