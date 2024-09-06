namespace Framework.Exceptions;

public class ConcurrencyException : Exception
{
    internal ConcurrencyException(string message) : base(message)
    {
    }
}