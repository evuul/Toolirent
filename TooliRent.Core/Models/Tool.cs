using TooliRent.Core.Enums;

namespace TooliRent.Core.Models;

public class Tool : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Pris per dag (sätt gärna decimal-precision i EF: decimal(18,2))
    public decimal RentalPricePerDay { get; set; }

    // Är verktyget i drift? (separerat från "tillgängligt just nu")
    public bool IsActive { get; set; } = true;

    // (valfritt) Statusfält – om du inte vill beräkna tillgänglighet dynamiskt
    public ToolStatus Status { get; set; } = ToolStatus.Available;

    // FK → kategori (Guid för att matcha BaseEntity)
    public Guid CategoryId { get; set; }
    public ToolCategory Category { get; set; } = null!;

    // Navigationer
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}