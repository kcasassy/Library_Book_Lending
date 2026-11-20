using System.ComponentModel.DataAnnotations;

namespace Library_Book_Lending.Models;

public class Book
{
    public int Id { get; set; }

    [Required]
    [MaxLength(30)]
    public string ISBN { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Author { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    public int? PublishedYear { get; set; }

    [Range(0, 10000)]
    public int TotalCopies { get; set; }

    [Range(0, 10000)]
    public int AvailableCopies { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BookReservation> Reservations { get; set; } = new List<BookReservation>();

    public ICollection<Borrowing> Borrowings { get; set; } = new List<Borrowing>();
}
