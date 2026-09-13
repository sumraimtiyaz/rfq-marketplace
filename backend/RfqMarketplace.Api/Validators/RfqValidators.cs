using FluentValidation;
using RfqMarketplace.Api.DTOs.Rfqs;

namespace RfqMarketplace.Api.Validators;

/// <summary>
/// Rules shared by create and update. The deadline comparison uses UTC "today", so a deadline
/// of today is still accepted - it is the last day suppliers can respond, not a past date.
/// </summary>
public class RfqWriteRequestValidator<T> : AbstractValidator<T> where T : IRfqWriteRequest
{
    public RfqWriteRequestValidator()
    {
        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name must be 200 characters or fewer.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(5000).WithMessage("Description must be 5000 characters or fewer.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0.")
            .LessThanOrEqualTo(10_000_000).WithMessage("Quantity must be 10,000,000 or fewer.");

        RuleFor(x => x.DeliveryLocation)
            .NotEmpty().WithMessage("Delivery location is required.")
            .MaximumLength(200).WithMessage("Delivery location must be 200 characters or fewer.");

        RuleFor(x => x.Deadline)
            .Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Deadline must be today or a future date.")
            .Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow).AddYears(5))
            .WithMessage("Deadline must be within the next 5 years.");
    }
}

public class CreateRfqRequestValidator : RfqWriteRequestValidator<CreateRfqRequest>;

public class UpdateRfqRequestValidator : RfqWriteRequestValidator<UpdateRfqRequest>;
