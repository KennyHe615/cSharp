using System.Diagnostics.CodeAnalysis;

using Application;
using Application.Contracts.InternalApis.Recovery;
using Application.Features.Recovery;
using Application.Mediator;

using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

using SharedKernel.Lobs;

using Xunit;


namespace Tests.Integration.Application;

public sealed class ApplicationValidationPipelineIntegrationTests
{
    [Fact]
    public async Task MediatorSend_RecoveryCommandWithoutIntervalAndJobId_ThrowsValidationException()
    {
        ServiceCollection services = [];

        services.AddApplication();
        services
           .AddScoped<IRequestHandler<CreateRecoveryRequestCommand, CreateRecoveryRequestResponse>,
                StubRecoveryHandler>();

        await using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        ISimpleMediator mediator = scope.ServiceProvider.GetRequiredService<ISimpleMediator>();

        CreateRecoveryRequestCommand invalid =
            new CreateRecoveryRequestCommand(new LobName("CRC"),
                                             RecoveryCategory.UsersDetails,
                                             null,
                                             null);

        await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(invalid, CancellationToken.None));
    }

    [ExcludeFromCodeCoverage]
    private sealed class
        StubRecoveryHandler : IRequestHandler<CreateRecoveryRequestCommand, CreateRecoveryRequestResponse>
    {
        public Task<CreateRecoveryRequestResponse> Handle(CreateRecoveryRequestCommand request,
                                                          CancellationToken ct = default)
        {
            return Task.FromResult(new CreateRecoveryRequestResponse(true, "ok", new {}));
        }
    }
}
