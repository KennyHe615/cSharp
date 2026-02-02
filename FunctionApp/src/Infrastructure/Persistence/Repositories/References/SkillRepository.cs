using Application.Dtos.References;
using Application.Enums;

using Infrastructure.Persistence.Entities.References;

using Microsoft.EntityFrameworkCore;

using Shared.Extensions;
using Shared.Providers;


namespace Infrastructure.Persistence.Repositories.References;

/// <summary>
/// Implementation of <see cref="ISkillRepository"/> for managing <see cref="Skill"/> entities in the database.
/// </summary>
/// <param name="dbContext">The database context for persistence operations.</param>
/// <param name="dateTimeProvider">The provider for date and time conversions and operations.</param>
public class SkillRepository(FunctionAppDbContext.FunctionAppDbContext dbContext,
                             IDateTimeProvider dateTimeProvider) : ISkillRepository
{
    /// <inheritdoc />
    /// <exception cref="DbConcurrencyException">Thrown when a concurrency conflict occurs while saving changes.</exception>
    /// <exception cref="EntityOperationException">Thrown when a database update or unexpected error occurs.</exception>
    public async Task UpsertSkillAsync(IReadOnlyCollection<SkillResponse> skills, CancellationToken ct)
    {
        try
        {
            // 1. Map API DTOs by ID for quick lookup
            Dictionary<Guid, SkillResponse> apiByIdToDto = skills.ToDictionary(s => s.Id);

            // 2. Fetch existing entities from the database
            List<Skill> dbSkills = await dbContext.Set<Skill>().ToListAsync(ct).ConfigureAwait(false);
            Dictionary<Guid, Skill> dbByIdToEntity = dbSkills.ToDictionary(s => s.Id);

            // 3. Determine the union of IDs to process additions, updates, and inactivations
            HashSet<Guid> allIds = [.. apiByIdToDto.Keys, .. dbByIdToEntity.Keys];

            foreach (Guid id in allIds)
            {
                bool apiHas = apiByIdToDto.TryGetValue(id, out SkillResponse? dto);
                bool dbHas = dbByIdToEntity.TryGetValue(id, out Skill? entity);

                // Prepare common values from DTO if available
                string? name = dto?.Name.Truncate(255);
                DateTimeOffset? dateModified = dateTimeProvider.ConvertToEst(dto?.DateModified);

                switch (apiHas)
                {
                    // Case: Present in API but not in DB => Add new entity
                    case true when !dbHas:
                    {
                        dbContext.Set<Skill>()
                                 .Add(new Skill
                                      {
                                          Id = dto!.Id,
                                          Name = name,
                                          Version = dto.Version,
                                          State = dto.State,
                                          DateModified = dateModified
                                      });

                        break;
                    }
                    // Scenario 2: API no, DB yes => Inactivate
                    case false when dbHas:
                    {
                        entity!.State = State.Inactive;

                        break;
                    }
                    // Scenario 3: API yes, DB yes => Update fields
                    // (If values are identical, EF would keep it Unchanged; your interceptor's IsModified flag
                    // is what forces the AppUpdatedAt UPDATE.)
                    case true when dbHas:
                    {
                        entity!.Name = name;
                        entity.Version = dto!.Version;
                        entity.State = dto.State;
                        entity.DateModified = dateModified;

                        break;
                    }
                }
            }

            // 4. Persist all changes in a single transaction
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new DbConcurrencyException("A concurrency conflict occurred while upserting skills.", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new EntityOperationException("Failed to save skill changes to the database.", ex, nameof(Skill));
        }
        catch (Exception ex) when (ex is not PersistenceException)
        {
            throw new EntityOperationException("An unexpected error occurred while upserting skills.",
                                               ex,
                                               nameof(Skill));
        }
    }
}
