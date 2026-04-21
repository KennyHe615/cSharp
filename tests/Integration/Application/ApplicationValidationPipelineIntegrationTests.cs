using System.Diagnostics.CodeAnalysis;

using Application;
using Application.Abstractions.Recovery;
using Application.Contracts.InternalApis.Recovery;
using Application.Features.Recovery;
using Application.Mediator;

using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

using SharedKernel.Lobs;

using Xunit;


namespace tests.Integration.Application;

public sealed class ApplicationValidationPipelineIntegrationTests
{
    [Fact]
    public async Task MediatorSend_RecoveryCommandWithoutIntervalAndJobId_ThrowsValidationException()
    {
        ServiceCollection services = [];

        services.AddApplication();
        services.AddScoped<IRecoveryIntervalPolicy, StubRecoveryIntervalPolicy>();
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

    #region ========== *** Private Section *** ==========

    [ExcludeFromCodeCoverage]
    private sealed class
                    StubRecoveryHandler : IRequestHandler<CreateRecoveryRequestCommand, CreateRecoveryRequestResponse>
    {
        public Task<CreateRecoveryRequestResponse> Handle(CreateRecoveryRequestCommand request,
                                                          CancellationToken ct = default)
        {
            CreateRecoveryRequestResponse response =
                            new CreateRecoveryRequestResponse(true,
                                                              "ok",
                                                              new CreateRecoveryRequestResponseData(Guid.NewGuid(),
                                                                  "Created",
                                                                  request.Lob.ToString(),
                                                                  request.Category.ToString(),
                                                                  request.Interval,
                                                                  request.GenesysJobId));

            return Task.FromResult(response);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubRecoveryIntervalPolicy : IRecoveryIntervalPolicy
    {
        public int HistoricalDataLimitDays => 558;

        public int FutureSkewDays => 1;

        public bool IsStartWithinRetention(DateTimeOffset start)
        {
            return true;
        }

        public bool IsEndWithinFutureSkew(DateTimeOffset end)
        {
            return true;
        }
    }

    #endregion
}
