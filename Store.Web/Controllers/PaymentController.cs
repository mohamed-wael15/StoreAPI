using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Store.service.services.BasketService.Dtos;
using Store.service.services.PaymentServices;
using Stripe;

namespace Store.Web.Controllers
{
   
    public class PaymentController : BaseController
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;
        private readonly string _endpointSecret;

        public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger, IConfiguration configuration)
        {
            _paymentService = paymentService;
            _logger = logger;
            _endpointSecret = configuration["Stripe:WebhookSecret"];
        }

        [HttpPost]
        public async Task<ActionResult<CustomerBasketDto>> CreateOrUpdatePaymentIntent(CustomerBasketDto input)
           => Ok(await _paymentService.CreateOrUpdatePaymentIntent(input));

        [HttpPost]
        public async Task<IActionResult> webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(json,
                    Request.Headers["Stripe-Signature"], _endpointSecret);

                PaymentIntent paymentIntent;

                // Handle the event
                if (stripeEvent.Type == "payment_intent.payment_failed")
                {
                    paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    _logger.LogInformation("Payment Failed :", paymentIntent.Id);
                    var order = await _paymentService.UpdateOrderPaymentFailed(paymentIntent.Id);
                    _logger.LogInformation("Order Updated to Payment Failed :", order.Id);
                }
                else if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    _logger.LogInformation("Payment Succeeded :", paymentIntent.Id);
                    var order = await _paymentService.UpdateOrderPaymentFailed(paymentIntent.Id);
                    _logger.LogInformation("Order Updated to Payment Succeeded :", order.Id);
                }

                else if (stripeEvent.Type == "payment_intent.created")
                {
                    _logger.LogInformation("Payment created :");
                }
            
                // ... handle other event types
                else
                {
                    Console.WriteLine("Unhandled event type: {0}", stripeEvent.Type);
                }

                return Ok();
            }
            catch (StripeException e)
            {
                return BadRequest();
            }
        }
    }
}
