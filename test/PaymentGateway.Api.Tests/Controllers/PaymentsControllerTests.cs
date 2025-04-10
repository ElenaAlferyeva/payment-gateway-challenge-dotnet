using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Validators;

namespace PaymentGateway.Api.Tests.Controllers
{
    public class PaymentsControllerTests
    {
        private readonly PaymentsController _controller;
        private readonly Mock<IPaymentsRepository> _repositoryMock;
        private readonly Mock<IPaymentsSimulator> _simulatorMock;
        private readonly PostPaymentRequestValidator _validator;
        private readonly Random _random = new();

        public PaymentsControllerTests()
        {
            _repositoryMock = new Mock<IPaymentsRepository>();
            _simulatorMock = new Mock<IPaymentsSimulator>();
            _validator = new PostPaymentRequestValidator();
            _controller = new PaymentsController(_repositoryMock.Object, _simulatorMock.Object, _validator);
        }

        #region GET /payments/{id}

        [Fact]
        public void GetPayment_ShouldReturnNotFound_WhenPaymentDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.Get(id)).Returns((PostPaymentResponse?)null);

            // Act
            var result = _controller.GetPayment(id);

            // Assert
            var response = result.Result as NotFoundObjectResult;
            response.Should().NotBeNull();
            response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }

        [Fact]
        public void GetPayment_ShouldReturnOk_WhenPaymentExists()
        {
            // Arrange
            var payment = CreateRandomResponse();
            _repositoryMock.Setup(r => r.Get(payment.Id)).Returns(payment);

            // Act
            var result = _controller.GetPayment(payment.Id);

            // Assert
            var response = result.Result as OkObjectResult;
            response.Should().NotBeNull();
            response.StatusCode.Should().Be((int)HttpStatusCode.OK);
            response.Value.Should().BeEquivalentTo(payment);
        }

        #endregion

        #region POST /payments

        [Fact]
        public async Task SubmitPaymentRequest_ShouldReturnBadRequest_WhenRequestIsInvalid()
        {
            // Arrange
            var invalidRequest = new PostPaymentRequest(); // Missing all required fields

            // Act
            var result = await _controller.SubmitPaymentRequest(invalidRequest);

            // Assert
            var response = result as BadRequestObjectResult;
            response.Should().NotBeNull();
            response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SubmitPaymentRequest_ShouldReturnOk_WhenRequestIsValid()
        {
            // Arrange
            var request = CreateValidRequest();
            _simulatorMock.Setup(s => s.SubmitAsync(It.IsAny<PostPaymentRequest>()))
                          .ReturnsAsync(PaymentStatus.Authorized);

            // Act
            var result = await _controller.SubmitPaymentRequest(request);

            // Assert
            var response = result as OkObjectResult;
            var value = response?.Value as PostPaymentResponse;

            response.Should().NotBeNull();
            response.StatusCode.Should().Be((int)HttpStatusCode.OK);
            value.Should().NotBeNull();
            value!.Status.Should().Be(PaymentStatus.Authorized);
            value.CardNumberLastFour.Should().Be(int.Parse(request.CardNumber[^4..]));
        }

        [Theory]
        [InlineData(PaymentStatus.Authorized)]
        [InlineData(PaymentStatus.Declined)]
        public async Task SubmitPaymentRequest_ShouldReturnExpectedStatus(PaymentStatus status)
        {
            // Arrange
            var request = CreateValidRequest();
            _simulatorMock.Setup(s => s.SubmitAsync(It.IsAny<PostPaymentRequest>()))
                          .ReturnsAsync(status);

            // Act
            var result = await _controller.SubmitPaymentRequest(request);

            // Assert
            var response = result as OkObjectResult;
            var value = response?.Value as PostPaymentResponse;

            response.Should().NotBeNull();
            value.Should().NotBeNull();
            value!.Status.Should().Be(status);
        }

        [Fact]
        public async Task SubmitPaymentRequest_ShouldReturnRejectedStatus_WhenSimulatorReturnsBadRequest()
        {
            // Arrange
            var request = CreateValidRequest();
            _simulatorMock.Setup(s => s.SubmitAsync(It.IsAny<PostPaymentRequest>()))
                          .ReturnsAsync(PaymentStatus.Rejected);

            // Act
            var result = await _controller.SubmitPaymentRequest(request);

            // Assert
            var response = result as OkObjectResult;
            var value = response?.Value as PostPaymentResponse;

            response.Should().NotBeNull();
            value.Should().NotBeNull();
            value!.Status.Should().Be(PaymentStatus.Rejected);
        }

        [Fact]
        public async Task SubmitPaymentRequest_ShouldReturnInternalServerError_WhenSimulatorThrows()
        {
            // Arrange
            var request = CreateValidRequest();
            _simulatorMock.Setup(s => s.SubmitAsync(It.IsAny<PostPaymentRequest>()))
                          .ThrowsAsync(new HttpRequestException("Simulator error"));

            // Act
            var result = await _controller.SubmitPaymentRequest(request);

            // Assert
            var response = result as ObjectResult;
            response.Should().NotBeNull();
            response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
            response.Value.Should().Be("Simulator error: Simulator error");
        }

        #endregion

        #region Helpers

        private PostPaymentRequest CreateValidRequest() => new()
        {
            CardNumber = string.Concat(Enumerable.Range(0, 16).Select(_ => _random.Next(0, 10))),
            ExpiryMonth = _random.Next(1, 13),
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = _random.Next(100, 10000),
            Cvv = _random.Next(100, 1000)
        };

        private PostPaymentResponse CreateRandomResponse() => new()
        {
            Id = Guid.NewGuid(),
            ExpiryYear = _random.Next(2023, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumberLastFour = _random.Next(1000, 9999),
            Currency = "GBP",
            Status = PaymentStatus.Authorized
        };

        #endregion
    }
}
