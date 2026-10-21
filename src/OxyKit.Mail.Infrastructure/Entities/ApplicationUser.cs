using Microsoft.AspNetCore.Identity;

namespace OxyKit.Mail.Infrastructure.Entities;

public class ApplicationUser : IdentityUser
{
    public ICollection<ApiKey> ApiKeys { get; set; } = [];
    public byte[]? Avatar { get; set; }
    public byte[]? AvatarThumbnail { get; set; }
}
