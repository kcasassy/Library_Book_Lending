using Library_Book_Lending.Data;
using Library_Book_Lending.DTOs;
using Library_Book_Lending.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Book_Lending.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReadingSpacesController : ControllerBase
{
    private readonly LibraryDbContext _context;

    public ReadingSpacesController(LibraryDbContext context)
    {
        _context = context;
    }

    private static ReadingSpaceResponse ToResponse(ReadingSpace space)
    {
        return new ReadingSpaceResponse
        {
            Id = space.Id,
            Name = space.Name,
            Location = space.Location,
            Capacity = space.Capacity,
            IsActive = space.IsActive
        };
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReadingSpaceResponse>>> GetReadingSpaces()
    {
        var spaces = await _context.ReadingSpaces
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();

        return Ok(spaces.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReadingSpaceResponse>> GetReadingSpace(int id)
    {
        var space = await _context.ReadingSpaces
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

        if (space == null)
        {
            return NotFound(new { message = "Reading space not found." });
        }

        return Ok(ToResponse(space));
    }

    [HttpPost]
    [Authorize(Roles = "Librarian")]
    public async Task<ActionResult<ReadingSpaceResponse>> CreateReadingSpace(
        ReadingSpaceRequest request)
    {
        var name = request.Name.Trim();
        var location = request.Location.Trim();

        var duplicate = await _context.ReadingSpaces.AnyAsync(s =>
            s.IsActive &&
            s.Name.ToLower() == name.ToLower() &&
            s.Location.ToLower() == location.ToLower());

        if (duplicate)
        {
            return Conflict(new
            {
                message = "A reading space with this name and location already exists."
            });
        }

        var space = new ReadingSpace
        {
            Name = name,
            Location = location,
            Capacity = request.Capacity,
            IsActive = true
        };

        _context.ReadingSpaces.Add(space);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetReadingSpace),
            new { id = space.Id },
            ToResponse(space));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Librarian")]
    public async Task<ActionResult<ReadingSpaceResponse>> UpdateReadingSpace(
        int id,
        ReadingSpaceRequest request)
    {
        var space = await _context.ReadingSpaces
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

        if (space == null)
        {
            return NotFound(new { message = "Reading space not found." });
        }

        var name = request.Name.Trim();
        var location = request.Location.Trim();

        var duplicate = await _context.ReadingSpaces.AnyAsync(s =>
            s.Id != id &&
            s.IsActive &&
            s.Name.ToLower() == name.ToLower() &&
            s.Location.ToLower() == location.ToLower());

        if (duplicate)
        {
            return Conflict(new
            {
                message = "A reading space with this name and location already exists."
            });
        }

        var hasOversizedReservation = await _context.ReadingSpaceReservations
            .AnyAsync(r =>
                r.ReadingSpaceId == id &&
                (r.Status == "Pending" || r.Status == "Approved") &&
                r.NumberOfPeople > request.Capacity);

        if (hasOversizedReservation)
        {
            return BadRequest(new
            {
                message = "Capacity cannot be lower than the number of people in an active reservation."
            });
        }

        space.Name = name;
        space.Location = location;
        space.Capacity = request.Capacity;

        await _context.SaveChangesAsync();

        return Ok(ToResponse(space));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Librarian")]
    public async Task<IActionResult> DeactivateReadingSpace(int id)
    {
        var space = await _context.ReadingSpaces
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

        if (space == null)
        {
            return NotFound(new { message = "Reading space not found." });
        }

        var hasActiveReservations = await _context.ReadingSpaceReservations
            .AnyAsync(r =>
                r.ReadingSpaceId == id &&
                (r.Status == "Pending" || r.Status == "Approved") &&
                r.EndTime > DateTime.UtcNow);

        if (hasActiveReservations)
        {
            return Conflict(new
            {
                message = "This reading space cannot be deactivated while it has pending or approved future reservations."
            });
        }

        space.IsActive = false;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Reading space deactivated successfully."
        });
    }
}