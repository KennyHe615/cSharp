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


namespace tests.Integration.Persistence;

public sealed class DateTimeEstConventionTests
{
    [Fact]
    public void SkillDateModified_UsesEstDateTimeOffsetConverter()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                                                .UseInMemoryDatabase($"est-convention-{Guid.NewGuid()}")
                                                .Options;

        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();
        AuditSaveChangesInterceptor interceptor = new AuditSaveChangesInterceptor(dateTimeProvider);

        using AppDbContext db = new AppDbContext(options,
                                                 Options.Create(new DatabaseOptions()),
                                                 new StubLobContext(),
                                                 dateTimeProvider,
                                                 interceptor);

        IEntityType entityType = db.Model.FindEntityType(typeof(Skill))!;
        IProperty property = entityType.FindProperty(nameof(Skill.DateModified))!;
        ValueConverter converter = property.GetValueConverter()!;

        // UTC input
        DateTimeOffset utc = new DateTimeOffset(2026,
                                                2,
                                                26,
                                                15,
                                                0,
                                                0,
                                                TimeSpan.Zero);

        object? providerValue = converter.ConvertToProvider(utc);

        DateTimeOffset est = Assert.IsType<DateTimeOffset>(providerValue);
        Assert.Equal(TimeSpan.FromHours(-5), est.Offset);
        Assert.Equal(10, est.Hour);// 15:00 UTC -> 10:00 EST (winter)
    }

    [Fact]
    public void DateTimeOffsetColumns_AreConfiguredAsDatetimeOffset()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                                                .UseInMemoryDatabase($"est-column-type-{Guid.NewGuid()}")
                                                .Options;

        FixedEstDateTimeProvider dateTimeProvider = new FixedEstDateTimeProvider();
        AuditSaveChangesInterceptor interceptor = new AuditSaveChangesInterceptor(dateTimeProvider);

        using AppDbContext db = new AppDbContext(options,
                                                 Options.Create(new DatabaseOptions()),
                                                 new StubLobContext(),
                                                 dateTimeProvider,
                                                 interceptor);

        IEntityType entityType = db.Model.FindEntityType(typeof(Skill))!;
        IProperty property = entityType.FindProperty(nameof(Skill.DateModified))!;

        object? configuredType = property.FindAnnotation(RelationalAnnotationNames.ColumnType)
                                        ?.Value;
        Assert.Equal("datetimeoffset(0)", configuredType);
    }
}
