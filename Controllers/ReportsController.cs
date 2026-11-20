using Library_Book_Lending.Data;
using Library_Book_Lending.DTOs;
using Library_Book_Lending.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Book_Lending.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Librarian")]
public class ReportsController : ControllerBase
{
    private readonly LibraryDbContext _context;

    public ReportsController(LibraryDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<LibraryReportResponse>> GetSummary()
    {
        var report = new LibraryReportResponse
        {
            TotalBooks = await _context.Books.CountAsync(),

            TotalBookCopies = await _context.Books
                .SumAsync(b => (int?)b.TotalCopies) ?? 0,

            AvailableBookCopies = await _context.Books
                .SumAsync(b => (int?)b.AvailableCopies) ?? 0,

            TotalMembers = await _context.Users
                .CountAsync(u => u.Role == UserRole.Member),

            TotalBorrowings = await _context.Borrowings.CountAsync(),

            TotalBookReservations = await _context.BookReservations.CountAsync(),

            TotalSeats = await _context.Seats.CountAsync(),

            ActiveSeats = await _context.Seats
                .CountAsync(s => s.IsActive),

            TotalSeatReservations = await _context.SeatReservations.CountAsync(),

            TotalReadingSpaces = await _context.ReadingSpaces.CountAsync(),

            ActiveReadingSpaces = await _context.ReadingSpaces
                .CountAsync(s => s.IsActive),

            TotalReadingSpaceReservations =
                await _context.ReadingSpaceReservations.CountAsync(),

            TotalQcplExternalReservations =
                await _context.QcplExternalReservations.CountAsync(),

            GeneratedAt = DateTime.UtcNow
        };

        return Ok(report);
    }
}
