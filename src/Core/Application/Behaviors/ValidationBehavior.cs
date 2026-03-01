using Application.Mediator;

using FluentValidation;
using FluentValidation.Results;


namespace Application.Behaviors;

/// <summary>
/// Mediator pipeline behavior that executes all registered FluentValidation validators
/// for the current request and blocks handler execution when validation fails.
/// </summary>
/// <typeparam name="TRequest">Request type.</typeparam>
/// <typeparam name="TResponse">Response type.</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // Deduplicate validators by type to prevent multiple executions
        List<IValidator<TRequest>> uniqueValidators = validators.GroupBy(v => v.GetType())
                                                                .Select(g => g.First())
                                                                .ToList();

        if (uniqueValidators.Count == 0)
        {
            return await next()
               .ConfigureAwait(false);
        }

        ValidationContext<TRequest> context = new ValidationContext<TRequest>(request);

        List<ValidationFailure> failures = [];

        foreach (IValidator<TRequest> validator in uniqueValidators)
        {
            ValidationResult result = await validator.ValidateAsync(context, ct)
                                                     .ConfigureAwait(false);

            if (result.Errors.Count > 0) failures.AddRange(result.Errors);
        }

        if (failures.Count > 0) throw new ValidationException(failures);

        return await next()
           .ConfigureAwait(false);
    }
}
