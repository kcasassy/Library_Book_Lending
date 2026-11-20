using System.ComponentModel.DataAnnotations;

namespace Library_Book_Lending.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; } = UserRole.Member;

    [MaxLength(50)]
    public string? MemberNumber { get; set; }

    public DateTime? MembershipExpiry { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BookReservation> BookReservations { get; set; } = new List<BookReservation>();

    public ICollection<Borrowing> Borrowings { get; set; } = new List<Borrowing>();

    public ICollection<SeatReservation> SeatReservations { get; set; } = new List<SeatReservation>();

    public ICollection<ReadingSpaceReservation> ReadingSpaceReservations { get; set; } = new List<ReadingSpaceReservation>();
}
