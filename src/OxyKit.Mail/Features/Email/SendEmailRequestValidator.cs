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

        When(
            x => string.IsNullOrWhiteSpace(x.TemplateId),
            () =>
            {
                RuleFor(x => x.Subject).NotEmpty().WithName(nameof(SendEmailRequest.Subject));
                RuleFor(x => x.Body).NotEmpty().WithName(nameof(SendEmailRequest.Body));
            }
        );

        When(
            x => !string.IsNullOrWhiteSpace(x.TemplateId),
            () =>
            {
                RuleFor(x => x.TemplateId)
                    .Matches("^[A-Za-z0-9][A-Za-z0-9_-]{0,63}$")
                    .WithName(nameof(SendEmailRequest.TemplateId));
                RuleFor(x => x.Data)
                    .NotNull()
                    .WithMessage("Field 'data' is required when templateId is set.")
                    .WithName(nameof(SendEmailRequest.Data));
            }
        );
    }
}
