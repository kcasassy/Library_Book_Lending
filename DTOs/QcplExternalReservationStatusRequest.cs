using System.ComponentModel.DataAnnotations;

namespace Library_Book_Lending.DTOs;

public class QcplExternalReservationStatusRequest
{
    [Required]
    [MaxLength(50)]
    public string ReservationStatus { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }
}
