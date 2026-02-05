using Application.Dtos.References;
using Application.Enums;
using Application.References;
using Application.Shared.Repositories;

using AutoMapper;

using Infrastructure.Persistence.Entities.References;


namespace Infrastructure.Persistence.Repositories.References;

/// <summary>
/// Implementation of <see cref="IReferencesRepository"/> that provides persistence operations for Reference entities.
/// </summary>
/// <remarks>
/// This repository coordinates with <see cref="IUnitOfWork"/> to perform batch upsert operations on Skills,
/// Presence Definitions, and other reference data. It uses AutoMapper to transform DTOs into domain entities
/// and applies inactivation logic to mark records that no longer exist in the source system.
/// </remarks>
public class ReferencesRepository(IUnitOfWork uow,
                                  IMapper mapper) : IReferencesRepository
{
    /// <summary>
    /// Synchronizes the provided skills with the database, inserting new records and updating existing ones.
    /// </summary>
    /// <param name="skills">A read-only collection of skill DTOs retrieved from the Genesys API.</param>
    /// <param name="ct">A token to monitor for cancellation requests during the asynchronous operation.</param>
    /// <remarks>
    /// <para>
    /// This method performs a full synchronization using the "Upsert with Inactivation" pattern:
    /// <list type="number">
    /// <item>Maps the incoming DTOs to <see cref="Skill"/> entities using AutoMapper.</item>
    /// <item>Calls <see cref="IUnitOfWork.UpsertRangeAsync{TEntity}"/> to insert new skills or update existing ones based on their unique identifier (Id).</item>
    /// <item>Applies the inactivation callback (<c>State = Inactive</c>) to all skills in the database that are NOT present in the incoming collection.</item>
    /// <item>Commits all changes to the database in a single transaction via <see cref="IUnitOfWork.SaveChangesAsync"/>.</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the <paramref name="ct"/>.</exception>
    /// <exception cref="PersistenceException">Thrown when a database operation fails (e.g., constraint violations, connection issues).</exception>
    public async Task UpsertSkillsAsync(IReadOnlyCollection<SkillResponse> skills, CancellationToken ct)
    {
        List<Skill>? mappedEntities = mapper.Map<List<Skill>>(skills);

        await uow.UpsertRangeAsync(mappedEntities, s => s.State = State.Inactive, ct);

        await uow.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Synchronizes the provided presence definitions with the database, inserting new records and updating existing ones.
    /// </summary>
    /// <param name="presenceDefinitions">A read-only collection of presence definition DTOs retrieved from the Genesys API.</param>
    /// <param name="ct">A token to monitor for cancellation requests during the asynchronous operation.</param>
    /// <remarks>
    /// <para>
    /// This method performs a full synchronization using the "Upsert with Inactivation" pattern:
    /// <list type="number">
    /// <item>Maps the incoming DTOs to <see cref="PresenceDefinition"/> entities using AutoMapper.</item>
    /// <item>Calls <see cref="IUnitOfWork.UpsertRangeAsync{TEntity}"/> to insert new presence definitions or update existing ones based on their unique identifier (Id).</item>
    /// <item>Applies the inactivation callback (<c>Deactivated = true</c>) to all presence definitions in the database that are NOT present in the incoming collection.</item>
    /// <item>Commits all changes to the database in a single transaction via <see cref="IUnitOfWork.SaveChangesAsync"/>.</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the <paramref name="ct"/>.</exception>
    /// <exception cref="PersistenceException">Thrown when a database operation fails (e.g., constraint violations, connection issues).</exception>
    public async Task UpsertPresenceDefinitionsAsync(
        IReadOnlyCollection<PresenceDefinitionResponse> presenceDefinitions,
        CancellationToken ct)
    {
        List<PresenceDefinition>? mappedEntities = mapper.Map<List<PresenceDefinition>>(presenceDefinitions);

        await uow.UpsertRangeAsync(mappedEntities, pd => pd.Deactivated = true, ct);

        await uow.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Synchronizes the provided groups with the database, inserting new records and updating existing ones.
    /// </summary>
    /// <param name="groups">A read-only collection of group DTOs retrieved from the Genesys API.</param>
    /// <param name="ct">A token to monitor for cancellation requests during the asynchronous operation.</param>
    /// <remarks>
    /// <para>
    /// This method performs a full synchronization using the "Upsert with Inactivation" pattern:
    /// <list type="number">
    /// <item>Maps the incoming DTOs to <see cref="Group"/> entities using AutoMapper.</item>
    /// <item>Calls <see cref="IUnitOfWork.UpsertRangeAsync{TEntity}"/> to insert new groups or update existing ones based on their unique identifier (Id).</item>
    /// <item>Applies the inactivation callback (<c>State = Inactive</c>) to all groups in the database that are NOT present in the incoming collection.</item>
    /// <item>Commits all changes to the database in a single transaction via <see cref="IUnitOfWork.SaveChangesAsync"/>.</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the <paramref name="ct"/>.</exception>
    /// <exception cref="PersistenceException">Thrown when a database operation fails (e.g., constraint violations, connection issues).</exception>
    public async Task UpsertGroupsAsync(IReadOnlyCollection<GroupResponse> groups, CancellationToken ct)
    {
        List<Group>? mappedEntities = mapper.Map<List<Group>>(groups);

        await uow.UpsertRangeAsync(mappedEntities, g => g.State = State.Inactive, ct);

        await uow.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Synchronizes the provided wrapup codes with the database, inserting new records and updating existing ones.
    /// </summary>
    /// <param name="wrapupCodes">A read-only collection of wrapup code DTOs retrieved from the Genesys API.</param>
    /// <param name="ct">A token to monitor for cancellation requests during the asynchronous operation.</param>
    /// <remarks>
    /// <para>
    /// This method performs a full synchronization using the "Upsert without Inactivation" pattern:
    /// <list type="number">
    /// <item>Maps the incoming DTOs to <see cref="WrapupCode"/> entities using AutoMapper.</item>
    /// <item>Calls <see cref="IUnitOfWork.UpsertRangeAsync{TEntity}"/> to insert new wrapup codes or update existing ones based on their unique identifier (Id).</item>
    /// <item>No inactivation callback is applied—records not present in the incoming collection remain unchanged in the database.</item>
    /// <item>Commits all changes to the database in a single transaction via <see cref="IUnitOfWork.SaveChangesAsync"/>.</item>
    /// </list>
    /// </para>
    /// <para>
    /// Unlike other reference data methods, wrapup codes do not support inactivation logic because the Genesys API does not expose a deleted/inactive state for these entities.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the <paramref name="ct"/>.</exception>
    /// <exception cref="PersistenceException">Thrown when a database operation fails (e.g., constraint violations, connection issues).</exception>
    public async Task UpsertWrapupCodesAsync(IReadOnlyCollection<WrapupCodeResponse> wrapupCodes, CancellationToken ct)
    {
        List<WrapupCode>? mappedEntities = mapper.Map<List<WrapupCode>>(wrapupCodes);

        await uow.UpsertRangeAsync(mappedEntities, null, ct);

        await uow.SaveChangesAsync(ct);
    }
}
