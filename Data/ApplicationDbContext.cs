using ComricFraudCalculatorBackend.Data.Configurations;
using ComricFraudCalculatorBackend.Entities;
using Microsoft.EntityFrameworkCore;

namespace ComricFraudCalculatorBackend.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<HrEvent> HrEvents => Set<HrEvent>();
    public DbSet<MnoEvent> MnoEvents => Set<MnoEvent>();
    public DbSet<Signal> Signals => Set<Signal>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TenantConfiguration());
        modelBuilder.ApplyConfiguration(new HrEventConfiguration());
        modelBuilder.ApplyConfiguration(new MnoEventConfiguration());
        modelBuilder.ApplyConfiguration(new SignalConfiguration());
        modelBuilder.ApplyConfiguration(new ActivityLogConfiguration());
    }
}
