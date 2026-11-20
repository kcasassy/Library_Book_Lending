namespace Library_Book_Lending.DTOs;

public class UserResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? MemberNumber { get; set; }
    public DateTime? MembershipExpiry { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
