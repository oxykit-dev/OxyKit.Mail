using FluentValidation;

namespace OxyKit.Mail.Features.ApiKeys;

public class CreateApiKeyRequestValidator : AbstractValidator<CreateApiKeyRequest>
{
    public CreateApiKeyRequestValidator()
    {
        RuleFor(x => x.Label).NotEmpty();
    }
}
