using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Validators;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly PaymentsRepository _paymentsRepository;
    private readonly PostPaymentRequestValidator _paymentValidator;

    public PaymentsController(PaymentsRepository paymentsRepository,
        PostPaymentRequestValidator postPaymentRequestValidator)
    {
        _paymentsRepository = paymentsRepository;
        _paymentValidator = postPaymentRequestValidator;
    }

    /// <summary>
    /// Retrieves a payment by ID.
    /// </summary>
    /// <param name="id">The GUID of the payment.</param>
    /// <returns>The matching payment if found, otherwise 404 Not Found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PostPaymentResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<PostPaymentResponse?> GetPayment(Guid id)
    {
        var payment = _paymentsRepository.Get(id);

        if (payment == null)
        {
            return new NotFoundObjectResult($"Payment with ID '{id}' was not found.");
        }

        return new OkObjectResult(payment);
    }

    /// <summary>
    /// Submits a payment request for processing.
    /// </summary>
    /// <param name="paymentRequest">The payment request payload.</param>
    /// <returns>The processed payment result with status and summary details.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PostPaymentResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SubmitPaymentRequest([FromBody] PostPaymentRequest paymentRequest)
    {
        // Validate the input model
        var validationResult = await _paymentValidator.ValidateAsync(paymentRequest);

        if (!validationResult.IsValid)
        {
            var errorMessage = $"Please provide all required fields for the payment request.\n" +
                               string.Join("\n", validationResult.Errors.Select(e => e.ErrorMessage));
            return new BadRequestObjectResult(errorMessage);
        }

        // Submit to simulator and determine payment status
        var status = await SubmitToSimulator(paymentRequest);

        // Prepare the response object
        var response = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = status,
            CardNumberLastFour = int.Parse(paymentRequest.CardNumber[^4..]),
            ExpiryMonth = paymentRequest.ExpiryMonth,
            ExpiryYear = paymentRequest.ExpiryYear,
            Currency = paymentRequest.Currency,
            Amount = paymentRequest.Amount
        };

        // Save the response to repository
        _paymentsRepository.Add(response);

        return new OkObjectResult(response);
    }

    /// <summary>
    /// Simulates payment processing by sending the request to an external simulator.
    /// </summary>
    /// <param name="request">The payment request to submit.</param>
    /// <returns>The payment status based on simulator response.</returns>
    /// <exception cref="HttpRequestException">Thrown when the simulator fails with a non-400 error.</exception>
    private static async Task<PaymentStatus> SubmitToSimulator(PostPaymentRequest request)
    {
        // Prepare payload in simulator's expected format
        var payload = new
        {
            card_number = request.CardNumber,
            expiry_date = $"{request.ExpiryMonth:D2}/{request.ExpiryYear}", // e.g., "04/2025"
            currency = request.Currency,
            amount = request.Amount,
            cvv = request.Cvv.ToString()
        };

        using var httpClient = new HttpClient();
        var response = await httpClient.PostAsJsonAsync("http://localhost:8080/payments", payload);

        // Special handling for 400 Bad Request (treated as rejected)
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return PaymentStatus.Rejected;
        }

        // Throw if any other non-successful status is returned
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Simulator responded with status {response.StatusCode}");
        }

        // Parse JSON response and extract "authorized" flag
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var isAuthorized = document.RootElement.GetProperty("authorized").GetBoolean();

        return isAuthorized ? PaymentStatus.Authorized : PaymentStatus.Declined;
    }
}
