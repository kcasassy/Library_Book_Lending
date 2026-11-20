namespace Library_Book_Lending.DTOs;

public class SeatResponse
{
    public int Id { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
