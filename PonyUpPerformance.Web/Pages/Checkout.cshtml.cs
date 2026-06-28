using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Stripe.Checkout;

namespace PonyUpPerformance.Web.Pages
{
    public class CheckoutModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public CheckoutModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult OnGet(string plan)
        {
            var priceId = plan?.ToLower() switch
            {
                "quickpack" => _configuration["Stripe:QuickPackPriceId"],
                "pro" => _configuration["Stripe:ProPriceId"],
                "unlimited" => _configuration["Stripe:UnlimitedPriceId"],
                _ => null
            };

            if (string.IsNullOrWhiteSpace(priceId))
            {
                return RedirectToPage("/Pricing");
            }

            var domain = $"{Request.Scheme}://{Request.Host}";

            var options = new SessionCreateOptions
            {
                Mode = plan?.ToLower() == "quickpack" ? "payment" : "subscription",
                SuccessUrl = $"{domain}/CheckoutSuccess",
                CancelUrl = $"{domain}/Pricing",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1
                    }
                }
            };

            var service = new SessionService();
            var session = service.Create(options);

            return Redirect(session.Url);
        }
    }
}
