namespace Library_Book_Lending.Controllers;

using global::Library_Book_Lending.DTOs;
using Library_Book_Lending.Data;
using Library_Book_Lending.DTOs;
using Library_Book_Lending.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SeatsController : ControllerBase
{
    private readonly LibraryDbContext _context;

    public SeatsController(LibraryDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetSeats()
    {
        var seats = await _context.Seats
            .AsNoTracking()
            .OrderBy(s => s.SeatNumber)
            .Select(s => new
            {
                s.Id,
                s.SeatNumber,
                s.Location,
                s.IsActive
            })
            .ToListAsync();

        var seatIds = seats.Select(s => s.Id).ToList();
        var now = DateTime.UtcNow;

        var reservations = await _context.SeatReservations
            .AsNoTracking()
            .Where(r =>
                seatIds.Contains(r.SeatId) &&
                (r.Status == "Pending" || r.Status == "Approved") &&
                r.EndTime > now)
            .OrderBy(r => r.StartTime)
            .Select(r => new
            {
                r.Id,
                r.SeatId,
                r.StartTime,
                r.EndTime,
                r.Status
            })
            .ToListAsync();

        var result = seats.Select(seat => new
        {
            seat.Id,
            seat.SeatNumber,
            seat.Location,
            seat.IsActive,
            Reservations = reservations
                .Where(r => r.SeatId == seat.Id)
                .Select(r => new
                {
                    r.Id,
                    r.StartTime,
                    r.EndTime,
                    r.Status
                })
                .ToList()
        });

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSeat(int id)
    {
        var seat = await _context.Seats
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new
            {
                s.Id,
                s.SeatNumber,
                s.Location,
                s.IsActive
            })
            .FirstOrDefaultAsync();

        if (seat == null)
            return NotFound(new { message = "Seat not found." });

        var now = DateTime.UtcNow;

        var reservations = await _context.SeatReservations
            .AsNoTracking()
            .Where(r =>
                r.SeatId == id &&
                (r.Status == "Pending" || r.Status == "Approved") &&
                r.EndTime > now)
            .OrderBy(r => r.StartTime)
            .Select(r => new
            {
                r.Id,
                r.StartTime,
                r.EndTime,
                r.Status
            })
            .ToListAsync();

        return Ok(new
        {
            seat.Id,
            seat.SeatNumber,
            seat.Location,
            seat.IsActive,
            Reservations = reservations
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> CreateSeat(SeatRequest request)
    {
        var seatNumber = request.SeatNumber.Trim();

        var existingSeat = await _context.Seats
            .AnyAsync(s => s.SeatNumber == seatNumber);

        if (existingSeat)
            return Conflict(new { message = "Seat number already exists." });

        var seat = new Seat
        {
            SeatNumber = seatNumber,
            Location = request.Location.Trim(),
            IsActive = true
        };

        _context.Seats.Add(seat);
        await _context.SaveChangesAsync();

        var response = new SeatResponse
        {
            Id = seat.Id,
            SeatNumber = seat.SeatNumber,
            Location = seat.Location,
            IsActive = seat.IsActive
        };

        return CreatedAtAction(nameof(GetSeat), new { id = seat.Id }, response);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> UpdateSeat(int id, SeatRequest request)
    {
        var seat = await _context.Seats
            .FirstOrDefaultAsync(s => s.Id == id);

        if (seat == null)
            return NotFound(new { message = "Seat not found." });

        var seatNumber = request.SeatNumber.Trim();

        var duplicateSeat = await _context.Seats
            .AnyAsync(s => s.Id != id && s.SeatNumber == seatNumber);

        if (duplicateSeat)
            return Conflict(new { message = "Seat number already exists." });

        seat.SeatNumber = seatNumber;
        seat.Location = request.Location.Trim();

        await _context.SaveChangesAsync();

        return Ok(new SeatResponse
        {
            Id = seat.Id,
            SeatNumber = seat.SeatNumber,
            Location = seat.Location,
            IsActive = seat.IsActive
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> DeactivateSeat(int id)
    {
        var seat = await _context.Seats
            .FirstOrDefaultAsync(s => s.Id == id);

        if (seat == null)
            return NotFound(new { message = "Seat not found." });

        if (!seat.IsActive)
            return BadRequest(new { message = "Seat is already inactive." });

        var hasActiveReservations = await _context.SeatReservations
            .AnyAsync(r =>
                r.SeatId == id &&
                (r.Status == "Pending" || r.Status == "Approved") &&
                r.EndTime > DateTime.UtcNow);

        if (hasActiveReservations)
        {
            return Conflict(new
            {
                message = "Cannot deactivate a seat with active reservations."
            });
        }

        seat.IsActive = false;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Seat deactivated successfully.",
            seat.Id,
            seat.SeatNumber,
            seat.IsActive
        });
    }
}

