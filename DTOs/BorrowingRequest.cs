using System.ComponentModel.DataAnnotations;

namespace Library_Book_Lending.DTOs;

public class BorrowingRequest
{
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    [Range(1, int.MaxValue)]
    public int BookId { get; set; }
}
