using ComricFraudCalculatorBackend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComricFraudCalculatorBackend.Data.Configurations;

public class HrEventConfiguration : IEntityTypeConfiguration<HrEvent>
{
    public void Configure(EntityTypeBuilder<HrEvent> builder)
    {
        builder.ToTable("HrEvents");
        builder.HasKey(e => e.EventId);

        builder.Property(e => e.IdNumber).HasMaxLength(20).IsRequired();
        builder.Property(e => e.EventType).HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.EventDate).HasColumnType("datetime2");
        builder.Property(e => e.EmployerName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.EmployeeNumber).HasMaxLength(50);
        builder.Property(e => e.VerificationStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Notes).HasColumnType("nvarchar(max)");
        builder.Property(e => e.CreatedAt).HasColumnType("datetime2");

        builder.HasOne(e => e.Tenant)
            .WithMany(t => t.HrEvents)
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.EventDate);
    }
}
