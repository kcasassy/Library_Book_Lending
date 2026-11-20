
using Library_Book_Lending.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Library_Book_Lending.Models;

public class SeatReservation
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int SeatId { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [ForeignKey(nameof(SeatId))]
    public Seat? Seat { get; set; }
}
