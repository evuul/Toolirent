namespace TooliRent.Services.Exceptions;

/// <summary>
/// Kastas när en batch-reservation inte kan genomföras eftersom en eller flera
/// av önskade verktyg inte är tillgängliga i angivet tidsfönster.
/// Innehåller listor över vilka som var tillgängliga/otillgängliga.
/// </summary>
public class BatchReservationFailedException : Exception
{
    public IReadOnlyList<Guid> AvailableToolIds { get; }
    public IReadOnlyList<Guid> UnavailableToolIds { get; }

    public BatchReservationFailedException(
        IEnumerable<Guid> availableToolIds,
        IEnumerable<Guid> unavailableToolIds,
        string? message = null)
        : base(message ?? "Batch-reservationen kunde inte genomföras eftersom ett eller flera verktyg inte är tillgängliga.")
    {
        AvailableToolIds = availableToolIds?.ToList() ?? new List<Guid>();
        UnavailableToolIds = unavailableToolIds?.ToList() ?? new List<Guid>();
    }
}