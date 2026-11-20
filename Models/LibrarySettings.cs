namespace Library_Book_Lending.Models;

public class LibrarySettings
{
    public const string SectionName = "Library";

    public int LoanDays { get; set; } = 14;

    public int ReservationHoldDays { get; set; } = 3;
}
