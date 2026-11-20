using System.Linq.Expressions;
using System.Security.Claims;
using Library_Book_Lending.Data;
using Library_Book_Lending.DTOs;
using Library_Book_Lending.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Library_Book_Lending.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BorrowingsController : ControllerBase
{
    private static readonly Expression<Func<Borrowing, BorrowingResponse>> ToBorrowingResponse =
        b => new BorrowingResponse
        {
            Id = b.Id,
            UserId = b.UserId,
            MemberName = b.User != null ? b.User.FullName : string.Empty,
            BookId = b.BookId,
            BookTitle = b.Book != null ? b.Book.Title : string.Empty,
            ISBN = b.Book != null ? b.Book.ISBN : string.Empty,
            ProcessedById = b.ProcessedById,
            BorrowedAt = b.BorrowedAt,
            DueDate = b.DueDate,
            ReturnedAt = b.ReturnedAt,
            Status = b.Status.ToString()
        };

    private readonly LibraryDbContext _context;
    private readonly LibrarySettings _settings;

    public BorrowingsController(
        LibraryDbContext context,
        IOptions<LibrarySettings> settings)
    {
        _context = context;
        _settings = settings.Value;
    }

    [HttpPost]
    [Authorize(Roles = "Librarian")]
    public async Task<IActionResult> CreateBorrowing(BorrowingRequest request)
    {
        var staffIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(staffIdValue, out var staffId))
        {
            return Unauthorized();
        }

        var member = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Id == request.UserId &&
                u.IsActive &&
                u.Role == UserRole.Member);

        if (member == null)
        {
            return NotFound(new
            {
                message = "Active member not found."
            });
        }

        if (!member.MembershipExpiry.HasValue ||
            member.MembershipExpiry.Value < DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message = "Member does not have an active membership. Update the membership expiry first."
            });
        }

        var book = await _context.Books
            .FirstOrDefaultAsync(b =>
                b.Id == request.BookId &&
                b.IsActive);

        if (book == null)
        {
            return NotFound(new
            {
                message = "Book not found or inactive."
            });
        }

        if (book.AvailableCopies <= 0)
        {
            return BadRequest(new
            {
                message = "No available copies for borrowing."
            });
        }

        var alreadyBorrowed = await _context.Borrowings
            .AnyAsync(b =>
                b.UserId == request.UserId &&
                b.BookId == request.BookId &&
                (b.Status == BorrowingStatus.Borrowed ||
                 b.Status == BorrowingStatus.Overdue));

        if (alreadyBorrowed)
        {
            return Conflict(new
            {
                message = "Member already has an unreturned copy of this book."
            });
        }

        var now = DateTime.UtcNow;

        var borrowing = new Borrowing
        {
            UserId = member.Id,
            BookId = book.Id,
            ProcessedById = staffId,
            BorrowedAt = now,
            DueDate = now.AddDays(_settings.LoanDays),
            Status = BorrowingStatus.Borrowed
        };

        book.AvailableCopies--;

        _context.Borrowings.Add(borrowing);

        await _context.SaveChangesAsync();

        var response = new BorrowingResponse
        {
            Id = borrowing.Id,
            UserId = member.Id,
            MemberName = member.FullName,
            BookId = book.Id,
            BookTitle = book.Title,
            ISBN = book.ISBN,
            ProcessedById = staffId,
            BorrowedAt = borrowing.BorrowedAt,
            DueDate = borrowing.DueDate,
            ReturnedAt = borrowing.ReturnedAt,
            Status = borrowing.Status.ToString()
        };

        return CreatedAtAction(
            nameof(GetBorrowingById),
            new { id = borrowing.Id },
            response);
    }

    [HttpGet]
    [Authorize(Roles = "Librarian")]
    public async Task<IActionResult> GetAllBorrowings()
    {
        await UpdateOverdueBorrowings();

        var borrowings = await _context.Borrowings
            .AsNoTracking()
            .OrderByDescending(b => b.BorrowedAt)
            .Select(ToBorrowingResponse)
            .ToListAsync();

        return Ok(borrowings);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Librarian")]
    public async Task<IActionResult> GetBorrowingById(int id)
    {
        await UpdateOverdueBorrowings();

        var borrowing = await _context.Borrowings
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(ToBorrowingResponse)
            .FirstOrDefaultAsync();

        if (borrowing == null)
        {
            return NotFound(new
            {
                message = "Borrowing record not found."
            });
        }

        return Ok(borrowing);
    }

    [HttpGet("my")]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> GetMyBorrowings()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        await UpdateOverdueBorrowings();

        var borrowings = await _context.Borrowings
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BorrowedAt)
            .Select(ToBorrowingResponse)
            .ToListAsync();

        return Ok(borrowings);
    }

    [HttpPut("{id:int}/return")]
    [Authorize(Roles = "Librarian")]
    public async Task<IActionResult> ReturnBook(int id)
    {
        var borrowing = await _context.Borrowings
            .Include(b => b.Book)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (borrowing == null)
        {
            return NotFound(new
            {
                message = "Borrowing record not found."
            });
        }

        if (borrowing.Status == BorrowingStatus.Returned)
        {
            return BadRequest(new
            {
                message = "This book has already been returned."
            });
        }

        if (borrowing.Book == null)
        {
            return BadRequest(new
            {
                message = "Book record not found."
            });
        }

        borrowing.ReturnedAt = DateTime.UtcNow;
        borrowing.Status = BorrowingStatus.Returned;

        if (borrowing.Book.AvailableCopies < borrowing.Book.TotalCopies)
        {
            borrowing.Book.AvailableCopies++;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Book successfully returned.",
            borrowing.Id,
            borrowing.Status,
            borrowing.ReturnedAt,
            borrowing.Book.AvailableCopies
        });
    }

    [HttpGet("overdue")]
    [Authorize(Roles = "Librarian")]
    public async Task<IActionResult> GetOverdueBorrowings()
    {
        await UpdateOverdueBorrowings();

        var overdueBorrowings = await _context.Borrowings
            .AsNoTracking()
            .Where(b => b.Status == BorrowingStatus.Overdue)
            .OrderBy(b => b.DueDate)
            .Select(ToBorrowingResponse)
            .ToListAsync();

        return Ok(overdueBorrowings);
    }

    private async Task UpdateOverdueBorrowings()
    {
        var now = DateTime.UtcNow;

        var overdueRecords = await _context.Borrowings
            .Where(b =>
                b.Status == BorrowingStatus.Borrowed &&
                b.DueDate < now)
            .ToListAsync();

        if (overdueRecords.Count == 0)
        {
            return;
        }

        foreach (var borrowing in overdueRecords)
        {
            borrowing.Status = BorrowingStatus.Overdue;
        }

        await _context.SaveChangesAsync();
    }
}
