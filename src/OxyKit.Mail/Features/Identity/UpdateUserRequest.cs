namespace OxyKit.Mail.Features.Identity;

public record UpdateUserRequest(
    string Id,
    string? UserName,
    string? Email,
    string? Password,
    string? ConfirmPassword
);
