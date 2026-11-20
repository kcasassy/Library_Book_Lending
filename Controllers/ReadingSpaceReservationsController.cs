using Library_Book_Lending.DTOs;
using Library_Book_Lending.Data;
using Library_Book_Lending.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Library_Book_Lending.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReadingSpaceReservationsController : ControllerBase
{
    private readonly LibraryDbContext _context;

    public ReadingSpaceReservationsController(LibraryDbContext context)
    {
        _context = context;
    }

    private int GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userId, out var parsedUserId))
        {
            throw new UnauthorizedAccessException("Invalid user identity.");
        }

        return parsedUserId;
    }

    private bool IsStaff()
    {
        return User.IsInRole("Admin") || User.IsInRole("Librarian");
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static ReadingSpaceReservationResponse ToResponse(
        ReadingSpaceReservation reservation)
    {
        return new ReadingSpaceReservationResponse
        {
            Id = reservation.Id,
            UserId = reservation.UserId,
            MemberName = reservation.User?.FullName ?? "Unknown Member",
            ReadingSpaceId = reservation.ReadingSpaceId,
            ReadingSpaceName = reservation.ReadingSpace?.Name ?? "Unknown Reading Space",
            Location = reservation.ReadingSpace?.Location ?? string.Empty,
            StartTime = reservation.StartTime,
            EndTime = reservation.EndTime,
            NumberOfPeople = reservation.NumberOfPeople,
            Status = reservation.Status
        };
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReadingSpaceReservationResponse>>> GetReservations()
    {
        var query = _context.ReadingSpaceReservations
            .Include(r => r.User)
            .Include(r => r.ReadingSpace)
            .AsQueryable();

        if (!IsStaff())
        {
            query = query.Where(r => r.UserId == GetUserId());
        }

        var reservations = await query
            .OrderByDescending(r => r.StartTime)
            .ToListAsync();

        return Ok(reservations.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReadingSpaceReservationResponse>> GetReservation(int id)
    {
        var reservation = await _context.ReadingSpaceReservations
            .Include(r => r.User)
            .Include(r => r.ReadingSpace)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
        {
            return NotFound(new { message = "Reservation not found." });
        }

        if (!IsStaff() && reservation.UserId != GetUserId())
        {
            return Forbid();
        }

        return Ok(ToResponse(reservation));
    }

    [HttpPost]
    [Authorize(Roles = "Member")]
    public async Task<ActionResult<ReadingSpaceReservationResponse>> CreateReservation(
        ReadingSpaceReservationRequest request)
    {
        var userId = GetUserId();
        var startTime = NormalizeUtc(request.StartTime);
        var endTime = NormalizeUtc(request.EndTime);

        if (startTime <= DateTime.UtcNow)
        {
            return BadRequest(new { message = "Reservation start time must be in the future." });
        }

        if (endTime <= startTime)
        {
            return BadRequest(new { message = "End time must be later than start time." });
        }

        var member = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        if (member == null || member.Role != UserRole.Member)
        {
            return Forbid();
        }

        if (member.MembershipExpiry.HasValue &&
            member.MembershipExpiry.Value.Date < DateTime.UtcNow.Date)
        {
            return BadRequest(new { message = "Your membership has expired." });
        }

        var space = await _context.ReadingSpaces
            .FirstOrDefaultAsync(s =>
                s.Id == request.ReadingSpaceId &&
                s.IsActive);

        if (space == null)
        {
            return NotFound(new { message = "Reading space not found or inactive." });
        }

        if (request.NumberOfPeople <= 0)
        {
            return BadRequest(new { message = "Number of people must be at least 1." });
        }

        if (request.NumberOfPeople > space.Capacity)
        {
            return BadRequest(new
            {
                message = "Number of people exceeds the reading space capacity."
            });
        }

        var hasConflict = await _context.ReadingSpaceReservations.AnyAsync(r =>
            r.ReadingSpaceId == request.ReadingSpaceId &&
            (r.Status == "Pending" || r.Status == "Approved") &&
            startTime < r.EndTime &&
            endTime > r.StartTime);

        if (hasConflict)
        {
            return Conflict(new
            {
                message = "This reading space already has a reservation during the selected time."
            });
        }

        var reservation = new ReadingSpaceReservation
        {
            UserId = userId,
            ReadingSpaceId = request.ReadingSpaceId,
            StartTime = startTime,
            EndTime = endTime,
            NumberOfPeople = request.NumberOfPeople,
            Status = "Pending"
        };

        _context.ReadingSpaceReservations.Add(reservation);
        await _context.SaveChangesAsync();

        await _context.Entry(reservation).Reference(r => r.User).LoadAsync();
        await _context.Entry(reservation).Reference(r => r.ReadingSpace).LoadAsync();

        return CreatedAtAction(
            nameof(GetReservation),
            new { id = reservation.Id },
            ToResponse(reservation));
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<ActionResult<ReadingSpaceReservationResponse>> UpdateStatus(
        int id,
        ReadingSpaceReservationStatusRequest request)
    {
        var reservation = await _context.ReadingSpaceReservations
            .Include(r => r.User)
            .Include(r => r.ReadingSpace)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
        {
            return NotFound(new { message = "Reservation not found." });
        }

        if (reservation.Status != "Pending")
        {
            return BadRequest(new
            {
                message = "Only Pending reservations can be approved or rejected."
            });
        }

        if (request.Status != "Approved" && request.Status != "Rejected")
        {
            return BadRequest(new
            {
                message = "Status must be Approved or Rejected."
            });
        }

        if (request.Status == "Approved")
        {
            if (reservation.ReadingSpace == null || !reservation.ReadingSpace.IsActive)
            {
                return BadRequest(new
                {
                    message = "Cannot approve a reservation for an inactive or unavailable reading space."
                });
            }

            if (reservation.EndTime <= DateTime.UtcNow)
            {
                return BadRequest(new
                {
                    message = "Cannot approve a reservation that has already ended."
                });
            }

            var hasConflict = await _context.ReadingSpaceReservations.AnyAsync(r =>
                r.Id != id &&
                r.ReadingSpaceId == reservation.ReadingSpaceId &&
                (r.Status == "Pending" || r.Status == "Approved") &&
                reservation.StartTime < r.EndTime &&
                reservation.EndTime > r.StartTime);

            if (hasConflict)
            {
                return Conflict(new
                {
                    message = "Another reservation conflicts with this schedule."
                });
            }

            if (reservation.NumberOfPeople > reservation.ReadingSpace.Capacity)
            {
                return BadRequest(new
                {
                    message = "Reservation exceeds the reading space capacity."
                });
            }
        }

        reservation.Status = request.Status;
        await _context.SaveChangesAsync();

        return Ok(ToResponse(reservation));
    }

    [HttpPut("{id:int}/cancel")]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> CancelReservation(int id)
    {
        var reservation = await _context.ReadingSpaceReservations
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
        {
            return NotFound(new { message = "Reservation not found." });
        }

        if (reservation.UserId != GetUserId())
        {
            return Forbid();
        }

        if (reservation.Status != "Pending")
        {
            return BadRequest(new
            {
                message = "Only Pending reservations can be cancelled."
            });
        }

        reservation.Status = "Cancelled";
        await _context.SaveChangesAsync();

        return Ok(new { message = "Reservation cancelled successfully." });
    }
}
