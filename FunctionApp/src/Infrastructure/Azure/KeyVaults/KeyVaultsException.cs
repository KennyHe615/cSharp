using Infrastructure.Exceptions;


namespace Infrastructure.Azure.KeyVaults;

/// <summary>
/// Represents errors that occur while configuring or interacting with Azure Key Vault\-related infrastructure.
/// </summary>
/// <remarks>
/// This exception type is intended to normalize Key Vault\-specific failures (e\.g\. invalid configuration,
/// client creation issues, or underlying SDK errors) behind a domain\-specific exception that derives from
/// <see cref="InfrastructureException"/>.
/// </remarks>
public sealed class KeyVaultsException : InfrastructureException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyVaultsException"/> class.
    /// </summary>
    public KeyVaultsException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyVaultsException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public KeyVaultsException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyVaultsException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="inner">The exception that caused the current exception.</param>
    public KeyVaultsException(string message, Exception inner) : base(message, inner)
    {
    }
}
