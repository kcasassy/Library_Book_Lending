namespace Library_Book_Lending.DTOs;

public class BookReservationResponse
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public DateTime? ReservedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
