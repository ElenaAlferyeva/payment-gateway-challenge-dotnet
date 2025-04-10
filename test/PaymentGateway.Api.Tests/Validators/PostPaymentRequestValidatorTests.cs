using FluentValidation.TestHelper;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Validators;
using FluentAssertions;

namespace PaymentGateway.Api.Tests.Validators
{
    public class PostPaymentRequestValidatorTests
    {
        private readonly PostPaymentRequestValidator _validator;
        private readonly Random _random = new();

        public PostPaymentRequestValidatorTests()
        {
            _validator = new PostPaymentRequestValidator();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        public async Task ShouldHaveError_WhenExpiryMonthIsOutOfRange(int month)
        {
            // Arrange
            var model = new PostPaymentRequest { ExpiryMonth = month };

            // Act
            var result = await _validator.TestValidateAsync(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ExpiryMonth);
        }

        [Fact]
        public async Task ShouldHaveError_WhenExpiryYearIsInPast()
        {
            // Arrange
            var model = new PostPaymentRequest
            {
                ExpiryYear = DateTime.UtcNow.Year - 1
            };

            // Act
            var result = await _validator.TestValidateAsync(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ExpiryYear);
        }

        [Fact]
        public async Task ShouldHaveError_WhenExpiryDateIsInThePast()
        {
            // Arrange
            var pastDate = DateTime.UtcNow.AddMonths(-1);
            var model = new PostPaymentRequest
            {
                ExpiryMonth = pastDate.Month,
                ExpiryYear = pastDate.Year
            };

            // Act
            var result = await _validator.TestValidateAsync(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x)
                  .WithErrorMessage("The expiry date must be in the future.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("US")]
        [InlineData("USAA")]
        public async Task ShouldHaveError_WhenCurrencyIsNotThreeCharacters(string currency)
        {
            // Arrange
            var model = new PostPaymentRequest { Currency = currency };

            // Act
            var result = await _validator.TestValidateAsync(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Currency);
        }

        [Fact]
        public async Task ShouldHaveError_WhenCurrencyIsNotValidISOCurrency()
        {
            // Arrange
            var model = new PostPaymentRequest { Currency = "AAA" };

            // Act
            var result = await _validator.TestValidateAsync(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Currency)
                .WithErrorMessage("Currency must be a valid ISO code.");
        }

        [Fact]
        public async Task ShouldHaveError_WhenAmountIsZero()
        {
            // Arrange
            var model = new PostPaymentRequest { Amount = 0 };

            // Act
            var result = await _validator.TestValidateAsync(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Amount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(12)]
        [InlineData(12345)]
        public async Task ShouldHaveError_WhenCvvIsInvalid(int? cvv)
        {
            // Arrange
            var model = new PostPaymentRequest { Cvv = cvv ?? 0 };

            // Act
            var result = await _validator.TestValidateAsync(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Cvv);
        }

        [Fact]
        public async Task ShouldBeValid_WhenAllFieldsAreCorrect()
        {
            // Arrange
            var model = new PostPaymentRequest
            {
                CardNumber = string.Concat(Enumerable.Range(0, 16).Select(_ => _random.Next(0, 10))),
                ExpiryMonth = _random.Next(1, 13),
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Currency = "USD",
                Amount = _random.Next(100, 10000),
                Cvv = _random.Next(100, 1000)
            };

            // Act
            var result = await _validator.ValidateAsync(model);

            // Assert
            result.IsValid.Should().BeTrue();
        }
    }
}
