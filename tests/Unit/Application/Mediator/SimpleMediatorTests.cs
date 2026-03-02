using System.Diagnostics.CodeAnalysis;

using Application.Mediator;

using Microsoft.Extensions.DependencyInjection;

using Xunit;


namespace tests.Unit.Application.Mediator;

public sealed class SimpleMediatorTests
{
    [Fact]
    public async Task Send_NullRequest_ThrowsArgumentNullException()
    {
        ServiceCollection services = new ServiceCollection();
        ISimpleMediator sut = CreateMediator(services);

        IRequest<string> request = null!;

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.Send(request, CancellationToken.None));
    }

    [Fact]
    public async Task Send_HandlerOnly_ReturnsHandlerResponse()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddScoped<IRequestHandler<TestRequest, string>, EchoHandler>();

        ISimpleMediator sut = CreateMediator(services);

        string result = await sut.Send(new TestRequest("abc"), CancellationToken.None);

        Assert.Equal("handled:abc", result);
    }

    [Fact]
    public async Task Send_WithTwoBehaviors_ExecutesInPipelineOrder()
    {
        List<string> trace = [];
        ServiceCollection services = new ServiceCollection();

        services.AddSingleton(trace);
        services.AddScoped<IRequestHandler<TestRequest, string>, EchoHandler>();
        services.AddScoped<IPipelineBehavior<TestRequest, string>, FirstBehavior>();
        services.AddScoped<IPipelineBehavior<TestRequest, string>, SecondBehavior>();

        ISimpleMediator sut = CreateMediator(services);

        string result = await sut.Send(new TestRequest("abc"), CancellationToken.None);

        Assert.Equal("handled:abc", result);
        Assert.Equal(["first-before", "second-before", "second-after", "first-after"], trace);
    }

    [Fact]
    public async Task Send_MissingHandler_ThrowsInvalidOperationException()
    {
        ServiceCollection services = new ServiceCollection();
        ISimpleMediator sut = CreateMediator(services);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Send(new TestRequest("abc"),
                                                                               CancellationToken.None));

        Assert.Contains("No service for type", ex.Message);
    }

    [Fact]
    public async Task Send_HandlerThrows_UnwrapsInnerException()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddScoped<IRequestHandler<TestRequest, string>, ThrowingHandler>();

        ISimpleMediator sut = CreateMediator(services);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Send(new TestRequest("abc"),
                                                                               CancellationToken.None));

        Assert.Equal("handler-boom", ex.Message);
    }

    [Fact]
    public async Task Send_BehaviorThrows_UnwrapsInnerException()
    {
        ServiceCollection services = new ServiceCollection();

        services.AddScoped<IRequestHandler<TestRequest, string>, EchoHandler>();
        services.AddScoped<IPipelineBehavior<TestRequest, string>, ThrowingBehavior>();

        ISimpleMediator sut = CreateMediator(services);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Send(new TestRequest("abc"),
                                                                               CancellationToken.None));

        Assert.Equal("behavior-boom", ex.Message);
    }

    [Fact]
    public async Task Send_HandlerWithExplicitInterfaceOnly_ThrowsMissingHandle()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddScoped<IRequestHandler<TestRequest, string>, ExplicitOnlyHandler>();

        ISimpleMediator sut = CreateMediator(services);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Send(new TestRequest("abc"),
                                                                               CancellationToken.None));

        Assert.Contains("does not have a Handle method", ex.Message);
    }

    [Fact]
    public async Task Send_BehaviorWithExplicitInterfaceOnly_ThrowsMissingHandle()
    {
        ServiceCollection services = new ServiceCollection();

        services.AddScoped<IRequestHandler<TestRequest, string>, EchoHandler>();
        services.AddScoped<IPipelineBehavior<TestRequest, string>, ExplicitOnlyBehavior>();

        ISimpleMediator sut = CreateMediator(services);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Send(new TestRequest("abc"),
                                                                               CancellationToken.None));

        Assert.Contains("does not have a Handle method", ex.Message);
    }

    [Fact]
    public async Task Send_HandlerPublicHandleWrongReturnType_ThrowsInvalidResult()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddScoped<IRequestHandler<TestRequest, string>, WrongReturnTypeHandler>();

        ISimpleMediator sut = CreateMediator(services);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Send(new TestRequest("abc"),
                                                                               CancellationToken.None));

        Assert.Contains("returned an invalid result", ex.Message);
    }

    [ExcludeFromCodeCoverage]
    private static ISimpleMediator CreateMediator(IServiceCollection services)
    {
        ServiceProvider provider = services.BuildServiceProvider();

        Type mediatorType = typeof(ISimpleMediator).Assembly.GetType("Application.Mediator.SimpleMediator", true)!;

        return (ISimpleMediator)Activator.CreateInstance(mediatorType, provider)!;
    }

    [ExcludeFromCodeCoverage]
    private sealed record TestRequest(string Value) : IRequest<string>;

    [ExcludeFromCodeCoverage]
    private sealed class EchoHandler : IRequestHandler<TestRequest, string>
    {
        public Task<string> Handle(TestRequest request, CancellationToken ct = default)
        {
            return Task.FromResult($"handled:{request.Value}");
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingHandler : IRequestHandler<TestRequest, string>
    {
        public Task<string> Handle(TestRequest request, CancellationToken ct = default)
        {
            throw new InvalidOperationException("handler-boom");
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ExplicitOnlyHandler : IRequestHandler<TestRequest, string>
    {
        Task<string> IRequestHandler<TestRequest, string>.Handle(TestRequest request, CancellationToken ct)
        {
            return Task.FromResult("ok");
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class WrongReturnTypeHandler : IRequestHandler<TestRequest, string>
    {
        // Picked by reflection, but wrong return type for mediator cast path.
        public Task<int> Handle(TestRequest request, CancellationToken ct = default)
        {
            return Task.FromResult(42);
        }

        Task<string> IRequestHandler<TestRequest, string>.Handle(TestRequest request, CancellationToken ct)
        {
            return Task.FromResult("ok");
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class FirstBehavior(List<string> trace) : IPipelineBehavior<TestRequest, string>
    {
        public async Task<string> Handle(TestRequest request,
                                         RequestHandlerDelegate<string> next,
                                         CancellationToken ct = default)
        {
            trace.Add("first-before");
            string result = await next()
               .ConfigureAwait(false);
            trace.Add("first-after");

            return result;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class SecondBehavior(List<string> trace) : IPipelineBehavior<TestRequest, string>
    {
        public async Task<string> Handle(TestRequest request,
                                         RequestHandlerDelegate<string> next,
                                         CancellationToken ct = default)
        {
            trace.Add("second-before");
            string result = await next()
               .ConfigureAwait(false);
            trace.Add("second-after");

            return result;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingBehavior : IPipelineBehavior<TestRequest, string>
    {
        public Task<string> Handle(TestRequest request,
                                   RequestHandlerDelegate<string> next,
                                   CancellationToken ct = default)
        {
            throw new InvalidOperationException("behavior-boom");
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ExplicitOnlyBehavior : IPipelineBehavior<TestRequest, string>
    {
        Task<string> IPipelineBehavior<TestRequest, string>.Handle(TestRequest request,
                                                                   RequestHandlerDelegate<string> next,
                                                                   CancellationToken ct)
        {
            return next();
        }
    }
}
