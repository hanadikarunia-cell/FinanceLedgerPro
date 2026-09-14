using FinanceLedger.Application.DTOs;
using FluentValidation;

namespace FinanceLedger.Application.Validators;

public class CreatePettyCashRequestDtoValidator : AbstractValidator<CreatePettyCashRequestDto>
{
    public CreatePettyCashRequestDtoValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required.")
            .MaximumLength(500);

        RuleFor(x => x.Branch)
            .NotEmpty().WithMessage("Branch is required.");
    }
}
