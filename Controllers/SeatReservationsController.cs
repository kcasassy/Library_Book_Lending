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
public class SeatReservationsController : ControllerBase
{
    private readonly LibraryDbContext _context;

    public SeatReservationsController(LibraryDbContext context)
    {
        _context = context;
    }

    private int? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub")
                    ?? User.FindFirstValue("id");

        return int.TryParse(value, out var id) ? id : null;
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

    private static SeatReservationResponse ToResponse(SeatReservation reservation)
    {
        return new SeatReservationResponse
        {
            Id = reservation.Id,
            UserId = reservation.UserId,
            MemberName = reservation.User?.FullName,
            SeatId = reservation.SeatId,
            SeatNumber = reservation.Seat?.SeatNumber,
            Location = reservation.Seat?.Location,
            StartTime = DateTime.SpecifyKind(reservation.StartTime, DateTimeKind.Utc),
            EndTime = DateTime.SpecifyKind(reservation.EndTime, DateTimeKind.Utc),
            Status = reservation.Status
        };
    }

    [HttpGet]
    public async Task<IActionResult> GetReservations()
    {
        var userId = GetUserId();

        if (userId == null)
            return Unauthorized();

        var query = _context.SeatReservations
            .Include(r => r.Seat)
            .Include(r => r.User)
            .AsNoTracking()
            .AsQueryable();

        if (!IsStaff())
            query = query.Where(r => r.UserId == userId.Value);

        var reservations = await query
            .OrderByDescending(r => r.StartTime)
            .ToListAsync();

        return Ok(reservations.Select(ToResponse));
    }

    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailableSeats(
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime)
    {
        startTime = NormalizeUtc(startTime);
        endTime = NormalizeUtc(endTime);

        if (startTime <= DateTime.UtcNow)
            return BadRequest(new { message = "Reservation must be in the future." });

        if (startTime >= endTime)
            return BadRequest(new { message = "End time must be later than start time." });

        var availableSeats = await _context.Seats
            .AsNoTracking()
            .Where(s => s.IsActive &&
                !_context.SeatReservations.Any(r =>
                    r.SeatId == s.Id &&
                    (r.Status == "Pending" || r.Status == "Approved") &&
                    r.StartTime < endTime &&
                    r.EndTime > startTime))
            .OrderBy(s => s.SeatNumber)
            .Select(s => new SeatResponse
            {
                Id = s.Id,
                SeatNumber = s.SeatNumber,
                Location = s.Location,
                IsActive = s.IsActive
            })
            .ToListAsync();

        return Ok(availableSeats);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetReservation(int id)
    {
        var userId = GetUserId();

        if (userId == null)
            return Unauthorized();

        var reservation = await _context.SeatReservations
            .Include(r => r.Seat)
            .Include(r => r.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
            return NotFound(new { message = "Reservation not found." });

        if (!IsStaff() && reservation.UserId != userId.Value)
            return Forbid();

        return Ok(ToResponse(reservation));
    }

    [HttpPost]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> CreateReservation(SeatReservationRequest request)
    {
        var userId = GetUserId();

        if (userId == null)
            return Unauthorized();

        var member = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId.Value);

        if (member == null || !member.IsActive || member.Role != UserRole.Member)
            return Forbid();

        if (member.MembershipExpiry.HasValue &&
            member.MembershipExpiry.Value.Date < DateTime.UtcNow.Date)
        {
            return BadRequest(new { message = "Your membership has expired." });
        }

        var startTime = NormalizeUtc(request.StartTime);
        var endTime = NormalizeUtc(request.EndTime);

        if (startTime <= DateTime.UtcNow)
            return BadRequest(new { message = "Reservation must be in the future." });

        if (startTime >= endTime)
            return BadRequest(new { message = "End time must be later than start time." });

        var seat = await _context.Seats
            .FirstOrDefaultAsync(s => s.Id == request.SeatId && s.IsActive);

        if (seat == null)
            return NotFound(new { message = "Seat not found or inactive." });

        var conflict = await _context.SeatReservations.AnyAsync(r =>
            r.SeatId == request.SeatId &&
            (r.Status == "Pending" || r.Status == "Approved") &&
            r.StartTime < endTime &&
            r.EndTime > startTime);

        if (conflict)
            return Conflict(new
            {
                message = "Seat is already reserved for this schedule."
            });

        var reservation = new SeatReservation
        {
            UserId = userId.Value,
            SeatId = request.SeatId,
            StartTime = startTime,
            EndTime = endTime,
            Status = "Pending"
        };

        _context.SeatReservations.Add(reservation);
        await _context.SaveChangesAsync();

        await _context.Entry(reservation)
            .Reference(r => r.Seat)
            .LoadAsync();

        await _context.Entry(reservation)
            .Reference(r => r.User)
            .LoadAsync();

        return CreatedAtAction(
            nameof(GetReservation),
            new { id = reservation.Id },
            ToResponse(reservation));
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> UpdateReservationStatus(
        int id,
        SeatReservationStatusRequest request)
    {
        var reservation = await _context.SeatReservations
            .Include(r => r.Seat)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
            return NotFound(new { message = "Reservation not found." });

        if (reservation.Status != "Pending")
            return BadRequest(new
            {
                message = "Only pending reservations can be approved or rejected."
            });

        if (request.Status == "Approved")
        {
            if (reservation.EndTime <= DateTime.UtcNow)
                return BadRequest(new
                {
                    message = "Cannot approve a reservation that has already ended."
                });

            var conflict = await _context.SeatReservations.AnyAsync(r =>
                r.Id != id &&
                r.SeatId == reservation.SeatId &&
                (r.Status == "Pending" || r.Status == "Approved") &&
                r.StartTime < reservation.EndTime &&
                r.EndTime > reservation.StartTime);

            if (conflict)
                return Conflict(new
                {
                    message = "Another active reservation conflicts with this schedule."
                });
        }

        reservation.Status = request.Status;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = $"Reservation {request.Status.ToLowerInvariant()} successfully.",
            reservation = ToResponse(reservation)
        });
    }

    [HttpPut("{id:int}/cancel")]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> CancelReservation(int id)
    {
        var userId = GetUserId();

        if (userId == null)
            return Unauthorized();

        var reservation = await _context.SeatReservations
            .Include(r => r.Seat)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
            return NotFound(new { message = "Reservation not found." });

        if (reservation.UserId != userId.Value)
            return Forbid();

        if (reservation.Status != "Pending")
            return BadRequest(new
            {
                message = "Only pending reservations can be cancelled."
            });

        reservation.Status = "Cancelled";

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Reservation cancelled successfully.",
            reservation = ToResponse(reservation)
        });
    }
}
