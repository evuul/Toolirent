namespace TooliRent.Services.DTOs.Loans;

/// <summary>
/// Batch-post för admin.
/// - Via reservation: ange endast ReservationId (Member härleds från reservationen).
/// - Direktlån: ange ToolId + MemberId + DueAtUtc (MemberId är obligatoriskt i detta fall).
/// </summary>
public record AdminLoanCheckoutDto(
    Guid? ReservationId,
    Guid? ToolId,
    Guid? MemberId,   // görs nullbart eftersom ReservationId-scenariot inte kräver det
    DateTime? DueAtUtc
);