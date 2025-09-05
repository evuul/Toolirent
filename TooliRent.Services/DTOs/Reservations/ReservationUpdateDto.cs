public record ReservationUpdateDto(
    DateTime StartUtc,
    DateTime EndUtc,
    bool IsPaid,
    int Status // skickas som int, vi mappar till enum i service
);