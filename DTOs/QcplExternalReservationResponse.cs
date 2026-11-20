namespace Library_Book_Lending.DTOs;

public class QcplExternalReservationResponse
{
    public int Id { get; set; }
    public string ExternalReferenceNumber { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string BookTitle { get; set; } = string.Empty;
    public string ReservationStatus { get; set; } = string.Empty;
    public DateTime ReservationDate { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

