using ComricFraudCalculatorBackend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComricFraudCalculatorBackend.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(t => t.TenantId);

        builder.Property(t => t.TenantName).HasMaxLength(200).IsRequired();
        builder.Property(t => t.TenantCode).HasMaxLength(50).IsRequired();
        builder.Property(t => t.SubscriptionTier)
            .HasConversion<string>()
            .HasMaxLength(50);
        builder.Property(t => t.CreatedAt).HasColumnType("datetime2");

        builder.HasIndex(t => t.TenantCode).IsUnique();
    }
}
