namespace Library_Book_Lending.DTOs;

public class LibraryReportResponse
{
    public int TotalBooks { get; set; }
    public int TotalBookCopies { get; set; }
    public int AvailableBookCopies { get; set; }
    public int TotalMembers { get; set; }
    public int TotalBorrowings { get; set; }
    public int TotalBookReservations { get; set; }
    public int TotalSeats { get; set; }
    public int ActiveSeats { get; set; }
    public int TotalSeatReservations { get; set; }
    public int TotalReadingSpaces { get; set; }
    public int ActiveReadingSpaces { get; set; }
    public int TotalReadingSpaceReservations { get; set; }
    public int TotalQcplExternalReservations { get; set; }
    public DateTime GeneratedAt { get; set; }
}
