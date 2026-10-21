namespace OxyKit.Mail.Features.Identity;

public class RemoveRoleRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
