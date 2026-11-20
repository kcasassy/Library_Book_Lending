using Library_Book_Lending.Models;
using System.ComponentModel.DataAnnotations;

namespace Library_Book_Lending.Models;

public class BookReservation
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int BookId { get; set; }

    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = ReservationStatuses.Pending;

    public User? User { get; set; }

    public Book? Book { get; set; }
}
