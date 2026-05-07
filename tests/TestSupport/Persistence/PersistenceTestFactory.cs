using Application.Abstractions.Persistence;

using Infrastructure.Persistence;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Interceptors;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

using Moq;

using SharedKernel.Time;

using tests.TestSupport.Context;


namespace tests.TestSupport.Persistence;

public static class PersistenceTestFactory
{
    public static AppDbContext CreateInMemoryDbContext(IDateTimeProvider dateTimeProvider, string? dbName = null)
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(dbName
                    ?? Guid.NewGuid()
                           .ToString("N"))
               .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
               .Options;

        AuditSaveChangesInterceptor interceptor = new AuditSaveChangesInterceptor(dateTimeProvider);

        AppDbContext dbContext = new AppDbContext(options,
                                                  Options.Create(new DatabaseOptions()),
                                                  new StubLobContext(),
                                                  dateTimeProvider,
                                                  interceptor);

        dbContext.Database.EnsureCreated();

        return dbContext;
    }

    public static AppDbContext CreateSqliteDbContext(IDateTimeProvider dateTimeProvider)
    {
        SqliteConnection connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection)
               .Options;

        AuditSaveChangesInterceptor interceptor = new AuditSaveChangesInterceptor(dateTimeProvider);

        AppDbContext dbContext = new AppDbContext(options,
                                                  Options.Create(new DatabaseOptions()),
                                                  new StubLobContext(),
                                                  dateTimeProvider,
                                                  interceptor);

        CreateSyncTrackingSqliteSchema(dbContext);

        return dbContext;
    }

    public static Mock<IUnitOfWork> CreateUnitOfWork<TEntity>(AppDbContext dbContext)
            where TEntity : class
    {
        Mock<IUnitOfWork> uow = new Mock<IUnitOfWork>(MockBehavior.Strict);

        uow.Setup(x => x.UpsertAsync(It.IsAny<TEntity>(), null, It.IsAny<CancellationToken>()))
           .Callback<object, Action<TEntity>?, CancellationToken>((entity, _, _) => dbContext.Set<TEntity>()
                                                                         .Add((TEntity)entity))
           .Returns(Task.CompletedTask);

        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns<CancellationToken>(dbContext.SaveChangesAsync);

        return uow;
    }

    #region ========== *** Private Section *** ==========

    private static void CreateSyncTrackingSqliteSchema(AppDbContext dbContext)
    {
        dbContext.Database.ExecuteSqlRaw("""
                                         CREATE TABLE sync_request
                                         (
                                             id INTEGER PRIMARY KEY,
                                             public_id TEXT NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                                             category TEXT NOT NULL,
                                             mode TEXT NOT NULL,
                                             status TEXT NOT NULL DEFAULT 'PENDING',
                                             reopen_count INTEGER NOT NULL DEFAULT 0,
                                             interval TEXT NULL,
                                             page_number INTEGER NULL,
                                             genesys_job_id TEXT NULL,
                                             scope_key TEXT NOT NULL,
                                             current_run_id INTEGER NULL,
                                             app_created_at_eastern TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                             app_updated_at_eastern TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                                         );

                                         CREATE TABLE sync_run
                                         (
                                             id INTEGER PRIMARY KEY,
                                             request_id INTEGER NOT NULL,
                                             status TEXT NOT NULL,
                                             superseded_by_run_id INTEGER NULL,
                                             attempt_no INTEGER NOT NULL,
                                             run_started_at_eastern TEXT NULL,
                                             run_completed_at_eastern TEXT NULL,
                                             failure_reason TEXT NULL,
                                             app_created_at_eastern TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                             app_updated_at_eastern TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                             FOREIGN KEY (request_id) REFERENCES sync_request(id),
                                             FOREIGN KEY (superseded_by_run_id) REFERENCES sync_run(id)
                                         );

                                         CREATE TABLE sync_run_item
                                         (
                                             id INTEGER PRIMARY KEY,
                                             run_id INTEGER NOT NULL,
                                             step TEXT NOT NULL,
                                             cursor TEXT NULL,
                                             page_number INTEGER NULL,
                                             status TEXT NOT NULL,
                                             failure_reason TEXT NULL,
                                             claimed_by TEXT NULL,
                                             lease_token TEXT NULL,
                                             claimed_at_eastern TEXT NULL,
                                             claim_expires_at_eastern TEXT NULL,
                                             attempt_count INTEGER NOT NULL DEFAULT 0,
                                             last_heartbeat_at_eastern TEXT NULL,
                                             app_created_at_eastern TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                             app_updated_at_eastern TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                             CHECK (((page_number IS NULL AND cursor IS NOT NULL)
                                                     OR (page_number IS NOT NULL AND cursor IS NULL))),
                                             FOREIGN KEY (run_id) REFERENCES sync_run(id)
                                         );
                                         CREATE TABLE analytics_recovery_request
                                         (
                                             id INTEGER PRIMARY KEY,
                                             public_id TEXT NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                                             category TEXT NOT NULL,
                                             status TEXT NOT NULL DEFAULT 'PENDING',
                                             interval TEXT NULL,
                                             genesys_job_id TEXT NULL,
                                             failure_reason TEXT NULL,
                                             scope_key TEXT NOT NULL,
                                             app_created_at_eastern TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                             app_updated_at_eastern TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                                         );

                                         CREATE UNIQUE INDEX UX_sync_request_scope_key_full
                                             ON sync_request(scope_key)
                                             WHERE mode = 'FULL';

                                         CREATE UNIQUE INDEX UX_sync_request_scope_key_incremental
                                             ON sync_request(scope_key)
                                             WHERE mode = 'INCREMENTAL';

                                         CREATE UNIQUE INDEX UX_sync_request_scope_key_recovery_active
                                             ON sync_request(scope_key)
                                             WHERE mode = 'RECOVERY' AND status IN ('PENDING', 'RUNNING');

                                         CREATE UNIQUE INDEX UX_sync_run_request_active
                                             ON sync_run(request_id)
                                             WHERE status IN ('PENDING', 'RUNNING');

                                         CREATE UNIQUE INDEX UX_sync_run_item_run_step_cursor
                                             ON sync_run_item(run_id, step, cursor)
                                             WHERE page_number IS NULL AND cursor IS NOT NULL;

                                         CREATE UNIQUE INDEX UX_sync_run_item_run_step_page_number
                                             ON sync_run_item(run_id, step, page_number)
                                             WHERE page_number IS NOT NULL;

                                         CREATE INDEX IX_sync_run_item_run_step_status_claim_exp_page
                                             ON sync_run_item(run_id, step, status, claim_expires_at_eastern, page_number)
                                             WHERE page_number IS NOT NULL;

                                         CREATE UNIQUE INDEX UX_analytics_recovery_request_scope_key_active
                                         ON analytics_recovery_request(scope_key)
                                         WHERE status IN ('PENDING', 'RUNNING');
                                         """);
    }

    #endregion
}
