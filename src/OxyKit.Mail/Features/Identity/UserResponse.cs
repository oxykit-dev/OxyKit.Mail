namespace OxyKit.Mail.Features.Identity;

public record UserResponse(string Id, string UserName, string Email, List<string> Roles);
