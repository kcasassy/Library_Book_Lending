namespace Library_Book_Lending.DTOs;

public class ReadingSpaceReservationResponse
{
	public int Id { get; set; }
	public int UserId { get; set; }
	public string MemberName { get; set; } = string.Empty;
	public int ReadingSpaceId { get; set; }
	public string ReadingSpaceName { get; set; } = string.Empty;
	public string Location { get; set; } = string.Empty;
	public DateTime StartTime { get; set; }
	public DateTime EndTime { get; set; }
	public int NumberOfPeople { get; set; }
	public string Status { get; set; } = string.Empty;
}

