

using System.ComponentModel.DataAnnotations;

namespace Library_Book_Lending.Models;

public class QcplExternalReservation
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string ExternalReferenceNumber { get; set; } = string.Empty;

    [MaxLength(150)]
    public string MemberName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string BookTitle { get; set; } = string.Empty;

    [MaxLength(50)]
    public string ReservationStatus { get; set; } = "Pending";

    public DateTime ReservationDate { get; set; }

    public DateTime? LastSyncedAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
