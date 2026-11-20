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
public class BooksController : ControllerBase
{
    private readonly LibraryDbContext _context;

    public BooksController(LibraryDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllBooks()
    {
        var books = await _context.Books
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Title)
            .Select(b => new BookResponse
            {
                Id = b.Id,
                ISBN = b.ISBN,
                Title = b.Title,
                Author = b.Author,
                Category = b.Category,
                PublishedYear = b.PublishedYear,
                TotalCopies = b.TotalCopies,
                AvailableCopies = b.AvailableCopies,
                IsActive = b.IsActive,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();

        return Ok(books);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetBookById(int id)
    {
        var book = await _context.Books
            .AsNoTracking()
            .Where(b => b.Id == id && b.IsActive)
            .Select(b => new BookResponse
            {
                Id = b.Id,
                ISBN = b.ISBN,
                Title = b.Title,
                Author = b.Author,
                Category = b.Category,
                PublishedYear = b.PublishedYear,
                TotalCopies = b.TotalCopies,
                AvailableCopies = b.AvailableCopies,
                IsActive = b.IsActive,
                CreatedAt = b.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (book == null)
        {
            return NotFound(new
            {
                message = "Book not found."
            });
        }

        return Ok(book);
    }

    [HttpPost]
    [Authorize(Roles = "Librarian")]
    public async Task<IActionResult> CreateBook(BookRequest request)
    {
        var isbn = request.ISBN.Trim();

        if (request.AvailableCopies > request.TotalCopies)
        {
            return BadRequest(new
            {
                message = "Available copies cannot exceed total copies."
            });
        }

        var existingBook = await _context.Books
            .AnyAsync(b => b.ISBN == isbn);

        if (existingBook)
        {
            return Conflict(new
            {
                message = "A book with this ISBN already exists."
            });
        }

        var book = new Book
        {
            ISBN = isbn,
            Title = request.Title.Trim(),
            Author = request.Author.Trim(),
            Category = request.Category?.Trim() ?? string.Empty,
            PublishedYear = request.PublishedYear,
            TotalCopies = request.TotalCopies,
            AvailableCopies = request.AvailableCopies,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Books.Add(book);
        await _context.SaveChangesAsync();

        var response = new BookResponse
        {
            Id = book.Id,
            ISBN = book.ISBN,
            Title = book.Title,
            Author = book.Author,
            Category = book.Category,
            PublishedYear = book.PublishedYear,
            TotalCopies = book.TotalCopies,
            AvailableCopies = book.AvailableCopies,
            IsActive = book.IsActive,
            CreatedAt = book.CreatedAt
        };

        return CreatedAtAction(
            nameof(GetBookById),
            new { id = book.Id },
            response);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Librarian")]
    public async Task<IActionResult> UpdateBook(int id, BookRequest request)
    {
        var book = await _context.Books
            .FirstOrDefaultAsync(b => b.Id == id && b.IsActive);

        if (book == null)
        {
            return NotFound(new
            {
                message = "Book not found."
            });
        }

        var isbn = request.ISBN.Trim();

        var duplicateIsbn = await _context.Books
            .AnyAsync(b => b.ISBN == isbn && b.Id != id);

        if (duplicateIsbn)
        {
            return Conflict(new
            {
                message = "Another book already uses this ISBN."
            });
        }

        var currentlyUnavailable = book.TotalCopies - book.AvailableCopies;

        if (request.TotalCopies < currentlyUnavailable)
        {
            return BadRequest(new
            {
                message = "Total copies cannot be lower than the number of currently unavailable copies."
            });
        }

        book.ISBN = isbn;
        book.Title = request.Title.Trim();
        book.Author = request.Author.Trim();
        book.Category = request.Category?.Trim() ?? string.Empty;
        book.PublishedYear = request.PublishedYear;
        book.TotalCopies = request.TotalCopies;
        book.AvailableCopies = request.TotalCopies - currentlyUnavailable;

        await _context.SaveChangesAsync();

        return Ok(new BookResponse
        {
            Id = book.Id,
            ISBN = book.ISBN,
            Title = book.Title,
            Author = book.Author,
            Category = book.Category,
            PublishedYear = book.PublishedYear,
            TotalCopies = book.TotalCopies,
            AvailableCopies = book.AvailableCopies,
            IsActive = book.IsActive,
            CreatedAt = book.CreatedAt
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Librarian")]
    public async Task<IActionResult> DeactivateBook(int id)
    {
        var book = await _context.Books
            .FirstOrDefaultAsync(b => b.Id == id);

        if (book == null)
        {
            return NotFound(new
            {
                message = "Book not found."
            });
        }

        if (!book.IsActive)
        {
            return NoContent();
        }

        book.IsActive = false;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Book successfully deactivated."
        });
    }
}
