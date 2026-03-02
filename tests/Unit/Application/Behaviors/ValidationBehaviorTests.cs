using System.Diagnostics.CodeAnalysis;

using Application.Behaviors;
using Application.Mediator;

using FluentValidation;

using Xunit;


namespace tests.Unit.Application.Behaviors;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_NoValidators_InvokesNextAndReturnsResponse()
    {
        ValidationBehavior<TestRequest, string> sut = new ValidationBehavior<TestRequest, string>([]);

        int nextCalls = 0;
        string result = await sut.Handle(new TestRequest("ok"),
                                         () =>
                                         {
                                             nextCalls++;

                                             return Task.FromResult("done");
                                         },
                                         CancellationToken.None);

        Assert.Equal("done", result);
        Assert.Equal(1, nextCalls);
    }

    [Fact]
    public async Task Handle_ValidRequest_InvokesNextAndReturnsResponse()
    {
        ValidationBehavior<TestRequest, string> sut =
            new ValidationBehavior<TestRequest, string>([new AlwaysValidValidator()]);

        int nextCalls = 0;
        string result = await sut.Handle(new TestRequest("ok"),
                                         () =>
                                         {
                                             nextCalls++;

                                             return Task.FromResult("done");
                                         },
                                         CancellationToken.None);

        Assert.Equal("done", result);
        Assert.Equal(1, nextCalls);
    }

    [Fact]
    public void Handle_InvalidRequest_ThrowsValidationException_WithAllFailures()
    {
        ValidationBehavior<TestRequest, string> sut =
            new ValidationBehavior<TestRequest, string>([new NameRequiredValidator(), new NameMinLengthValidator()]);

        ValidationException ex =
            Assert.Throws<ValidationException>(() => sut.Handle(new TestRequest(string.Empty),
                                                                () => Task.FromResult("should-not-run"),
                                                                CancellationToken.None)
                                                        .GetAwaiter()
                                                        .GetResult());

        List<string> errors = ex.Errors.Select(e => e.ErrorMessage)
                                .ToList();

        Assert.Contains("Name is required.", errors);
        Assert.Contains("Name must be at least 3 characters.", errors);
        Assert.True(errors.Count >= 2);
    }

    [Fact]
    public async Task Handle_DuplicateValidatorType_ExecutesTypeOnce()
    {
        CountingValidator.Reset();

        ValidationBehavior<TestRequest, string> sut =
            new ValidationBehavior<TestRequest, string>([new CountingValidator(), new CountingValidator()]);

        int nextCalls = 0;
        await sut.Handle(new TestRequest("ok"),
                         () =>
                         {
                             nextCalls++;

                             return Task.FromResult("done");
                         },
                         CancellationToken.None);

        Assert.Equal(1, CountingValidator.ValidateCalls);
        Assert.Equal(1, nextCalls);
    }

    #region ========== *** Private Methods *** ==========

    [ExcludeFromCodeCoverage]
    private sealed record TestRequest(string Name) : IRequest<string>;

    [ExcludeFromCodeCoverage]
    private sealed class AlwaysValidValidator : AbstractValidator<TestRequest>
    {
    }

    [ExcludeFromCodeCoverage]
    private sealed class NameRequiredValidator : AbstractValidator<TestRequest>
    {
        public NameRequiredValidator()
        {
            RuleFor(x => x.Name)
               .NotEmpty()
               .WithMessage("Name is required.");
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class NameMinLengthValidator : AbstractValidator<TestRequest>
    {
        public NameMinLengthValidator()
        {
            RuleFor(x => x.Name)
               .MinimumLength(3)
               .WithMessage("Name must be at least 3 characters.");
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class CountingValidator : AbstractValidator<TestRequest>
    {
        internal static int ValidateCalls { get; private set; }

        public CountingValidator()
        {
            RuleFor(x => x)
               .Custom((_, _) =>
                       {
                           ValidateCalls++;
                       });
        }

        internal static void Reset()
        {
            ValidateCalls = 0;
        }
    }

    #endregion
}
