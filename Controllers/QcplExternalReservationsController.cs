

using Library_Book_Lending.Data;
using Library_Book_Lending.DTOs;
using Library_Book_Lending.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Book_Lending.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Librarian")]
public class QcplExternalReservationsController : ControllerBase
{
	private readonly LibraryDbContext _context;

	private static readonly string[] AllowedStatuses =
	{
		"Pending",
		"Confirmed",
		"Cancelled",
		"Completed"
	};

	public QcplExternalReservationsController(LibraryDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<QcplExternalReservationResponse>>> GetAll(
		[FromQuery] string? status)
	{
		var query = _context.QcplExternalReservations.AsNoTracking();

		if (!string.IsNullOrWhiteSpace(status))
		{
			query = query.Where(r => r.ReservationStatus == status);
		}

		var reservations = await query
			.OrderByDescending(r => r.ReservationDate)
			.Select(r => new QcplExternalReservationResponse
			{
				Id = r.Id,
				ExternalReferenceNumber = r.ExternalReferenceNumber,
				MemberName = r.MemberName,
				BookTitle = r.BookTitle,
				ReservationStatus = r.ReservationStatus,
				ReservationDate = r.ReservationDate,
				LastSyncedAt = r.LastSyncedAt,
				Notes = r.Notes,
				CreatedAt = r.CreatedAt
			})
			.ToListAsync();

		return Ok(reservations);
	}

	[HttpGet("{id:int}")]
	public async Task<ActionResult<QcplExternalReservationResponse>> GetById(int id)
	{
		var reservation = await _context.QcplExternalReservations
			.AsNoTracking()
			.Where(r => r.Id == id)
			.Select(r => new QcplExternalReservationResponse
			{
				Id = r.Id,
				ExternalReferenceNumber = r.ExternalReferenceNumber,
				MemberName = r.MemberName,
				BookTitle = r.BookTitle,
				ReservationStatus = r.ReservationStatus,
				ReservationDate = r.ReservationDate,
				LastSyncedAt = r.LastSyncedAt,
				Notes = r.Notes,
				CreatedAt = r.CreatedAt
			})
			.FirstOrDefaultAsync();

		if (reservation == null)
		{
			return NotFound(new { message = "QCPL reservation record not found." });
		}

		return Ok(reservation);
	}

	[HttpPost]
	public async Task<ActionResult<QcplExternalReservationResponse>> Create(
		QcplExternalReservationRequest request)
	{
		if (!IsValidStatus(request.ReservationStatus))
		{
			return BadRequest(new
			{
				message = "Invalid reservation status.",
				allowedStatuses = AllowedStatuses
			});
		}

		if (request.ReservationDate == default)
		{
			return BadRequest(new { message = "A valid reservation date is required." });
		}

		var referenceNumber = request.ExternalReferenceNumber.Trim();

		var existingReference = await _context.QcplExternalReservations
			.AnyAsync(r => r.ExternalReferenceNumber == referenceNumber);

		if (existingReference)
		{
			return Conflict(new
			{
				message = "This external reference number already exists."
			});
		}

		var reservation = new QcplExternalReservation
		{
			ExternalReferenceNumber = referenceNumber,
			MemberName = request.MemberName.Trim(),
			BookTitle = request.BookTitle.Trim(),
			ReservationStatus = NormalizeStatus(request.ReservationStatus),
			ReservationDate = request.ReservationDate,
			Notes = request.Notes?.Trim(),
			CreatedAt = DateTime.UtcNow
		};

		_context.QcplExternalReservations.Add(reservation);
		await _context.SaveChangesAsync();

		var response = MapResponse(reservation);

		return CreatedAtAction(
			nameof(GetById),
			new { id = reservation.Id },
			response);
	}

	[HttpPut("{id:int}")]
	public async Task<ActionResult<QcplExternalReservationResponse>> Update(
		int id,
		QcplExternalReservationRequest request)
	{
		if (!IsValidStatus(request.ReservationStatus))
		{
			return BadRequest(new
			{
				message = "Invalid reservation status.",
				allowedStatuses = AllowedStatuses
			});
		}

		if (request.ReservationDate == default)
		{
			return BadRequest(new { message = "A valid reservation date is required." });
		}

		var reservation = await _context.QcplExternalReservations
			.FirstOrDefaultAsync(r => r.Id == id);

		if (reservation == null)
		{
			return NotFound(new { message = "QCPL reservation record not found." });
		}

		var referenceNumber = request.ExternalReferenceNumber.Trim();

		var duplicateReference = await _context.QcplExternalReservations
			.AnyAsync(r =>
				r.ExternalReferenceNumber == referenceNumber &&
				r.Id != id);

		if (duplicateReference)
		{
			return Conflict(new
			{
				message = "This external reference number already exists."
			});
		}

		reservation.ExternalReferenceNumber = referenceNumber;
		reservation.MemberName = request.MemberName.Trim();
		reservation.BookTitle = request.BookTitle.Trim();
		reservation.ReservationStatus = NormalizeStatus(request.ReservationStatus);
		reservation.ReservationDate = request.ReservationDate;
		reservation.Notes = request.Notes?.Trim();

		await _context.SaveChangesAsync();

		return Ok(MapResponse(reservation));
	}

	[HttpPut("{id:int}/status")]
	public async Task<ActionResult<QcplExternalReservationResponse>> UpdateStatus(
		int id,
		QcplExternalReservationStatusRequest request)
	{
		if (!IsValidStatus(request.ReservationStatus))
		{
			return BadRequest(new
			{
				message = "Invalid reservation status.",
				allowedStatuses = AllowedStatuses
			});
		}

		var reservation = await _context.QcplExternalReservations
			.FirstOrDefaultAsync(r => r.Id == id);

		if (reservation == null)
		{
			return NotFound(new { message = "QCPL reservation record not found." });
		}

		reservation.ReservationStatus = NormalizeStatus(request.ReservationStatus);
		reservation.Notes = request.Notes?.Trim();

		await _context.SaveChangesAsync();

		return Ok(MapResponse(reservation));
	}

	[HttpPost("{id:int}/record-sync")]
	public async Task<ActionResult<QcplExternalReservationResponse>> RecordSync(int id)
	{
		var reservation = await _context.QcplExternalReservations
			.FirstOrDefaultAsync(r => r.Id == id);

		if (reservation == null)
		{
			return NotFound(new { message = "QCPL reservation record not found." });
		}

		reservation.LastSyncedAt = DateTime.UtcNow;

		await _context.SaveChangesAsync();

		return Ok(MapResponse(reservation));
	}

	private static bool IsValidStatus(string? status)
	{
		return !string.IsNullOrWhiteSpace(status) &&
			   AllowedStatuses.Any(s =>
				   s.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
	}

	private static string NormalizeStatus(string status)
	{
		return AllowedStatuses.First(s =>
			s.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
	}

	private static QcplExternalReservationResponse MapResponse(
		QcplExternalReservation reservation)
	{
		return new QcplExternalReservationResponse
		{
			Id = reservation.Id,
			ExternalReferenceNumber = reservation.ExternalReferenceNumber,
			MemberName = reservation.MemberName,
			BookTitle = reservation.BookTitle,
			ReservationStatus = reservation.ReservationStatus,
			ReservationDate = reservation.ReservationDate,
			LastSyncedAt = reservation.LastSyncedAt,
			Notes = reservation.Notes,
			CreatedAt = reservation.CreatedAt
		};
	}
}

