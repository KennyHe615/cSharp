using Infrastructure.ExternalApis.Providers.Genesys.Enums;
using Infrastructure.Persistence;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.References;
using Infrastructure.Persistence.Interceptors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;

using tests.TestSupport.Context;
using tests.TestSupport.Time;

using Xunit;


namespace Tests.Integration.Persistence;

public sealed class EnumPersistenceFormatTests
{
    [Fact]
    public void SkillState_UsesUpperSnakeEnumValueConverter()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                                                .UseInMemoryDatabase($"enum-format-{Guid.NewGuid()}")
                                                .Options;

        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();
        AuditSaveChangesInterceptor interceptor = new AuditSaveChangesInterceptor(dateTimeProvider);

        using AppDbContext db = new AppDbContext(options,
                                                 Options.Create(new DatabaseOptions()),
                                                 new StubLobContext(),
                                                 dateTimeProvider,
                                                 interceptor);

        IEntityType entityType = db.Model.FindEntityType(typeof(Skill))!;
        IProperty stateProperty = entityType.FindProperty(nameof(Skill.State))!;
        ValueConverter converter = stateProperty.GetValueConverter()!;

        object? providerValue = converter.ConvertToProvider(State.Inactive);

        Assert.Equal("INACTIVE", providerValue?.ToString());
    }
}
