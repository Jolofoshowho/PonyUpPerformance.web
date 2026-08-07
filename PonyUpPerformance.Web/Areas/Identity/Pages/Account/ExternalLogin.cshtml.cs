
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Areas.Identity.Pages.Account
{
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ExternalLoginModel> _logger;

        public ExternalLoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<ExternalLoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string ProviderDisplayName { get; set; } = string.Empty;

        public string ReturnUrl { get; set; } = string.Empty;

        [TempData]
        public string ErrorMessage { get; set; } = string.Empty;

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;
        }

        public IActionResult OnPost(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Page(
                "./ExternalLogin",
                pageHandler: "Callback",
                values: new { returnUrl });

            var properties =
                _signInManager.ConfigureExternalAuthenticationProperties(
                    provider,
                    redirectUrl!);

            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(
            string? returnUrl = null,
            string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!string.IsNullOrEmpty(remoteError))
            {
                ErrorMessage =
                    $"External provider error: {remoteError}";

                return RedirectToPage("./Login");
            }

            var info =
                await _signInManager.GetExternalLoginInfoAsync();

            if (info == null)
            {
                ErrorMessage =
                    "Unable to load external login information.";

                return RedirectToPage("./Login");
            }

            var result =
                await _signInManager.ExternalLoginSignInAsync(
                    info.LoginProvider,
                    info.ProviderKey,
                    isPersistent: false,
                    bypassTwoFactor: true);

            if (result.Succeeded)
            {
                _logger.LogInformation(
                    "User logged in with {Provider}.",
                    info.LoginProvider);

                return LocalRedirect(returnUrl);
            }

            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }

            var email =
                info.Principal.FindFirstValue(
                    ClaimTypes.Email);

            if (!string.IsNullOrWhiteSpace(email))
            {
                email = email.Trim();

                var existingUser =
                    await _userManager.FindByEmailAsync(email);

                /*
                 * Existing PonyUp account:
                 * attach Google/Facebook to it instead
                 * of attempting to create a duplicate account.
                 */
                if (existingUser != null)
                {
                    var addLoginResult =
                        await _userManager.AddLoginAsync(
                            existingUser,
                            info);

                    if (!addLoginResult.Succeeded)
                    {
                        foreach (var error in addLoginResult.Errors)
                        {
                            ModelState.AddModelError(
                                string.Empty,
                                error.Description);
                        }

                        ProviderDisplayName =
                            info.ProviderDisplayName ??
                            info.LoginProvider;

                        Input.Email = email;
                        ReturnUrl = returnUrl;

                        return Page();
                    }

                    await _signInManager.SignInAsync(
                        existingUser,
                        isPersistent: false,
                        authenticationMethod:
                            info.LoginProvider);

                    _logger.LogInformation(
                        "{Provider} was linked to an existing PonyUp account.",
                        info.LoginProvider);

                    return LocalRedirect(returnUrl);
                }

                /*
                 * No PonyUp account exists for this email:
                 * create one automatically.
                 */
                var newUser = new ApplicationUser();

                await _userManager.SetUserNameAsync(
                    newUser,
                    email);

                await _userManager.SetEmailAsync(
                    newUser,
                    email);

                var createResult =
                    await _userManager.CreateAsync(newUser);

                if (createResult.Succeeded)
                {
                    var addLoginResult =
                        await _userManager.AddLoginAsync(
                            newUser,
                            info);

                    if (addLoginResult.Succeeded)
                    {
                        await _signInManager.SignInAsync(
                            newUser,
                            isPersistent: false,
                            authenticationMethod:
                                info.LoginProvider);

                        _logger.LogInformation(
                            "New user created and logged in with {Provider}.",
                            info.LoginProvider);

                        return LocalRedirect(returnUrl);
                    }

                    foreach (var error in addLoginResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description);
                    }
                }
                else
                {
                    foreach (var error in createResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description);
                    }
                }
            }

            /*
             * Fallback only if the provider did not
             * return an email address.
             */
            ProviderDisplayName =
                info.ProviderDisplayName ??
                info.LoginProvider;

            Input.Email = email ?? string.Empty;
            ReturnUrl = returnUrl;

            return Page();
        }

        public async Task<IActionResult> OnPostConfirmationAsync(
            string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            var info =
                await _signInManager.GetExternalLoginInfoAsync();

            if (info == null)
            {
                ErrorMessage =
                    "Unable to load external login information.";

                return RedirectToPage("./Login");
            }

            ProviderDisplayName =
                info.ProviderDisplayName ??
                info.LoginProvider;

            ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var email = Input.Email.Trim();

            var user =
                await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser();

                await _userManager.SetUserNameAsync(
                    user,
                    email);

                await _userManager.SetEmailAsync(
                    user,
                    email);

                var createResult =
                    await _userManager.CreateAsync(user);

                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description);
                    }

                    return Page();
                }
            }

            var addLoginResult =
                await _userManager.AddLoginAsync(
                    user,
                    info);

            if (!addLoginResult.Succeeded)
            {
                foreach (var error in addLoginResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return Page();
            }

            await _signInManager.SignInAsync(
                user,
                isPersistent: false,
                authenticationMethod:
                    info.LoginProvider);

            _logger.LogInformation(
                "User logged in with {Provider}.",
                info.LoginProvider);

            return LocalRedirect(returnUrl);
        }
    }
}
