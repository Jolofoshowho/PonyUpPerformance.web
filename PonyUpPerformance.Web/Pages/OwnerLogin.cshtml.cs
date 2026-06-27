using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Pages
{
    public class OwnerLoginModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _config;

        public OwnerLoginModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
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

            ApplicationUser? user = await _userManager.FindByEmailAsync(ownerEmail);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = ownerEmail,
                    Email = ownerEmail,
                    EmailConfirmed = true
                };

                IdentityResult result = await _userManager.CreateAsync(user);

                if (!result.Succeeded)
                {
                    ErrorMessage = "Could not create owner account.";
                    return Page();
                }
            }

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            await _signInManager.SignInAsync(user, isPersistent: true);

            return Redirect("/");
        }

        public class OwnerLoginInput
        {
            public string Email { get; set; } = "";
            public string OwnerToken { get; set; } = "";
        }
    }
}
