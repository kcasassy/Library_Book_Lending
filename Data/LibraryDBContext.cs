
using Library_Book_Lending.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace Library_Book_Lending.Data;

public class LibraryDbContext : DbContext
{
	public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
		: base(options)
	{
	}

	public DbSet<User> Users => Set<User>();
	public DbSet<Book> Books => Set<Book>();
	public DbSet<BookReservation> BookReservations => Set<BookReservation>();
	public DbSet<Borrowing> Borrowings => Set<Borrowing>();
	public DbSet<Seat> Seats => Set<Seat>();
	public DbSet<ReadingSpace> ReadingSpaces => Set<ReadingSpace>();
	public DbSet<SeatReservation> SeatReservations => Set<SeatReservation>();
	public DbSet<ReadingSpaceReservation> ReadingSpaceReservations => Set<ReadingSpaceReservation>();
	public DbSet<QcplExternalReservation> QcplExternalReservations => Set<QcplExternalReservation>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<User>()
			.HasIndex(u => u.Email)
			.IsUnique();

		modelBuilder.Entity<User>()
			.HasIndex(u => u.MemberNumber)
			.IsUnique()
			.HasFilter("[MemberNumber] IS NOT NULL");

		modelBuilder.Entity<Book>()
			.HasIndex(b => b.ISBN)
			.IsUnique();

		modelBuilder.Entity<Book>()
			.ToTable(t => t.HasCheckConstraint(
				"CK_Books_AvailableCopies",
				"[AvailableCopies] >= 0 AND [AvailableCopies] <= [TotalCopies]"));

		modelBuilder.Entity<Borrowing>()
			.HasOne(b => b.User)
			.WithMany(u => u.Borrowings)
			.HasForeignKey(b => b.UserId)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<Borrowing>()
			.HasOne(b => b.Book)
			.WithMany(b => b.Borrowings)
			.HasForeignKey(b => b.BookId)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<Borrowing>()
			.HasOne(b => b.ProcessedBy)
			.WithMany()
			.HasForeignKey(b => b.ProcessedById)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<BookReservation>()
			.HasOne(r => r.User)
			.WithMany(u => u.BookReservations)
			.HasForeignKey(r => r.UserId)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<BookReservation>()
			.HasOne(r => r.Book)
			.WithMany(b => b.Reservations)
			.HasForeignKey(r => r.BookId)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<SeatReservation>()
			.HasOne(r => r.User)
			.WithMany(u => u.SeatReservations)
			.HasForeignKey(r => r.UserId)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<SeatReservation>()
			.HasOne(r => r.Seat)
			.WithMany(s => s.Reservations)
			.HasForeignKey(r => r.SeatId)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<ReadingSpaceReservation>()
			.HasOne(r => r.User)
			.WithMany(u => u.ReadingSpaceReservations)
			.HasForeignKey(r => r.UserId)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<ReadingSpaceReservation>()
			.HasOne(r => r.ReadingSpace)
			.WithMany(s => s.Reservations)
			.HasForeignKey(r => r.ReadingSpaceId)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<QcplExternalReservation>()
			.HasIndex(r => r.ExternalReferenceNumber)
			.IsUnique();
	}
}
