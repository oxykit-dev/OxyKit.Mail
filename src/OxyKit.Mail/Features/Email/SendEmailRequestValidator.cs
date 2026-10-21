using FluentValidation;
using OxyKit.Mail.Contracts;

namespace OxyKit.Mail.Features.Email;

/// <summary>
/// Validator for <see cref="SendEmailRequest"/>.
/// </summary>
public class SendEmailRequestValidator : AbstractValidator<SendEmailRequest>
{
    public SendEmailRequestValidator()
    {
        RuleFor(x => x.To).NotEmpty().EmailAddress().WithName(nameof(SendEmailRequest.To));
        RuleFor(x => x.Subject).NotEmpty().WithName(nameof(SendEmailRequest.Subject));
        RuleFor(x => x.Body).NotEmpty().WithName(nameof(SendEmailRequest.Body));
    }
}
