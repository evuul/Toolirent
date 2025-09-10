namespace TooliRent.Services.Exceptions;

public class ToolUnavailableException : Exception
{
    public ToolUnavailableException(string message) : base(message) { }
}