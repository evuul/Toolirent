// TooliRent.Services/DTOs/Loans/LoanCheckoutDto.cs
namespace TooliRent.Services.DTOs.Loans;

/// <summary>
/// Batch-post för medlemmen.
/// - Via reservation: ange bara ReservationId (Tool/Member härleds, DueAtUtc valfri; default = reservationens EndUtc)
/// - Direktlån: ange ToolId + DueAtUtc. MemberId kommer från JWT, inte här.
/// </summary>
public record LoanCheckoutDto(
    Guid? ReservationId,        // om satt: hämta hela bokningen
    IEnumerable<Guid>? ToolIds, // annars: direktlån av dessa verktyg
    DateTime? DueAtUtc
);