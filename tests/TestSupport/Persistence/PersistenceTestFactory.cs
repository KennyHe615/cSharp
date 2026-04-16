using Application.Abstractions.Persistence;

using Infrastructure.Persistence;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Interceptors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

using Moq;

using SharedKernel.Time;

using tests.TestSupport.Context;


namespace tests.TestSupport.Persistence;

public static class PersistenceTestFactory
{
    public static AppDbContext CreateDbContext(IDateTimeProvider dateTimeProvider, string? dbName = null)
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
}
