using ComricFraudCalculatorBackend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComricFraudCalculatorBackend.Data.Configurations;

public class SignalConfiguration : IEntityTypeConfiguration<Signal>
{
    public void Configure(EntityTypeBuilder<Signal> builder)
    {
        builder.ToTable("Signals");
        builder.HasKey(s => s.SignalId);

        builder.Property(s => s.IdNumberHash).HasMaxLength(128).IsRequired();
        builder.Property(s => s.SignalType).HasConversion<string>().HasMaxLength(50);
        builder.Property(s => s.SignalCategory).HasConversion<string>().HasMaxLength(50);
        builder.Property(s => s.FirstSeen).HasColumnType("datetime2");
        builder.Property(s => s.LastSeen).HasColumnType("datetime2");

        builder.HasIndex(s => s.IdNumberHash);
        builder.HasIndex(s => new { s.IdNumberHash, s.SignalType, s.SignalCategory }).IsUnique();
        builder.HasIndex(s => s.IsActive);
    }
}
