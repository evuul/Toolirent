public record ReservationCreateDto(
    Guid ToolId,
    Guid MemberId,
    DateTime StartUtc,
    DateTime EndUtc
);