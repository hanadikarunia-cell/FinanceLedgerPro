using FinanceLedger.Application.DTOs;
using FluentValidation;

namespace FinanceLedger.Application.Validators;

public class CreateTenantDtoValidator : AbstractValidator<CreateTenantDto>
{
    public CreateTenantDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Site name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Site code is required.")
            .MaximumLength(20)
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("Use letters, digits, - or _ only.");

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("The site admin's email is required.")
            .EmailAddress().WithMessage("Enter a valid email address.");

        RuleFor(x => x.AdminDisplayName)
            .NotEmpty().WithMessage("The site admin's name is required.")
            .MaximumLength(200);

        RuleFor(x => x.AdminPassword)
            .NotEmpty().WithMessage("A password is required for the site admin.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");
    }
}
