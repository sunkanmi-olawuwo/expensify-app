using Expensify.Common.Domain;

namespace Expensify.Common.Application.Exceptions;

public sealed class ExpensifyException : Exception
{
    public ExpensifyException(string requestName, Error? error = default, Exception? innerException = default)
        : base("Application exception", innerException)
    {
        RequestName = requestName;
        Error = error;
    }

    public string RequestName { get; }

    public Error? Error { get; }
}
