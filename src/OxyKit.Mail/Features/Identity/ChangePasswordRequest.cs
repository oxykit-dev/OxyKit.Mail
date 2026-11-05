namespace OxyKit.Mail.Features.Identity;

/// <summary>
/// Signed-in user changing their own password.
/// </summary>
public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword
);
