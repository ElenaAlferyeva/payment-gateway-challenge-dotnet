# Instructions for candidates

This is the .NET version of the Payment Gateway challenge. If you haven't already read this [README.md](https://github.com/cko-recruitment/) on the details of this exercise, please do so now. 

## Template structure
```
src/
    PaymentGateway.Api - a skeleton ASP.NET Core Web API
test/
    PaymentGateway.Api.Tests - an empty xUnit test project
imposters/ - contains the bank simulator configuration. Don't change this

.editorconfig - don't change this. It ensures a consistent set of rules for submissions when reformatting code
docker-compose.yml - configures the bank simulator
PaymentGateway.sln
```

Feel free to change the structure of the solution, use a different test library etc.


## Assumptions and Notes

- The `PostPaymentRequest` model originally contained `public int CardNumberLastFour`. Since the full card number needs to be posted (as per the spec), I updated it to `public string CardNumber`.
- I used interfaces (`IPaymentsRepository` and `IPaymentsSimulator`) to follow good design practices and make testing easier with mocks.
- The PostPaymentRequestValidator is used for model validation.
- I validate that the expiry date is in the future using **UTC time**. Time zones or local time were **not** considered for simplicity and consistency.
- Authorization logic was left untouched, as the task did not specify any changes in that area.