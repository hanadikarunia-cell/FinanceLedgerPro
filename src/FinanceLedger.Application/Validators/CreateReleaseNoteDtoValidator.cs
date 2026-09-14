using FinanceLedger.Application.DTOs;
using FluentValidation;

namespace FinanceLedger.Application.Validators;

public class CreateReleaseNoteDtoValidator : AbstractValidator<CreateReleaseNoteDto>
{
    public CreateReleaseNoteDtoValidator()
    {
        RuleFor(x => x.Version)
            .NotEmpty().WithMessage("Version is required.")
            .MaximumLength(50);

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200);

        RuleFor(x => x.Type)
            .IsInEnum();
    }
}

public class UpdateReleaseNoteDtoValidator : AbstractValidator<UpdateReleaseNoteDto>
{
    public UpdateReleaseNoteDtoValidator()
    {
        RuleFor(x => x.Version)
            .NotEmpty().WithMessage("Version is required.")
            .MaximumLength(50);

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200);

        RuleFor(x => x.Type)
            .IsInEnum();
    }
}
