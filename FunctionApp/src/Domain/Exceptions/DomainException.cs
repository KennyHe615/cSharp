namespace Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException()
    {
    }

    protected DomainException(string message) : base(message)
    {
    }

    protected DomainException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}

public sealed class InvariantViolationException(string message) : DomainException(message);
