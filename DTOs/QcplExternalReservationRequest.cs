using System.ComponentModel.DataAnnotations;

namespace Library_Book_Lending.DTOs;

public class QcplExternalReservationRequest
{
	[Required]
	[MaxLength(100)]
	public string ExternalReferenceNumber { get; set; } = string.Empty;

	[MaxLength(150)]
	public string MemberName { get; set; } = string.Empty;

	[MaxLength(200)]
	public string BookTitle { get; set; } = string.Empty;

	[Required]
	[MaxLength(50)]
	public string ReservationStatus { get; set; } = "Pending";

	public DateTime ReservationDate { get; set; }

	[MaxLength(500)]
	public string? Notes { get; set; }
}

