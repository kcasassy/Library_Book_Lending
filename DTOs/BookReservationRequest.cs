using System.ComponentModel.DataAnnotations;

namespace Library_Book_Lending.DTOs;

public class BookReservationRequest
{
    [Range(1, int.MaxValue)]
    public int BookId { get; set; }
}
