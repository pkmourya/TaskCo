using FluentValidation;
using TaskCo.Api.Models.Dtos.Tasks;

namespace TaskCo.Api.Validators.Tasks;

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        When(x => x.Description is not null, () =>
            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters"));

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status must be Todo, InProgress, or Done");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Priority must be Low, Medium, or High");
    }
}
