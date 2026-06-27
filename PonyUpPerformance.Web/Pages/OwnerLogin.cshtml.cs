using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace PonyUpPerformance.Web.Pages
{
    public class OwnerLoginModel : PageModel
    {
        private readonly IConfiguration _config;

        public OwnerLoginModel(IConfiguration config)
        {
            _config = config;
        }

        [BindProperty]
        public OwnerLoginInput Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            string ownerEmail = _config["OwnerLogin:Email"] ?? "";
            string ownerToken = _config["OwnerLogin:Token"] ?? "";

            if (string.IsNullOrWhiteSpace(ownerEmail) || string.IsNullOrWhiteSpace(ownerToken))
            {
                ErrorMessage = "Owner login is not configured.";
                return Page();
            }

            if (!string.Equals(Input.Email, ownerEmail, StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Invalid owner email.";
                return Page();
            }

            if (!string.Equals(Input.OwnerToken, ownerToken, StringComparison.Ordinal))
            {
                ErrorMessage = "Invalid owner token.";
                return Page();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "Owner"),
                new Claim(ClaimTypes.Email, ownerEmail),
                new Claim(ClaimTypes.NameIdentifier, ownerEmail)
            };

            var identity = new ClaimsIdentity(
                claims,
                IdentityConstants.ApplicationScheme);

            await HttpContext.SignInAsync(
                IdentityConstants.ApplicationScheme,
                new ClaimsPrincipal(identity));

            return Redirect("/");
        }

        public class OwnerLoginInput
        {
            public string Email { get; set; } = "";
            public string OwnerToken { get; set; } = "";
        }
    }
}
