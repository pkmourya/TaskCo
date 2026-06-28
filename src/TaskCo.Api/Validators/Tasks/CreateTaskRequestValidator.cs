using FluentValidation;
using TaskCo.Api.Models.Dtos.Tasks;

namespace TaskCo.Api.Validators.Tasks;

public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        When(x => x.Description is not null, () =>
            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters"));

        When(x => x.Status.HasValue, () =>
            RuleFor(x => x.Status!.Value)
                .IsInEnum().WithMessage("Status must be Todo, InProgress, or Done"));

        When(x => x.Priority.HasValue, () =>
            RuleFor(x => x.Priority!.Value)
                .IsInEnum().WithMessage("Priority must be Low, Medium, or High"));
    }
}
