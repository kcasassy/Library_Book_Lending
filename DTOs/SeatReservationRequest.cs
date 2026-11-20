using System.ComponentModel.DataAnnotations;

namespace Library_Book_Lending.DTOs;
public class SeatReservationRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int SeatId { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }
}
