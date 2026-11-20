using System.ComponentModel.DataAnnotations;

namespace Library_Book_Lending.DTOs;

public class ReadingSpaceReservationStatusRequest
{
    [Required]
    [RegularExpression("^(Approved|Rejected)$")]
    public string Status { get; set; } = string.Empty;
}

