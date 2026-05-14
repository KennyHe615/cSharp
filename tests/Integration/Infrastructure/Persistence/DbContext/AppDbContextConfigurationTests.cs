using Infrastructure.Persistence;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Interceptors;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using tests.TestSupport.Context;
using tests.TestSupport.Time;

using Xunit;


namespace tests.Integration.Infrastructure.Persistence.DbContext;

public sealed class AppDbContextConfigurationTests
{
    [Fact]
    public void OnConfiguring_WhenNoProviderAndConnectionStringMissing_ThrowsDbContextConfigurationException()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().Options;

        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();
        AuditSaveChangesInterceptor interceptor = new AuditSaveChangesInterceptor(dateTimeProvider);

        using AppDbContext db = new AppDbContext(options,
                                                 Options.Create(new DatabaseOptions()),
                                                 new StubLobContext { DbConnectionString = string.Empty },
                                                 dateTimeProvider,
                                                 interceptor);

        DbContextConfigurationException ex = Assert.Throws<DbContextConfigurationException>(() =>
        {
            _ = db.Model;
        });

        Assert.Contains("ConnectionString", ex.Message);
        Assert.Contains("NTT", ex.Message);
    }

    [Fact]
    public void OnConfiguring_WhenNoProviderAndCommandTimeoutInvalid_ThrowsDbContextConfigurationException()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().Options;

        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();
        AuditSaveChangesInterceptor interceptor = new AuditSaveChangesInterceptor(dateTimeProvider);

        DatabaseOptions badOptions = new DatabaseOptions
                                     {
                                         CommandTimeout = -2
                                     };

        using AppDbContext db = new AppDbContext(options,
                                                 Options.Create(badOptions),
                                                 new StubLobContext
                                                 {
                                                     DbConnectionString =
                                                             "Server=(localdb)\\MSSQLLocalDB;Database=coverage_probe;Trusted_Connection=True;"
                                                 },
                                                 dateTimeProvider,
                                                 interceptor);

        DbContextConfigurationException ex = Assert.Throws<DbContextConfigurationException>(() =>
        {
            _ = db.Model;
        });

        Assert.Contains("Failed to configure DbContext", ex.Message);
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    [Fact]
    public async Task OnConfiguring_WhenProviderAlreadyConfigured_DoesNotRequireConnectionString()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                                                .UseInMemoryDatabase($"app-db-config-{Guid.NewGuid()}")
                                                .Options;

        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();
        AuditSaveChangesInterceptor interceptor = new AuditSaveChangesInterceptor(dateTimeProvider);

        await using AppDbContext db = new AppDbContext(options,
                                                       Options.Create(new DatabaseOptions()),
                                                       new StubLobContext { DbConnectionString = string.Empty },
                                                       dateTimeProvider,
                                                       interceptor);

        Exception? ex = await Record.ExceptionAsync(async () => await db.Database.EnsureCreatedAsync());

        Assert.Null(ex);
    }
}
