using FluentValidation;
using InvestAPI.DTOs.Assets;

namespace InvestAPI.Validators.Assets
{
    public class CreateAssetDtoValidator : AbstractValidator<CreateAssetDto>
    {
        public CreateAssetDtoValidator()
        {
            RuleFor(x => x.Ticker)
                .NotEmpty()
                .MaximumLength(20);

            RuleFor(x => x.Quantity)
                .GreaterThan(0);

            RuleFor(x => x.AvgBuyPrice)
                .GreaterThan(0);
        }
    }
}
