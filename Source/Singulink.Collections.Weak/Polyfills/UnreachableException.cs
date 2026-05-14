#if !NET

namespace System.Diagnostics;

internal sealed class UnreachableException : InvalidOperationException
{
    public UnreachableException()
    {
    }

    public UnreachableException(string? message) : base(message)
    {
    }

    public UnreachableException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

#endif
