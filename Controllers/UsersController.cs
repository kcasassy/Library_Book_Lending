using System.Security.Claims;
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
public class UsersController : ControllerBase
{
    private readonly LibraryDbContext _context;

    public UsersController(LibraryDbContext context)
    {
        _context = context;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token." });
        }

        var user = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role.ToString(),
                MemberNumber = u.MemberNumber,
                MembershipExpiry = u.MembershipExpiry,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return NotFound(new { message = "User account not found." });
        }

        return Ok(user);
    }

    [HttpGet]
    [Authorize(Roles = "Librarian")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users
            .OrderBy(u => u.Id)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role.ToString(),
                MemberNumber = u.MemberNumber,
                MembershipExpiry = u.MembershipExpiry,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Librarian")]
    public async Task<IActionResult> UpdateMember(int id, UpdateMemberRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { message = "User account not found." });
        }

        if (user.Role != UserRole.Member)
        {
            return BadRequest(new
            {
                message = "Only member accounts can be updated here."
            });
        }

        user.MembershipExpiry = DateTime.SpecifyKind(
            request.MembershipExpiry!.Value.Date.AddDays(1).AddTicks(-1),
            DateTimeKind.Utc);
        user.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return Ok(new UserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            MemberNumber = user.MemberNumber,
            MembershipExpiry = user.MembershipExpiry,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        });
    }



}