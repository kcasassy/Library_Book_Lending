
using System.ComponentModel.DataAnnotations;

namespace Library_Book_Lending.Models;

public class Seat
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string SeatNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Location { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<SeatReservation> Reservations { get; set; } = new List<SeatReservation>();
}
