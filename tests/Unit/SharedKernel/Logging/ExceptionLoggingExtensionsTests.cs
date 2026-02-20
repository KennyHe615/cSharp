using System.Diagnostics;

using SharedKernel.Logging;

using Xunit;


namespace tests.Unit.SharedKernel.Logging;

public sealed class ExceptionLoggingExtensionsTests
{
    #region ToSummary

    [Fact]
    public void ToSummary_IncludesRootCause_WhenNested()
    {
        InvalidOperationException root = new InvalidOperationException("root cause");
        ApplicationException top = new ApplicationException("top level", root);

        string summary = top.ToSummary();

        Assert.Contains("ApplicationException", summary);
        Assert.Contains("Root:", summary);
        Assert.Contains("InvalidOperationException", summary);
        Assert.Contains("root cause", summary);
    }

    [Fact]
    public void ToSummary_TruncatesLongMessages()
    {
        string longMessage = new string('x', 2000);
        Exception ex = new Exception(longMessage);

        string summary = ex.ToSummary();

        Assert.Contains("...", summary);
        Assert.True(summary.Length < 1200);// sanity bound, avoid overly large summary
    }

    [Fact]
    public void ToSummary_Throws_WhenExceptionIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ExceptionLoggingExtensions.ToSummary(null!));
    }

    #endregion

    #region ToLogScope

    [Fact]
    public void ToLogScope_UsesLowercaseDefaultPrefix()
    {
        Exception ex = new Exception("boom");

        IReadOnlyDictionary<string, object?> scope = ex.ToLogScope();

        Assert.True(scope.ContainsKey("exception.type"));
        Assert.True(scope.ContainsKey("exception.message"));
        Assert.True(scope.ContainsKey("exception.hresult"));
    }

    [Fact]
    public void ToLogScope_Throws_WhenPrefixInvalid()
    {
        Exception ex = new Exception("boom");

        Assert.Throws<ArgumentException>(() => ex.ToLogScope(""));
        Assert.Throws<ArgumentException>(() => ex.ToLogScope("   "));
    }

    [Fact]
    public void ToLogScope_Throws_WhenExceptionIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ExceptionLoggingExtensions.ToLogScope(null!));
    }

    [Fact]
    public void ToLogScope_IncludesSourceAndActivityId_WhenAvailable()
    {
        using Activity activity = new Activity("unit-test");
        activity.Start();

        Exception ex = new Exception("boom") { Source = "UnitTests.Source" };

        IReadOnlyDictionary<string, object?> scope = ex.ToLogScope();

        Assert.Equal("UnitTests.Source", scope["exception.source"]);
        Assert.True(scope.ContainsKey("exception.activity_id"));
        Assert.False(string.IsNullOrWhiteSpace(scope["exception.activity_id"]?.ToString()));
    }

    [Fact]
    public void ToLogScope_IncludesRootFields_WhenRootDiffers()
    {
        Exception ex = new Exception("outer", new InvalidOperationException("inner"));

        IReadOnlyDictionary<string, object?> scope = ex.ToLogScope();

        Assert.True(scope.ContainsKey("exception.root_type"));
        Assert.True(scope.ContainsKey("exception.root_message"));
        Assert.Equal("inner", scope["exception.root_message"]);
    }

    #endregion

    #region GetRootCause

    [Fact]
    public void GetRootCause_ReturnsDeepestInnerException()
    {
        InvalidOperationException root = new InvalidOperationException("root");
        ArgumentException middle = new ArgumentException("middle", root);
        Exception top = new Exception("top", middle);

        Exception actual = top.GetRootCause();

        Assert.Same(root, actual);
    }

    [Fact]
    public void GetRootCause_ForAggregate_ReturnsPrimaryFlattenedBranchRoot()
    {
        InvalidOperationException firstRoot = new InvalidOperationException("first root");
        Exception first = new Exception("first wrapper", firstRoot);
        Exception second = new Exception("second");
        AggregateException aggregate = new AggregateException(first, second);

        Exception actual = aggregate.GetRootCause();

        Assert.Same(firstRoot, actual);
    }

    [Fact]
    public void GetRootCause_Throws_WhenExceptionIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ExceptionLoggingExtensions.GetRootCause(null!));
    }

    [Fact]
    public void GetRootCause_ForEmptyAggregate_ReturnsAggregateItself()
    {
        AggregateException aggregate = new AggregateException();

        Exception root = aggregate.GetRootCause();

        Assert.Same(aggregate, root);
    }

    #endregion

    #region ToReadableString

    [Fact]
    public void ToReadableString_ExcludesStack_WhenIncludeStackTraceFalse()
    {
        Exception ex = CreateThrownException();

        string text = ex.ToReadableString(includeStackTrace: false);

        Assert.DoesNotContain("Stack:", text);
        Assert.Contains("Type:", text);
        Assert.Contains("Message:", text);
    }

    [Fact]
    public void ToReadableString_ShowsDepthTruncation_WhenVeryDeep()
    {
        Exception ex = BuildDeepExceptionChain(depth: 20);

        string text = ex.ToReadableString();

        Assert.Contains("<max depth reached>", text);
    }

    [Fact]
    public void ToReadableString_TruncatesData_WhenTooManyDataItems()
    {
        Exception ex = new Exception("data-heavy");
        for (int i = 0; i < 200; i++)
        {
            ex.Data[$"k{i}"] = $"v{i}";
        }

        string text = ex.ToReadableString(includeStackTrace: false);

        Assert.Contains("Data:", text);
        Assert.Contains("<truncated>", text);
    }

    [Fact]
    public void ToReadableString_Aggregate_DoesNotDuplicateViaInnerSection()
    {
        Exception a = new Exception("A");
        Exception b = new Exception("B");
        AggregateException aggregate = new AggregateException(a, b);

        string text = aggregate.ToReadableString(includeStackTrace: false);

        Assert.Contains("Aggregate (2):", text);
        Assert.DoesNotContain("Inner:", text);// should use aggregate branch listing, not duplicate inner section
        Assert.Contains("Message: A", text);
        Assert.Contains("Message: B", text);
    }

    [Fact]
    public void ToReadableString_Throws_WhenExceptionIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ExceptionLoggingExtensions.ToReadableString(null!));
    }

    [Fact]
    public void ToReadableString_IncludesHResultAndSource_WhenPresent()
    {
        Exception ex = new HResultException("with hresult", -1) { Source = "My.Source" };

        string text = ex.ToReadableString(includeStackTrace: false);

        Assert.Contains("Source: My.Source", text);
        Assert.Contains("HResult: -1", text);
    }

    [Fact]
    public void ToReadableString_DoesNotShowDataTruncated_WhenBelowLimit()
    {
        Exception ex = new Exception("small-data");
        ex.Data["k1"] = "v1";

        string text = ex.ToReadableString(includeStackTrace: false);

        Assert.Contains("Data:", text);
        Assert.Contains("- k1: v1", text);
        Assert.DoesNotContain("<truncated>", text);
    }

    [Fact]
    public void ToReadableString_ShowsStackTruncated_WhenStackExceedsLimit()
    {
        Exception ex = new FakeStackException("stack-over-limit", BuildStackLines(1000));// MaxStackLines = 800

        string text = ex.ToReadableString(includeStackTrace: true);

        Assert.Contains("Stack:", text);
        Assert.Contains("<stack truncated>", text);
    }

    #endregion

    #region ========== *** Private *** ==========

    private static Exception CreateThrownException()
    {
        try
        {
            throw new InvalidOperationException("thrown");
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private static Exception BuildDeepExceptionChain(int depth)
    {
        Exception current = new Exception("leaf");

        for (int i = 0; i < depth; i++)
        {
            current = new Exception($"level-{i}", current);
        }

        return current;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private sealed class HResultException : Exception
    {
        public HResultException(string message, int hresult) : base(message)
        {
            HResult = hresult;
        }
    }

    private static string BuildStackLines(int count)
    {
        return string.Join(Environment.NewLine, Enumerable.Range(0, count).Select(i => $"at Method{i}()"));
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private sealed class FakeStackException(string message,
                                            string stackTrace) : Exception(message)
    {
        public override string StackTrace => stackTrace;
    }

    #endregion
}
