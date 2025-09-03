namespace TooliRent.Core.Enums;

// Status för ett lån
public enum LoanStatus
{
    Open = 0,
    Returned = 1,
    Late = 2
}

// Status för ett verktyg (kan vara bra längre fram)
public enum ToolStatus
{
    Available = 0,
    Reserved = 1,
    Loaned = 2,
    Maintenance = 3
}

public enum ReservationStatus
{
    Active = 0,     // bokad, ej påbörjad
    Cancelled = 1,  // avbokad
    Completed = 2   // avslutad (t.ex. efter återlämning via Loan)
}