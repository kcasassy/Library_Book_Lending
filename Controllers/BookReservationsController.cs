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
public class BookReservationsController : ControllerBase
{
    private readonly LibraryDbContext _context;
    private readonly LibrarySettings _settings;

    public BookReservationsController(
        LibraryDbContext context,
        IOptions<LibrarySettings> settings)
    {
        _context = context;
        _settings = settings.Value;
    }

    [HttpPost]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> CreateReservation(BookReservationRequest request)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var book = await _context.Books
            .FirstOrDefaultAsync(b => b.Id == request.BookId && b.IsActive);

        if (book == null)
        {
            return NotFound(new
            {
                message = "Book not found or unavailable."
            });
        }

        var now = DateTime.UtcNow;

        var existingReservation = await _context.BookReservations
            .AnyAsync(r =>
                r.UserId == userId &&
                r.BookId == request.BookId &&
                (r.Status == ReservationStatuses.Pending || r.Status == ReservationStatuses.Approved) &&
                r.ExpiresAt > now);

        if (existingReservation)
        {
            return Conflict(new
            {
                message = "You already have an active reservation for this book."
            });
        }

        var activeReservations = await _context.BookReservations
            .CountAsync(r =>
                r.BookId == request.BookId &&
                (r.Status == ReservationStatuses.Pending || r.Status == ReservationStatuses.Approved) &&
                r.ExpiresAt > now);

        if (activeReservations >= book.AvailableCopies)
        {
            return BadRequest(new
            {
                message = "No available copies for reservation."
            });
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "Active member account not found."
            });
        }

        var reservation = new BookReservation
        {
            UserId = userId,
            BookId = request.BookId,
            ReservedAt = now,
            ExpiresAt = now.AddDays(_settings.ReservationHoldDays),
            Status = ReservationStatuses.Pending
        };

        _context.BookReservations.Add(reservation);
        await _context.SaveChangesAsync();

        var response = new BookReservationResponse
        {
            Id = reservation.Id,
            BookId = book.Id,
            BookTitle = book.Title,
            ISBN = book.ISBN,
            UserId = user.Id,
            MemberName = user.FullName,
            ReservedAt = reservation.ReservedAt,
            ExpiresAt = reservation.ExpiresAt,
            Status = reservation.Status
        };

        return CreatedAtAction(
            nameof(GetMyReservations),
            new { },
            response);
    }

    [HttpGet("my")]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> GetMyReservations()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var reservations = await _context.BookReservations
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.ReservedAt)
            .Select(r => new BookReservationResponse
            {
                Id = r.Id,
                BookId = r.BookId,
                BookTitle = r.Book != null ? r.Book.Title : string.Empty,
                ISBN = r.Book != null ? r.Book.ISBN : string.Empty,
                UserId = r.UserId,
                MemberName = r.User != null ? r.User.FullName : string.Empty,
                ReservedAt = r.ReservedAt,
                ExpiresAt = r.ExpiresAt,
                Status = r.Status
            })
            .ToListAsync();

        return Ok(reservations);
    }

    [HttpGet]
    [Authorize(Roles = "Librarian")]
    public async Task<IActionResult> GetAllReservations()
    {
        var reservations = await _context.BookReservations
            .AsNoTracking()
            .OrderByDescending(r => r.ReservedAt)
            .Select(r => new BookReservationResponse
            {
                Id = r.Id,
                BookId = r.BookId,
                BookTitle = r.Book != null ? r.Book.Title : string.Empty,
                ISBN = r.Book != null ? r.Book.ISBN : string.Empty,
                UserId = r.UserId,
                MemberName = r.User != null ? r.User.FullName : string.Empty,
                ReservedAt = r.ReservedAt,
                ExpiresAt = r.ExpiresAt,
                Status = r.Status
            })
            .ToListAsync();

        return Ok(reservations);
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Librarian")]
    public async Task<IActionResult> UpdateReservationStatus(
        int id,
        BookReservationStatusRequest request)
    {
        var reservation = await _context.BookReservations
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
        {
            return NotFound(new
            {
                message = "Reservation not found."
            });
        }

        if (reservation.Status != ReservationStatuses.Pending)
        {
            return BadRequest(new
            {
                message = "Only pending reservations can be approved or rejected."
            });
        }

        if (reservation.ExpiresAt <= DateTime.UtcNow)
        {
            reservation.Status = ReservationStatuses.Expired;
            await _context.SaveChangesAsync();

            return BadRequest(new
            {
                message = "This reservation has expired."
            });
        }

        var newStatus = request.Status.Trim();

        if (newStatus != ReservationStatuses.Approved && newStatus != ReservationStatuses.Rejected)
        {
            return BadRequest(new
            {
                message = "Status must be Approved or Rejected."
            });
        }

        reservation.Status = newStatus;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = $"Reservation successfully {newStatus.ToLower()}.",
            reservation.Id,
            reservation.Status
        });
    }

    [HttpPut("{id:int}/cancel")]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> CancelReservation(int id)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var reservation = await _context.BookReservations
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (reservation == null)
        {
            return NotFound(new
            {
                message = "Reservation not found."
            });
        }

        if (reservation.Status != ReservationStatuses.Pending)
        {
            return BadRequest(new
            {
                message = "Only pending reservations can be cancelled."
            });
        }

        reservation.Status = ReservationStatuses.Cancelled;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Reservation successfully cancelled."
        });
    }
}
