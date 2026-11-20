using System.ComponentModel.DataAnnotations;

namespace Library_Book_Lending.DTOs;

public class ReadingSpaceRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Location { get; set; } = string.Empty;

    [Range(1, 500)]
    public int Capacity { get; set; }
}