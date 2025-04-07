using FluentValidation.TestHelper;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Validators;
using FluentAssertions;

namespace PaymentGateway.Api.Tests.Validators
{
    public class PostPaymentRequestValidatorTests
    {
        private readonly PostPaymentRequestValidator _validator;

        public PostPaymentRequestValidatorTests()
        {
            _validator = new PostPaymentRequestValidator();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        public async Task ShouldHaveErrorWhenExpiryMonthIsOutOfRange(int month)
        {
            // Arrange
            var model = new PostPaymentRequest { ExpiryMonth = month };

            // Act
            var result = await _validator.TestValidateAsync(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ExpiryMonth);
        }

        [Fact]
        public async Task ShouldHaveErrorWhenExpiryYearIsInPast()
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
        public async Task ShouldHaveErrorWhenExpiryDateIsInThePast()
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
        public async Task ShouldHaveErrorWhenCurrencyIsNotThreeCharacters(string currency)
        {
            // Arrange
            var model = new PostPaymentRequest { Currency = currency };

            // Act
            var result = await _validator.TestValidateAsync(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Currency);
        }

        [Fact]
        public async Task ShouldHaveErrorWhenAmountIsZero()
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
        public async Task ShouldHaveErrorWhenCvvIsInvalid(int? cvv)
        {
            // Arrange
            var model = new PostPaymentRequest { Cvv = cvv ?? 0 };

            // Act
            var result = await _validator.TestValidateAsync(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Cvv);
        }

        [Fact]
        public async Task ShouldBeValidWhenAllFieldsAreCorrect()
        {
            // Arrange
            var model = new PostPaymentRequest
            {
                ExpiryMonth = 12,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Currency = "USD",
                Amount = 100,
                Cvv = 123
            };

            // Act
            var result = await _validator.ValidateAsync(model);

            // Assert
            result.IsValid.Should().BeTrue();
        }
    }
}
