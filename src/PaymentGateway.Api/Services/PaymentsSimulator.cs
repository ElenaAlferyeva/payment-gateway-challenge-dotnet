using System.Net;
using System.Text.Json;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using Microsoft.Extensions.Logging;

namespace PaymentGateway.Api.Services
{
    public class PaymentsSimulator(ILogger<PaymentsSimulator> logger) : IPaymentsSimulator
    {
        public async Task<PaymentStatus> SubmitAsync(PostPaymentRequest request)
        {
            var payload = new
            {
                card_number = request.CardNumber,
                expiry_date = $"{request.ExpiryMonth:D2}/{request.ExpiryYear}",
                currency = request.Currency,
                amount = request.Amount,
                cvv = request.Cvv.ToString()
            };

            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsJsonAsync("http://localhost:8080/payments", payload);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return PaymentStatus.Rejected;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Simulator responded with status {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);
            var isAuthorized = document.RootElement.GetProperty("authorized").GetBoolean();

            return isAuthorized ? PaymentStatus.Authorized : PaymentStatus.Declined;
        }
    }
}
