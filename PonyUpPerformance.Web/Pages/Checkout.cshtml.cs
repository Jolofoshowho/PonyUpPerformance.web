using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PonyUpPerformance.Web.Models;
using PonyUpPerformance.Web.Services;

namespace PonyUpPerformance.Web.Pages
{
    public class CheckoutModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly StripeCheckoutService _stripeCheckoutService;

        public CheckoutModel(
            UserManager<ApplicationUser> userManager,
            StripeCheckoutService stripeCheckoutService)
        {
            _userManager = userManager;
            _stripeCheckoutService = stripeCheckoutService;
        }

        public async Task<IActionResult> OnGetAsync(string plan)
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            string baseUrl = $"{Request.Scheme}://{Request.Host}";
            string checkoutUrl = await _stripeCheckoutService.CreateCheckoutUrlAsync(user, plan, baseUrl);

            return Redirect(checkoutUrl);
        }
    }
}