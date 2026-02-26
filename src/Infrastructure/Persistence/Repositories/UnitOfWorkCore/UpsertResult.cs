namespace Infrastructure.Persistence.Repositories.UnitOfWorkCore;

/// <summary>
/// Represents the result of an upsert (update or insert) operation containing statistics and key information.
/// </summary>
/// <param name="IncomingKeys">
/// The set of all primary keys from the incoming entity list. Used to identify entities that exist in the
/// database but are missing from the incoming data.
/// </param>
/// <param name="AddedCount">
/// The number of entities that were added to the database context because their primary keys were not found
/// in the existing database records.
/// </param>
/// <param name="UpdatedCount">
/// The number of entities that were updated in the database context because their primary keys matched
/// existing database records.
/// </param>
/// <remarks>
/// This record struct provides a lightweight value type for communicating upsert operation results.
/// The IncomingKeys set enables efficient comparison with database entities to identify records that
/// should be soft-deleted or marked as inactive when they no longer appear in the source data.
/// <para>
/// <b>Note</b>: The counts represent entities tracked for changes in the context but do not reflect actual
/// database modifications until SaveChanges is called.
/// </para>
/// </remarks>
internal readonly record struct UpsertResult(HashSet<object> IncomingKeys,
                                             int AddedCount,
                                             int UpdatedCount);
