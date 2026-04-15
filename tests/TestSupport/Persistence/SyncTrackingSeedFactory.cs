using Application.Enums;

using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.SyncTracking;


namespace tests.TestSupport.Persistence;

public static class SyncTrackingSeedFactory
{
    public static async Task<SyncRequestEntity> SeedRequestAsync(AppDbContext dbContext, string category, SyncMode mode)
    {
        SyncRequestEntity request = new SyncRequestEntity { Category = category, Mode = mode };
        request.RebuildScopeKey();
        dbContext.Set<SyncRequestEntity>()
                 .Add(request);
        await dbContext.SaveChangesAsync();

        return request;
    }
}
