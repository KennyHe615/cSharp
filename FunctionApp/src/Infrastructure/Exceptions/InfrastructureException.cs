namespace Infrastructure.Exceptions;

public abstract class InfrastructureException : Exception
{
    protected InfrastructureException()
    {
    }

    protected InfrastructureException(string message) : base(message)
    {
    }

    protected InfrastructureException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}
