namespace TooliRent.Core.Models;

public class Member : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? IdentityUserId { get; set; }
    public int TokenVersion { get; set; } = 0;

    
    // Relationer
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}