using FinanceLedger.Application.DTOs;
using FinanceLedger.Domain.Constants;
using FinanceLedger.Domain.Enums;
using FluentValidation;

namespace FinanceLedger.Application.Validators;

public class UpdateTransactionDtoValidator : AbstractValidator<UpdateTransactionDto>
{
    public UpdateTransactionDtoValidator()
    {
        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .MaximumLength(100)
            .Must((dto, category) => TransactionCategories.IsValid(dto.Type, category))
            .WithMessage("Category must be one of the supported categories for the selected type.");

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Date)
            .NotEmpty()
            .LessThanOrEqualTo(_ => DateTime.UtcNow.AddDays(1))
            .WithMessage("Date cannot be in the future.");

        RuleFor(x => x.Branch)
            .NotEmpty().WithMessage("Branch is required.");

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.RelatedUserId)
            .NotEmpty()
            .When(x => x.Type == TransactionType.Expense
                && string.Equals(x.Category, "Salaries", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Select which employee this salary payment is for.");

        RuleFor(x => x.CarId)
            .NotEmpty()
            .When(x => x.Type == TransactionType.Expense
                && (string.Equals(x.Category, "Service", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(x.Category, "Car Debt", StringComparison.OrdinalIgnoreCase)))
            .WithMessage("Select which car this is for.");
    }
}
