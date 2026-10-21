using FluentValidation;

namespace OxyKit.Mail.Features.Identity;

public class RemoveRoleRequestValidator : AbstractValidator<RemoveRoleRequest>
{
    public RemoveRoleRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).NotEmpty();
    }
}
