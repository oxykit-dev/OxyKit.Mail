using FluentValidation;
using OxyKit.Mail.Contracts;

namespace OxyKit.Mail.Features.Email;

/// <summary>
/// Validator for <see cref="AttachmentRequest"/>.
/// </summary>
public class AttachmentRequestValidator : AbstractValidator<AttachmentRequest>
{
    public AttachmentRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().WithName(nameof(AttachmentRequest.FileName));
        RuleFor(x => x.ContentType).NotEmpty().WithName(nameof(AttachmentRequest.ContentType));
        RuleFor(x => x.Content).NotNull().WithName(nameof(AttachmentRequest.Content));
    }
}
