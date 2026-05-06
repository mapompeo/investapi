using FluentValidation;
using InvestAPI.DTOs.Users;

namespace InvestAPI.Validators.Users
{
    public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
    {
        public UpdateUserDtoValidator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(100)
                .Must(v => v is null || !string.IsNullOrWhiteSpace(v))
                .WithMessage("Nome não pode ser vazio.");

            RuleFor(x => x.Email)
                .EmailAddress()
                .MaximumLength(150)
                .When(x => !string.IsNullOrWhiteSpace(x.Email));

            RuleFor(x => x.Password)
                .MinimumLength(6)
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.Password));
        }
    }
}
