using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PonyUpPerformance.Web.Models;
using PonyUpPerformance.Web.Services;

namespace PonyUpPerformance.Web.Pages;

[Authorize]
public sealed class BetaAccessModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly BetaAccessCodeService _betaAccessCodeService;
    private readonly UsageCreditService _usageCreditService;

    public BetaAccessModel(
        UserManager<ApplicationUser> userManager,
        BetaAccessCodeService betaAccessCodeService,
        UsageCreditService usageCreditService)
    {
        _userManager = userManager;
        _betaAccessCodeService = betaAccessCodeService;
        _usageCreditService = usageCreditService;
    }

    [BindProperty]
    public InputModel Input { get; set; } =
        new();

    public string Message { get; set; } =
        string.Empty;

    public bool Success { get; set; }

    public int RemainingCredits { get; set; }

    public sealed class InputModel
    {
        [Required]
        [StringLength(
            80,
            ErrorMessage = "Enter a valid beta access code.")]
        [Display(Name = "Beta Access Code")]
        public string Code { get; set; } =
            string.Empty;
    }

    public async Task OnGetAsync()
    {
        await LoadCreditsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadCreditsAsync();
            return Page();
        }

        ApplicationUser? user =
            await _userManager.GetUserAsync(
                User);

        if (user == null)
        {
            return Challenge();
        }

        BetaAccessRedeemResult result =
            await _betaAccessCodeService
                .RedeemAsync(
                    user,
                    Input.Code);

        Message =
            result.Message;

        Success =
            result.Status ==
            BetaAccessRedeemStatus.Applied;

        RemainingCredits =
            result.RemainingCredits;

        ModelState.Clear();

        return Page();
    }

    private async Task LoadCreditsAsync()
    {
        UsageCreditStatus status =
            await _usageCreditService
                .GetStatusAsync(
                    User);

        RemainingCredits =
            status.UnlimitedStandardAnalyses
                ? int.MaxValue
                : status.RemainingCredits;
    }
}
