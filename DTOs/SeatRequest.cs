using System.ComponentModel.DataAnnotations;

namespace Library_Book_Lending.DTOs;
public class SeatRequest
{
    [Required]
    [MaxLength(20)]
    public string SeatNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Location { get; set; } = string.Empty;
}

