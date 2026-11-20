namespace Library_Book_Lending.DTOs;

public class BorrowingResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public int? ProcessedById { get; set; }
    public DateTime BorrowedAt { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
