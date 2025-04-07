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
    private readonly IPaymentsRepository _paymentsRepository;
    private readonly IPaymentsSimulator _paymentsSimulator;
    private readonly PostPaymentRequestValidator _paymentValidator;

    public PaymentsController(IPaymentsRepository paymentsRepository,
        IPaymentsSimulator paymentsSimulator,
        PostPaymentRequestValidator postPaymentRequestValidator)
    {
        _paymentsRepository = paymentsRepository;
        _paymentsSimulator = paymentsSimulator;
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

        try
        {
            // Submit to simulator and determine payment status
            var status = await _paymentsSimulator.SubmitAsync(paymentRequest);

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
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Simulator error: {ex.Message}");
        }
    }

}
