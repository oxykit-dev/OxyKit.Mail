using FluentValidation;

namespace OxyKit.Mail.Features.Identity;

/// <summary>
/// Validator for user update requests.
/// </summary>
public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Invalid email format.");

        RuleFor(x => x.UserName)
            .MinimumLength(3)
            .MaximumLength(50)
            .Matches("^[a-zA-Z0-9_.-]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.UserName))
            .WithMessage(
                "Username must be 3-50 characters and contain only letters, numbers, dots, hyphens, and underscores."
            );

        RuleFor(x => x.Password)
            .MinimumLength(8)
            .When(x => !string.IsNullOrWhiteSpace(x.Password))
            .WithMessage("Password must be at least 8 characters.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .When(x => !string.IsNullOrWhiteSpace(x.Password))
            .WithMessage("Passwords do not match.");
    }
}
