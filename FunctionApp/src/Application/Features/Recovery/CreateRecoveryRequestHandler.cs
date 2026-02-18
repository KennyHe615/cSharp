using Application.Common.Mediator;


namespace Application.Features.Recovery;

public class CreateRecoveryRequestHandler : IRequestHandler<CreateRecoveryRequestCommand, CreateRecoveryRequestResponse>
{
    public async Task<CreateRecoveryRequestResponse> Handle(CreateRecoveryRequestCommand request, CancellationToken ct)
    {
        // Business logic here
        await Task.CompletedTask; // Remove when adding real logic

        return new CreateRecoveryRequestResponse(true,
                                                 "Recovery request submitted successfully",
                                                 new
                                                 {
                                                     Lob = request.Lob.ToString(),
                                                     RecoveryCategory = request.Category.HasValue
                                                         ? request.Category.ToString()?.Replace("Recovery", "")
                                                         : "Unknown",
                                                     request.Interval,
                                                     request.JobId
                                                 });
    }
}
