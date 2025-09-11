namespace TooliRent.Services.DTOs.Loans;

// För utlåning baserat på en reservation
public class LoanCheckoutFromReservationDto
{
    public Guid ReservationId { get; set; }
    public DateTime? DueAtUtc { get; set; } // optional; om null -> res.EndUtc
}