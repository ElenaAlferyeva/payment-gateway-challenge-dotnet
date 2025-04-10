using System.Globalization;
using FluentValidation;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Validators
{
    public class PostPaymentRequestValidator : AbstractValidator<PostPaymentRequest>
    {
        public PostPaymentRequestValidator()
        {
            RuleFor(x => x.CardNumber)
                .NotEmpty()
                .WithMessage("Card number is required.")
                .Length(14, 19)
                .WithMessage("Card number must be between 14 and 19 digits long.");

            RuleFor(x => x.ExpiryMonth)
                .NotEmpty()
                .WithMessage("ExpiryMonth is required.")
                .InclusiveBetween(1, 12)
                .WithMessage("Expiry month must be between 1 and 12.");

            RuleFor(x => x.ExpiryYear)
                .NotEmpty()
                .WithMessage("ExpiryYear is required.")
                .GreaterThanOrEqualTo(DateTime.UtcNow.Year)
                .WithMessage("Expiry year must be this year or in the future.");

            RuleFor(x => x)
                .Must(request =>
                {
                    if (request.ExpiryMonth is < 1 or > 12 || request.ExpiryYear == 0)
                        return false; // Let the individual rules handle this

                    // Create a DateTime for the *last moment* of the expiry month
                    var expiryDate = new DateTime(request.ExpiryYear, request.ExpiryMonth, 1).AddMonths(1).AddSeconds(-1);
                    return expiryDate >= DateTime.UtcNow;
                })
                .WithMessage("The expiry date must be in the future.");

            RuleFor(x => x.Currency)
                .NotEmpty()
                .WithMessage("Currency is required.")
                .Length(3)
                .WithMessage("Currency code must be exactly 3 characters.")
                .Must(BeAValidISOCurrency)
                .WithMessage("Currency must be a valid ISO code.");

            RuleFor(x => x.Amount)
                .NotEmpty()
                .WithMessage("Amount is required.");

            RuleFor(x => x.Cvv)
                .NotEmpty()
                .WithMessage("CVV is required.")
                .Must(cvv => cvv.ToString().Length is 3 or 4)
                .WithMessage("CVV must be 3 or 4 digits long.");
        }

        private static readonly HashSet<string> IsoCurrencyCodes = CultureInfo
            .GetCultures(CultureTypes.SpecificCultures)
            .Select(c =>
            {
                try
                {
                    return new RegionInfo(c.LCID).ISOCurrencySymbol.ToUpperInvariant();
                }
                catch
                {
                    return null;
                }
            })
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Cast<string>()
            .Distinct()
            .ToHashSet();

        private static bool BeAValidISOCurrency(string currencyCode)
        {
            return !string.IsNullOrWhiteSpace(currencyCode) &&
                   currencyCode.Length == 3 &&
                   IsoCurrencyCodes.Contains(currencyCode.ToUpperInvariant());
        }

    }
}
