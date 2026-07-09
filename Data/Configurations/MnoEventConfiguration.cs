using ComricFraudCalculatorBackend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComricFraudCalculatorBackend.Data.Configurations;

public class MnoEventConfiguration : IEntityTypeConfiguration<MnoEvent>
{
    public void Configure(EntityTypeBuilder<MnoEvent> builder)
    {
        builder.ToTable("MnoEvents");
        builder.HasKey(e => e.EventId);

        builder.Property(e => e.IdNumber).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Msisdn).HasMaxLength(20).IsRequired();
        builder.Property(e => e.EventType).HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.EventDate).HasColumnType("datetime2");
        builder.Property(e => e.ApplicationChannel).HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.OutletOrDealer).HasMaxLength(200).IsRequired();
        builder.Property(e => e.DeviceImei).HasMaxLength(20);
        builder.Property(e => e.FlagReason).HasColumnType("nvarchar(max)");
        builder.Property(e => e.CreatedAt).HasColumnType("datetime2");

        builder.HasOne(e => e.Tenant)
            .WithMany(t => t.MnoEvents)
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.EventDate);
    }
}
