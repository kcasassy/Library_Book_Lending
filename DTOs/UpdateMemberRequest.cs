using System.ComponentModel.DataAnnotations;

namespace Library_Book_Lending.DTOs;

public class UpdateMemberRequest
{
    [Required]
    public DateTime? MembershipExpiry { get; set; }

    public bool IsActive { get; set; }
}
