using Microsoft.EntityFrameworkCore;
using RequestWorkflowService.Models;

namespace RequestWorkflowService.Data;

public class RequestDbContext : DbContext
{
    public RequestDbContext(DbContextOptions<RequestDbContext> options) : base(options)
    {
    }

    public DbSet<FoodRequest> Requests => Set<FoodRequest>();

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
    }
}
