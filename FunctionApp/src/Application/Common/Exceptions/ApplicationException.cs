namespace Application.Common.Exceptions;

public abstract class ApplicationException : Exception
{
    protected ApplicationException()
    {
    }

    protected ApplicationException(string message) : base(message)
    {
    }

    protected ApplicationException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}

public sealed class AuthorizationConfigurationException(string message,
                                                        Exception? inner = null) : ApplicationException(message, inner);
