

using Library_Book_Lending.Models;
using System.ComponentModel.DataAnnotations;

namespace Library_Book_Lending.Models;

public class ReadingSpaceReservation
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ReadingSpaceId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int NumberOfPeople { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = ReservationStatuses.Pending;

    public User? User { get; set; }

    public ReadingSpace? ReadingSpace { get; set; }
}
