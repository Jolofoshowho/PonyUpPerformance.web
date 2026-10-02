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

        public async Task<IActionResult> OnGetAsync(
            string plan,
            string billing = "monthly")
        {
            if (string.IsNullOrWhiteSpace(
                    plan))
            {
                return RedirectToPage(
                    "/Pricing");
            }

            ApplicationUser? user =
                await _userManager.GetUserAsync(
                    User);

            if (user == null)
            {
                return RedirectToPage(
                    "/Account/Login",
                    new
                    {
                        area = "Identity"
                    });
            }

            string normalizedPlan =
                PonyUpPlanCatalog.NormalizeKey(
                    plan);

            if (normalizedPlan !=
                    PonyUpPlanCatalog.QuickPackKey &&
                !string.IsNullOrWhiteSpace(
                    user.ActiveStripeSubscriptionId))
            {
                string returnUrl =
                    $"{Request.Scheme}://{Request.Host}" +
                    "/Identity/Account/Manage";

                try
                {
                    string portalUrl =
                        await _stripeCheckoutService
                            .CreateCustomerPortalUrlAsync(
                                user,
                                returnUrl);

                    return Redirect(
                        portalUrl);
                }
                catch (InvalidOperationException)
                {
                    return RedirectToPage(
                        "/Pricing");
                }
            }

            string baseUrl =
                $"{Request.Scheme}://{Request.Host}";

            try
            {
                string checkoutUrl =
                    await _stripeCheckoutService
                        .CreateCheckoutUrlAsync(
                            user,
                            plan,
                            billing,
                            baseUrl);

                return Redirect(
                    checkoutUrl);
            }
            catch (InvalidOperationException)
            {
                return RedirectToPage(
                    "/Pricing");
            }
        }
    }
}
