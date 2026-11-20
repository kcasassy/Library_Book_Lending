using System.ComponentModel.DataAnnotations;

namespace Library_Book_Lending.DTOs;

public class BookRequest
{
    [Required]
    [StringLength(30, MinimumLength = 1)]
    public string ISBN { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(150, MinimumLength = 1)]
    public string Author { get; set; } = string.Empty;

    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [Range(0, 2100)]
    public int? PublishedYear { get; set; }

    [Range(0, 10000)]
    public int TotalCopies { get; set; }

    [Range(0, 10000)]
    public int AvailableCopies { get; set; }
}
