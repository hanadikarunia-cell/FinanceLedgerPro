using FinanceLedger.Application.DTOs;
using FluentValidation;

namespace FinanceLedger.Application.Validators;

public class UpdateCarDtoValidator : AbstractValidator<UpdateCarDto>
{
    public UpdateCarDtoValidator()
    {
        RuleFor(x => x.Branch)
            .NotEmpty().WithMessage("Branch is required.");

        RuleFor(x => x.Client)
            .NotEmpty().WithMessage("Client is required.")
            .MaximumLength(200);

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Type is required.")
            .MaximumLength(100);

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Model is required.")
            .MaximumLength(100);

        RuleFor(x => x.PlateNumber)
            .NotEmpty().WithMessage("Plate number is required.")
            .MaximumLength(20);

        RuleFor(x => x.MonthlyBill)
            .GreaterThanOrEqualTo(0).WithMessage("Monthly bill cannot be negative.");

        RuleFor(x => x.InitialDebt)
            .GreaterThanOrEqualTo(0).WithMessage("Initial debt cannot be negative.");

        RuleFor(x => x.ContractStartDate)
            .NotEmpty().WithMessage("Contract start date is required.");

        RuleFor(x => x.ContractDurationMonths)
            .GreaterThan(0).WithMessage("Contract duration must be at least 1 month.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000);
    }
}
