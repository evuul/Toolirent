namespace TooliRent.Services.Exceptions;

public class ToolUnavailableException : Exception
{
    /// <summary>
    /// Id:n för verktyg som inte är tillgängliga.
    /// </summary>
    public IReadOnlyList<Guid> ToolIds { get; }

    public const string DefaultMessage = "Ett eller flera verktyg är inte tillgängliga i valt tidsintervall.";

    // Primär ctor för flera verktyg
    public ToolUnavailableException(IEnumerable<Guid> toolIds, string? message = null)
        : base(message ?? DefaultMessage)
    {
        ToolIds = toolIds?.Distinct().ToList() ?? new List<Guid>();
    }

    // Bekvämlighet för ett enda verktyg
    public ToolUnavailableException(Guid toolId, string? message = null)
        : this(new[] { toolId }, message) { }

    // Behåll en enkel ctor för bakåtkompabilitet
    public ToolUnavailableException(string message)
        : this(Array.Empty<Guid>(), message) { }
}