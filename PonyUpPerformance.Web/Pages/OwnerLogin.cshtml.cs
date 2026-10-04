using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Pages
{
    public class OwnerLoginModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public OwnerLoginModel(
            IConfiguration config,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _config = config;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public OwnerLoginInput Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            string ownerEmail =
                _config["OwnerLogin:Email"]
                ?? string.Empty;

            string ownerToken =
                _config["OwnerLogin:Token"]
                ?? string.Empty;

            if (!ModelState.IsValid ||
                string.IsNullOrWhiteSpace(
                    ownerEmail) ||
                string.IsNullOrWhiteSpace(
                    ownerToken) ||
                !string.Equals(
                    Input.Email?.Trim(),
                    ownerEmail,
                    StringComparison.OrdinalIgnoreCase) ||
                !TokenMatches(
                    Input.OwnerToken,
                    ownerToken))
            {
                ErrorMessage =
                    "Owner login failed.";

                return Page();
            }

            ApplicationUser? user =
                await _userManager.FindByEmailAsync(
                    ownerEmail);

            if (user == null)
            {
                ErrorMessage =
                    "Owner login failed.";

                return Page();
            }

            await _signInManager.SignInAsync(
                user,
                isPersistent: false);

            return LocalRedirect(
                Url.Content("~/"));
        }

        private static bool TokenMatches(
            string? providedToken,
            string expectedToken)
        {
            byte[] providedHash =
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(
                        providedToken
                        ?? string.Empty));

            byte[] expectedHash =
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(
                        expectedToken));

            return CryptographicOperations
                .FixedTimeEquals(
                    providedHash,
                    expectedHash);
        }

        public class OwnerLoginInput
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = "";

            [Required]
            [DataType(DataType.Password)]
            public string OwnerToken { get; set; } = "";
        }
    }
}
