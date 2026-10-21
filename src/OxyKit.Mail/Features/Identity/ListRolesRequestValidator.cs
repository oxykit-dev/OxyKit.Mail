using FluentValidation;

namespace OxyKit.Mail.Features.Identity;

public class ListRolesRequestValidator : AbstractValidator<ListRolesRequest>
{
    public ListRolesRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
