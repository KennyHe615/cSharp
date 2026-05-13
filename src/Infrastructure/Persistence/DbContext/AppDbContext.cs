using Application.Abstractions.Context;

using Infrastructure.Persistence.Converters;
using Infrastructure.Persistence.Entities;
using Infrastructure.Persistence.Interceptors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;

using SharedKernel.Extensions;
using SharedKernel.Time;


namespace Infrastructure.Persistence.DbContext;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options,
                                 IOptions<DatabaseOptions> databaseOptions,
                                 ILobContext lobContext,
                                 IDateTimeProvider dateTimeProvider,
                                 AuditSaveChangesInterceptor auditInterceptor)
        : Microsoft.EntityFrameworkCore.DbContext(options)
{
    private const string DefaultDateTimeOffsetColumnType = "datetimeoffset(0)";
    private const string PrimaryKeyDateTimeOffsetColumnType = "datetimeoffset(3)";

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        DatabaseOptions dbOptions = databaseOptions.Value;

        // Detect whether any database provider (SQL Server, InMemory, Sqlite, etc.) was configured externally.
        bool providerConfigured = optionsBuilder.Options.Extensions.Any(x => x.Info.IsDatabaseProvider);

        // Always register cross-cutting interceptor (works for SQL Server and InMemory tests).
        optionsBuilder.AddInterceptors(auditInterceptor);

        if (dbOptions.EnableDetailedErrors) optionsBuilder.EnableDetailedErrors();
        if (dbOptions.EnableSensitiveDataLogging) optionsBuilder.EnableSensitiveDataLogging();

        // Only set provider/connection when no relational provider is configured yet.
        if (providerConfigured) return;

        string connectionString = lobContext.DbConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new
                    DbContextConfigurationException($"Database ConnectionString for LOB '{lobContext.LobName}' is missing.");
        }

        try
        {
            optionsBuilder.UseSqlServer(connectionString,
                                        sqlOptions =>
                                        {
                                            sqlOptions.EnableRetryOnFailure(dbOptions.MaxRetryCount);
                                            sqlOptions.CommandTimeout(dbOptions.CommandTimeout);
                                        });
        }
        catch (ArgumentException ex)
        {
            throw new DbContextConfigurationException($"Invalid connection string for LOB '{lobContext.LobName}'.", ex);
        }
        catch (InvalidOperationException ex)
        {
            throw new DbContextConfigurationException($"Failed to configure DbContext for LOB '{lobContext.LobName}'.",
                                                      ex);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // This automatically finds all classes in this assembly that implement IEntityTypeConfiguration
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ApplyEstDateTimeConvention(modelBuilder, dateTimeProvider);

        ApplyEnumSnakeUpperConvention(modelBuilder);

        ApplySnakeCaseNamingConvention(modelBuilder);

        ApplyAuditConvention(modelBuilder);
    }

    internal static bool IsUtcDateTimeOffsetProperty(IReadOnlyProperty property)
    {
        return property.Name.EndsWith("Utc", StringComparison.Ordinal);
    }

    #region ========== *** Private Section *** ==========

    private static void ApplyEstDateTimeConvention(ModelBuilder modelBuilder, IDateTimeProvider dateTimeProvider)
    {
        ValueConverter<DateTimeOffset, DateTimeOffset> dtoConverter =
                new ValueConverter<DateTimeOffset, DateTimeOffset>(v => dateTimeProvider.ConvertToEst(v),
                                                                   v => dateTimeProvider.ConvertToEst(v));

        ValueConverter<DateTimeOffset?, DateTimeOffset?> dtoNullableConverter =
                new ValueConverter<DateTimeOffset?, DateTimeOffset?>(v =>
                                                                             v.HasValue
                                                                                     ? dateTimeProvider
                                                                                            .ConvertToEst(v.Value)
                                                                                     : null,
                                                                     v => v.HasValue
                                                                                  ? dateTimeProvider
                                                                                         .ConvertToEst(v.Value)
                                                                                  : null);

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (IMutableProperty property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset))
                {
                    property.SetColumnType(property.GetColumnType() ?? DefaultDateTimeOffsetColumnType);

                    if (!IsUtcDateTimeOffsetProperty(property))
                    {
                        property.SetValueConverter(dtoConverter);
                    }
                }
                else if (property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetColumnType(property.GetColumnType() ?? DefaultDateTimeOffsetColumnType);

                    if (!IsUtcDateTimeOffsetProperty(property))
                    {
                        property.SetValueConverter(dtoNullableConverter);
                    }
                }
            }

            // ensure all PK DateTimeOffset columns are also forced (some configs override keys).
            foreach (IMutableKey key in entityType.GetKeys())
            {
                foreach (IMutableProperty keyProp in key.Properties.Where(p => p.ClrType == typeof(DateTimeOffset)))
                {
                    keyProp.SetColumnType(keyProp.GetColumnType() ?? PrimaryKeyDateTimeOffsetColumnType);

                    if (!IsUtcDateTimeOffsetProperty(keyProp))
                    {
                        keyProp.SetValueConverter(dtoConverter);
                    }
                }

                foreach (IMutableProperty keyProp in key.Properties.Where(p => p.ClrType == typeof(DateTimeOffset?)))
                {
                    keyProp.SetColumnType(keyProp.GetColumnType() ?? PrimaryKeyDateTimeOffsetColumnType);

                    if (!IsUtcDateTimeOffsetProperty(keyProp))
                    {
                        keyProp.SetValueConverter(dtoNullableConverter);
                    }
                }
            }
        }
    }

    private static void ApplyEnumSnakeUpperConvention(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (IMutableProperty property in entityType.GetProperties())
            {
                Type clrType = property.ClrType;
                Type? nullableUnderlying = Nullable.GetUnderlyingType(clrType);

                Type? enumType =
                        clrType.IsEnum ? clrType :
                        nullableUnderlying is not null && nullableUnderlying.IsEnum ? nullableUnderlying : null;

                if (enumType is null) continue;

                ValueConverter converter = CreateEnumConverter(enumType, clrType);
                property.SetValueConverter(converter);

                property.SetColumnType("nvarchar(64)");
            }
        }
    }

    private static void ApplySnakeCaseNamingConvention(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entity in modelBuilder.Model.GetEntityTypes())
        {
            // Table name can be null for non-table mapped types; skip those safely.
            string? tableName = entity.GetTableName();
            if (!string.IsNullOrWhiteSpace(tableName))
            {
                entity.SetTableName(tableName.ToSnakeCase());
            }

            // Set column names to snake_case
            foreach (IMutableProperty property in entity.GetProperties())
            {
                property.SetColumnName(property.Name.ToSnakeCase());
            }

            // Set key and index names to snake_case
            foreach (IMutableKey key in entity.GetKeys())
            {
                string? keyName = key.GetName();
                if (!string.IsNullOrWhiteSpace(keyName))
                {
                    key.SetName(keyName.ToSnakeCase());
                }
            }

            foreach (IMutableForeignKey foreignKey in entity.GetForeignKeys())
            {
                string? fkName = foreignKey.GetConstraintName();
                if (!string.IsNullOrWhiteSpace(fkName))
                {
                    foreignKey.SetConstraintName(fkName.ToSnakeCase());
                }
            }

            foreach (IMutableIndex index in entity.GetIndexes())
            {
                string? indexName = index.GetDatabaseName();
                if (!string.IsNullOrWhiteSpace(indexName))
                {
                    index.SetDatabaseName(indexName.ToSnakeCase());
                }
            }
        }
    }

    private static void ApplyAuditConvention(ModelBuilder modelBuilder)
    {
        const string easternNowSql =
                "SWITCHOFFSET(SYSDATETIMEOFFSET(), DATENAME(TzOffset, SYSDATETIMEOFFSET() AT TIME ZONE 'Eastern Standard Time'))";

        foreach (IMutableEntityType entity in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(Audit).IsAssignableFrom(entity.ClrType)) continue;

            modelBuilder.Entity(entity.ClrType)
                        .Property(nameof(Audit.AppCreatedAtEastern))
                        .IsRequired()
                        .HasColumnType(DefaultDateTimeOffsetColumnType)
                        .HasDefaultValueSql(easternNowSql);

            modelBuilder.Entity(entity.ClrType)
                        .Property(nameof(Audit.AppUpdatedAtEastern))
                        .IsRequired()
                        .HasColumnType(DefaultDateTimeOffsetColumnType)
                        .HasDefaultValueSql(easternNowSql);

            modelBuilder.Entity(entity.ClrType)
                        .HasIndex(nameof(Audit.AppUpdatedAtEastern));
        }
    }

    private static ValueConverter CreateEnumConverter(Type enumType, Type propertyType)
    {
        if (!enumType.IsEnum)
        {
            throw
                    new
                            InvalidOperationException($"CreateEnumConverter called with non-enum type '{enumType.FullName}'.");
        }

        bool isNullableEnumProperty = Nullable.GetUnderlyingType(propertyType) is not null;

        Type converterType = isNullableEnumProperty
                                     ? typeof(NullableEnumToSnakeUpperStringConverter<>).MakeGenericType(enumType)
                                     : typeof(EnumToSnakeUpperStringConverter<>).MakeGenericType(enumType);

        return Activator.CreateInstance(converterType) as ValueConverter
               ?? throw new
                       InvalidOperationException($"Failed to create enum value converter '{converterType.FullName}'.");
    }

    #endregion
}
