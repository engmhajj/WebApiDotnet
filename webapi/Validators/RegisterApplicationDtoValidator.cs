using FluentValidation;
using webapi.Models.Dtos;

namespace webapi.Validators;

public class RegisterApplicationDtoValidator : AbstractValidator<RegisterApplicationDto>
{
    public RegisterApplicationDtoValidator()
    {
        RuleFor(x => x.ApplicationName)
            .NotEmpty()
            .WithMessage("Application name is required")
            .MaximumLength(100);

        // RuleFor(x => x.ClientId)
        //     .NotEmpty()
        //     .WithMessage("Client ID is required")
        //     .Matches("^[a-zA-Z0-9\\-]{10,}$")
        //     .WithMessage("Client ID must be at least 10 characters and alphanumeric");
        //
        // RuleFor(x => x.Secret)
        //     .NotEmpty()
        //     .WithMessage("Secret is required")
        //     .MinimumLength(16)
        //     .WithMessage("Secret must be at least 16 characters long");

        RuleFor(x => x.Scopes)
            .NotEmpty()
            .WithMessage("Scopes are required")
            .Matches("^(read|write|delete)(,(read|write|delete))*$")
            .WithMessage("Scopes must be a comma-separated list of: read, write, delete");
    }
}
