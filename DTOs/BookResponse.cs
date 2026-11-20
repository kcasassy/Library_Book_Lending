namespace Library_Book_Lending.DTOs;

public class BookResponse
{
    public int Id { get; set; }

    public string ISBN { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public int? PublishedYear { get; set; }

    public int TotalCopies { get; set; }

    public int AvailableCopies { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
