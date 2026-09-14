using FinanceLedger.Application.DTOs;
using FluentValidation;

namespace FinanceLedger.Application.Validators;

public class CreateFeedbackDtoValidator : AbstractValidator<CreateFeedbackDto>
{
    public CreateFeedbackDtoValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Feedback message is required.")
            .MaximumLength(4000);

        RuleFor(x => x.AppVersion)
            .MaximumLength(50);
    }
}
