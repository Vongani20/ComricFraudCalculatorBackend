using ComricFraudCalculatorBackend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComricFraudCalculatorBackend.Data.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("ActivityLogs");
        builder.HasKey(a => a.ActivityLogId);

        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Endpoint).HasMaxLength(500).IsRequired();
        builder.Property(a => a.HttpMethod).HasMaxLength(10).IsRequired();
        builder.Property(a => a.ClientIp).HasMaxLength(45);
        builder.Property(a => a.CreatedAt).HasColumnType("datetime2");

        builder.HasOne(a => a.Tenant)
            .WithMany(t => t.ActivityLogs)
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => a.CreatedAt);
    }
}
