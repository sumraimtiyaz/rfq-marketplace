using FluentValidation;
using RfqMarketplace.Api.DTOs.Quotations;

namespace RfqMarketplace.Api.Validators;

public class CreateQuotationRequestValidator : AbstractValidator<CreateQuotationRequest>
{
    public CreateQuotationRequestValidator()
    {
        RuleFor(x => x.QuotedPrice)
            .GreaterThan(0).WithMessage("Quoted price must be greater than 0.")
            .LessThanOrEqualTo(999_999_999_999.99m).WithMessage("Quoted price is too large.")
            .Must(p => decimal.Round(p, 2) == p).WithMessage("Quoted price can have at most 2 decimal places.");

        RuleFor(x => x.EstimatedDeliveryDays)
            .GreaterThan(0).WithMessage("Estimated delivery must be at least 1 day.")
            .LessThanOrEqualTo(3650).WithMessage("Estimated delivery must be 3650 days or fewer.");

        RuleFor(x => x.Message)
            .MaximumLength(2000).WithMessage("Message must be 2000 characters or fewer.");
    }
}
