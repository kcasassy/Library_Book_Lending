
using Library_Book_Lending.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Library_Book_Lending.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(LibraryDbContext context)
    {
        var passwordHasher = new PasswordHasher<User>();

        var testAccounts = new[]
        {
            new User
            {
                FullName = "Librarian",
                Email = "admin@library.com",
                Role = UserRole.Librarian,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                FullName = "Librarian",
                Email = "librarian@library.com",
                Role = UserRole.Librarian,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                FullName = "Member",
                Email = "member@library.com",
                Role = UserRole.Member,
                MemberNumber = "MEM-0001",
                MembershipExpiry = DateTime.UtcNow.AddDays(MembershipDefaults.DefaultDurationDays),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        const string defaultPassword = "Library123!";

        foreach (var account in testAccounts)
        {
            var exists = await context.Users
                .AnyAsync(u => u.Email == account.Email);

            if (exists)
            {
                continue;
            }

            account.PasswordHash = passwordHasher.HashPassword(
                account,
                defaultPassword);

            context.Users.Add(account);
        }

        var membersWithoutExpiry = await context.Users
            .Where(u => u.Role == UserRole.Member && u.MembershipExpiry == null)
            .ToListAsync();

        foreach (var member in membersWithoutExpiry)
        {
            member.MembershipExpiry = DateTime.UtcNow.AddDays(
                MembershipDefaults.DefaultDurationDays);
        }

        var sampleBooks = new[]
        {
            new Book { ISBN = "9780141439518", Title = "Pride and Prejudice", Author = "Jane Austen", Category = "Classic Fiction", PublishedYear = 1813, TotalCopies = 5, AvailableCopies = 5 },
            new Book { ISBN = "9780061120084", Title = "To Kill a Mockingbird", Author = "Harper Lee", Category = "Literary Fiction", PublishedYear = 1960, TotalCopies = 5, AvailableCopies = 5 },
            new Book { ISBN = "9780743273565", Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", Category = "Classic Fiction", PublishedYear = 1925, TotalCopies = 4, AvailableCopies = 4 },
            new Book { ISBN = "9780451524935", Title = "1984", Author = "George Orwell", Category = "Dystopian Fiction", PublishedYear = 1949, TotalCopies = 5, AvailableCopies = 5 },
            new Book { ISBN = "9780547928227", Title = "The Hobbit", Author = "J.R.R. Tolkien", Category = "Fantasy", PublishedYear = 1937, TotalCopies = 4, AvailableCopies = 4 },
            new Book { ISBN = "9780439139601", Title = "Harry Potter and the Goblet of Fire", Author = "J.K. Rowling", Category = "Fantasy", PublishedYear = 2000, TotalCopies = 6, AvailableCopies = 6 },
            new Book { ISBN = "9780316769488", Title = "The Catcher in the Rye", Author = "J.D. Salinger", Category = "Literary Fiction", PublishedYear = 1951, TotalCopies = 3, AvailableCopies = 3 },
            new Book { ISBN = "9781451673319", Title = "Fahrenheit 451", Author = "Ray Bradbury", Category = "Science Fiction", PublishedYear = 1953, TotalCopies = 4, AvailableCopies = 4 },
            new Book { ISBN = "9780486280615", Title = "Jane Eyre", Author = "Charlotte Bronte", Category = "Classic Fiction", PublishedYear = 1847, TotalCopies = 3, AvailableCopies = 3 },
            new Book { ISBN = "9780140449136", Title = "Crime and Punishment", Author = "Fyodor Dostoevsky", Category = "Classic Fiction", PublishedYear = 1866, TotalCopies = 3, AvailableCopies = 3 },

            new Book { ISBN = "9780307474278", Title = "The Da Vinci Code", Author = "Dan Brown", Category = "Mystery", PublishedYear = 2003, TotalCopies = 5, AvailableCopies = 5 },
            new Book { ISBN = "9780439064873", Title = "The Hunger Games", Author = "Suzanne Collins", Category = "Young Adult", PublishedYear = 2008, TotalCopies = 5, AvailableCopies = 5 },
            new Book { ISBN = "9780553573428", Title = "A Game of Thrones", Author = "George R.R. Martin", Category = "Fantasy", PublishedYear = 1996, TotalCopies = 4, AvailableCopies = 4 },
            new Book { ISBN = "9780385732550", Title = "The Book Thief", Author = "Markus Zusak", Category = "Historical Fiction", PublishedYear = 2005, TotalCopies = 3, AvailableCopies = 3 },
            new Book { ISBN = "9780062315007", Title = "The Alchemist", Author = "Paulo Coelho", Category = "Philosophy", PublishedYear = 1988, TotalCopies = 5, AvailableCopies = 5 },
            new Book { ISBN = "9780143127741", Title = "The Little Prince", Author = "Antoine de Saint-Exupery", Category = "Fable", PublishedYear = 1943, TotalCopies = 4, AvailableCopies = 4 },
            new Book { ISBN = "9781501128035", Title = "Educated", Author = "Tara Westover", Category = "Memoir", PublishedYear = 2018, TotalCopies = 3, AvailableCopies = 3 },
            new Book { ISBN = "9780060850524", Title = "Brave New World", Author = "Aldous Huxley", Category = "Dystopian Fiction", PublishedYear = 1932, TotalCopies = 4, AvailableCopies = 4 },
            new Book { ISBN = "9780385504201", Title = "The Kite Runner", Author = "Khaled Hosseini", Category = "Literary Fiction", PublishedYear = 2003, TotalCopies = 4, AvailableCopies = 4 },
            new Book { ISBN = "9780316015844", Title = "Twilight", Author = "Stephenie Meyer", Category = "Romance", PublishedYear = 2005, TotalCopies = 3, AvailableCopies = 3 },

            new Book { ISBN = "9780134685991", Title = "Effective Java", Author = "Joshua Bloch", Category = "Programming", PublishedYear = 2018, TotalCopies = 3, AvailableCopies = 3 },
            new Book { ISBN = "9780132350884", Title = "Clean Code", Author = "Robert C. Martin", Category = "Programming", PublishedYear = 2008, TotalCopies = 3, AvailableCopies = 3 },
            new Book { ISBN = "9780596007126", Title = "Head First Design Patterns", Author = "Eric Freeman", Category = "Programming", PublishedYear = 2004, TotalCopies = 2, AvailableCopies = 2 },
            new Book { ISBN = "9780262033848", Title = "Introduction to Algorithms", Author = "Thomas H. Cormen", Category = "Computer Science", PublishedYear = 2009, TotalCopies = 2, AvailableCopies = 2 },
            new Book { ISBN = "9780131103627", Title = "The C Programming Language", Author = "Brian W. Kernighan", Category = "Programming", PublishedYear = 1988, TotalCopies = 2, AvailableCopies = 2 },

            new Book { ISBN = "9780321573513", Title = "The Art of Computer Programming", Author = "Donald Knuth", Category = "Computer Science", PublishedYear = 1968, TotalCopies = 2, AvailableCopies = 2 },
            new Book { ISBN = "9780134494166", Title = "Computer Networking", Author = "James Kurose", Category = "Networking", PublishedYear = 2017, TotalCopies = 3, AvailableCopies = 3 },
            new Book { ISBN = "9781119607695", Title = "Operating System Concepts", Author = "Abraham Silberschatz", Category = "Computer Science", PublishedYear = 2018, TotalCopies = 3, AvailableCopies = 3 },
            new Book { ISBN = "9780133761313", Title = "Database System Concepts", Author = "Abraham Silberschatz", Category = "Database", PublishedYear = 2019, TotalCopies = 3, AvailableCopies = 3 },
            new Book { ISBN = "9780321146533", Title = "Fundamentals of Software Engineering", Author = "Ian Sommerville", Category = "Software Engineering", PublishedYear = 2011, TotalCopies = 2, AvailableCopies = 2 },
            new Book { ISBN = "9780201633610", Title = "Design Patterns", Author = "Erich Gamma", Category = "Software Engineering", PublishedYear = 1994, TotalCopies = 2, AvailableCopies = 2 }
        };

        foreach (var book in sampleBooks)
        {
            var exists = await context.Books
                .AnyAsync(b => b.ISBN == book.ISBN);

            if (!exists)
            {
                book.IsActive = true;
                book.CreatedAt = DateTime.UtcNow;
                context.Books.Add(book);
            }
        }

        if (!await context.Seats.AnyAsync())
        {
            var seats = new[]
            {
                new Seat { SeatNumber = "A01", Location = "Reading Area A", IsActive = true },
                new Seat { SeatNumber = "A02", Location = "Reading Area A", IsActive = true },
                new Seat { SeatNumber = "A03", Location = "Reading Area A", IsActive = true },
                new Seat { SeatNumber = "A04", Location = "Reading Area A", IsActive = true },
                new Seat { SeatNumber = "A05", Location = "Reading Area A", IsActive = true },
                new Seat { SeatNumber = "B01", Location = "Reading Area B", IsActive = true },
                new Seat { SeatNumber = "B02", Location = "Reading Area B", IsActive = true },
                new Seat { SeatNumber = "B03", Location = "Reading Area B", IsActive = true },
                new Seat { SeatNumber = "B04", Location = "Reading Area B", IsActive = true },
                new Seat { SeatNumber = "B05", Location = "Reading Area B", IsActive = true }
            };

            context.Seats.AddRange(seats);
        }

        if (!await context.ReadingSpaces.AnyAsync())
        {
            var readingSpaces = new[]
            {
                new ReadingSpace
                {
                    Name = "Individual Study Area",
                    Location = "Quiet Reading Section",
                    Capacity = 1,
                    IsActive = true
                },
                new ReadingSpace
                {
                    Name = "Group Discussion Room",
                    Location = "Second Floor",
                    Capacity = 8,
                    IsActive = true
                },
                new ReadingSpace
                {
                    Name = "Quiet Reading Area",
                    Location = "Main Reading Hall",
                    Capacity = 20,
                    IsActive = true
                },
                new ReadingSpace
                {
                    Name = "Computer Research Area",
                    Location = "Digital Learning Section",
                    Capacity = 10,
                    IsActive = true
                }
            };

            context.ReadingSpaces.AddRange(readingSpaces);
        }

        await context.SaveChangesAsync();
    }
}
