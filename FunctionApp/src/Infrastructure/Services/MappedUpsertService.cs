using Application.Shared.Repositories;

using AutoMapper;

using Infrastructure.Persistence;


namespace Infrastructure.Services;

public sealed class MappedUpsertService(IUnitOfWork unitOfWork,
                                        IMapper mapper) : IMappedUpsertService
{
    public async Task UpsertAsync<TSource, TEntity>(IReadOnlyList<TSource> dtos, CancellationToken cancellationToken)
        where TEntity : class
    {
        if (dtos.Count == 0) return;

        List<TEntity> entities;

        try
        {
            entities = mapper.Map<List<TEntity>>(dtos);
        }
        catch (AutoMapperMappingException ex)
        {
            throw new EntityMappingException($"Failed to map from {typeof(TSource).Name} to {typeof(TEntity).Name}.",
                                             ex,
                                             typeof(TSource),
                                             typeof(TEntity));
        }
        catch (Exception ex)
        {
            throw new EntityMappingException(
                $"An unexpected error occurred while mapping from {typeof(TSource).Name} to {typeof(TEntity).Name}.",
                ex,
                typeof(TSource),
                typeof(TEntity));
        }

        await unitOfWork.UpsertRangeAsync(entities, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
