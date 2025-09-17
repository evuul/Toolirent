namespace TooliRent.Services.DTOs.Reservations;

// Inkommande payload för att boka flera verktyg i ett fönster.
public record ReservationBatchCreateDto(
    IEnumerable<Guid> ToolIds,
    DateTime StartUtc,
    DateTime EndUtc,
    Guid? MemberId // skrivs över av controllern
);