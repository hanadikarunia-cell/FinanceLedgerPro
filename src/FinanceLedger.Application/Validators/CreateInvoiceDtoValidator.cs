using FinanceLedger.Application.DTOs;
using FinanceLedger.Domain.Enums;
using FluentValidation;

namespace FinanceLedger.Application.Validators;

public class CreateInvoiceDtoValidator : AbstractValidator<CreateInvoiceDto>
{
    public CreateInvoiceDtoValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.Branch)
            .NotEmpty().WithMessage("Branch is required.");

        RuleFor(x => x.InvoiceDate)
            .NotEmpty().WithMessage("Invoice date is required.");

        RuleFor(x => x.CarId)
            .NotEmpty()
            .When(x => x.Type == InvoiceType.Rental)
            .WithMessage("Select which car this rental invoice is for.");

        RuleFor(x => x.ClientName)
            .NotEmpty()
            .When(x => x.Type == InvoiceType.ServiceBill)
            .WithMessage("Client name is required.")
            .MaximumLength(200);

        RuleFor(x => x.WageDeposit)
            .NotNull()
            .GreaterThanOrEqualTo(0)
            .When(x => x.Type == InvoiceType.ServiceBill)
            .WithMessage("Wage Deposit is required and cannot be negative.");

        RuleFor(x => x.Fee)
            .NotNull()
            .GreaterThanOrEqualTo(0)
            .When(x => x.Type == InvoiceType.ServiceBill)
            .WithMessage("Fee is required and cannot be negative.");

        RuleFor(x => x.TaxScheme)
            .NotNull()
            .IsInEnum()
            .When(x => x.Type == InvoiceType.ServiceBill)
            .WithMessage("Select a tax scheme.");

        RuleFor(x => x.DriverName)
            .MaximumLength(200);
    }
}
