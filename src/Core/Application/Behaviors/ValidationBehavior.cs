// using Application.Common.Mediator;
//
// using FluentValidation;
// using FluentValidation.Results;
//
//
// namespace Application.Behaviors;
//
// public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
//     : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
// {
//     public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
//     {
//         // Deduplicate validators by type to prevent multiple executions
//         List<IValidator<TRequest>> uniqueValidators =
//             validators.GroupBy(v => v.GetType()).Select(g => g.First()).ToList();
//
//         if (uniqueValidators.Count <= 0) return await next();
//
//         ValidationContext<TRequest> context = new(request);
//
//         List<ValidationFailure> failures = [];
//
//         foreach (IValidator<TRequest> validator in uniqueValidators)
//         {
//             ValidationResult result = await validator.ValidateAsync(context, ct);
//             if (result.Errors.Count > 0)
//             {
//                 failures.AddRange(result.Errors);
//             }
//         }
//
//         if (failures.Count != 0)
//         {
//             throw new ValidationException(failures);
//         }
//
//         return await next();
//     }
// }
