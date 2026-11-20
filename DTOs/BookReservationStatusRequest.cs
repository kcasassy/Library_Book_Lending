using System.ComponentModel.DataAnnotations;

namespace Library_Book_Lending.DTOs;

public class BookReservationStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}
