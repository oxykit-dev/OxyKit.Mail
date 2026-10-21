namespace OxyKit.Mail.Features.Identity;

public record MeResponse(string? UserName, IReadOnlyList<string> Roles);
