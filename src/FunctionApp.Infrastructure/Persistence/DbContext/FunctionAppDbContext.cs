using Microsoft.EntityFrameworkCore;


namespace FunctionApp.Infrastructure.Persistence.DbContext;

public class FunctionAppDbContext(DbContextOptions<FunctionAppDbContext> options)
    : Microsoft.EntityFrameworkCore.DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // This automatically finds all classes in this assembly that implement IEntityTypeConfiguration
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FunctionAppDbContext).Assembly);
    }
}
