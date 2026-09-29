using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PonyUpPerformance.Web.Pages
{
    public class PricingModel : PageModel
    {
        public IActionResult OnGet()
        {
            return Redirect("/Pricing");
        }
    }
}
