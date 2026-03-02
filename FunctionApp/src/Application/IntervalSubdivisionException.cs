namespace Application.Common.Exceptions;

public sealed class IntervalSubdivisionException : ApplicationException
{
    public IntervalSubdivisionException(string message) : base(message)
    {
    }

    public IntervalSubdivisionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
