namespace OxyKit.Mail.Features.Identity;

public record CreateUserRequest(
    string UserName,
    string Email,
    string Password,
    string ConfirmPassword
);
