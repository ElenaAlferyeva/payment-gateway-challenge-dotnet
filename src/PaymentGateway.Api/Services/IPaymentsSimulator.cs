using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models;

namespace PaymentGateway.Api.Services
{
    public interface IPaymentsSimulator
    {
        Task<PaymentStatus> SubmitAsync(PostPaymentRequest request);
    }
}
