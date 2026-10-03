namespace PonyUpPerformance.Web.Models;

public sealed record PonyUpPlanAccess
{
    public string Key { get; init; } = PonyUpPlanCatalog.FreeKey;
    public string DisplayName { get; init; } = "Free";

    public bool UnlimitedStandardAnalyses { get; init; }

    public bool HasBasicGarage { get; init; } = true;
    public bool Has3DGarage { get; init; }
    public bool CanCustomizeExterior { get; init; }
    public bool CanCustomizeInterior { get; init; }
    public bool CanCustomizeWheels { get; init; }
    public bool CanUseSpecialTrims { get; init; }

    public int RevUpReportsPerBillingCycle { get; init; }
    public bool UnlimitedRevUpReports { get; init; }

    public bool HasFullGarage =>
        Has3DGarage &&
        CanCustomizeExterior &&
        CanCustomizeInterior &&
        CanCustomizeWheels &&
        CanUseSpecialTrims;
}

public static class PonyUpPlanCatalog
{
    public const string FreeKey = "free";
    public const string QuickPackKey = "quickpack";
    public const string ProKey = "pro";
    public const string FullThrottleKey = "fullthrottle";
    public const string RedlineKey = "redline";
    public const string RedlinePlusKey = "redlineplus";
    public const string OwnerKey = "owner";

    public static PonyUpPlanAccess Resolve(
        string? plan,
        bool isOwner = false,
        bool hasQuickPackPurchase = false)
    {
        if (isOwner)
        {
            return new PonyUpPlanAccess
            {
                Key = OwnerKey,
                DisplayName = "Owner",
                UnlimitedStandardAnalyses = true,
                HasBasicGarage = true,
                Has3DGarage = true,
                CanCustomizeExterior = true,
                CanCustomizeInterior = true,
                CanCustomizeWheels = true,
                CanUseSpecialTrims = true,
                UnlimitedRevUpReports = true
            };
        }

        PonyUpPlanAccess access =
            NormalizeKey(plan) switch
            {
                QuickPackKey => QuickPack(),
                ProKey => Pro(),
                FullThrottleKey => FullThrottle(),
                RedlineKey => Redline(),
                RedlinePlusKey => RedlinePlus(),
                _ => Free()
            };

        /*
         * Quick Pack is a one-time purchase. Its paid Garage access
         * remains available even after the five analysis credits are used
         * or a later subscription ends.
         */
        if (hasQuickPackPurchase &&
            !access.Has3DGarage)
        {
            access = access with
            {
                Has3DGarage = true,
                CanCustomizeExterior = true
            };
        }

        return access;
    }

    public static string NormalizeKey(
        string? plan)
    {
        string value =
            (plan ?? string.Empty)
                .Trim()
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace("_", string.Empty)
                .ToLowerInvariant();

        return value switch
        {
            "quickpack" => QuickPackKey,
            "pro" => ProKey,

            // Backward compatibility for the old plan name.
            "unlimited" => FullThrottleKey,
            "fullthrottle" => FullThrottleKey,

            "redline" => RedlineKey,
            "redline+" => RedlinePlusKey,
            "redlineplus" => RedlinePlusKey,
            "owner" => OwnerKey,

            _ => FreeKey
        };
    }

    public static string DisplayName(
        string? plan)
    {
        return Resolve(plan).DisplayName;
    }

    public static bool IsUnlimitedStandardAnalysisPlan(
        string? plan)
    {
        return Resolve(plan).UnlimitedStandardAnalyses;
    }

    private static PonyUpPlanAccess Free() =>
        new()
        {
            Key = FreeKey,
            DisplayName = "Free",
            HasBasicGarage = true
        };

    private static PonyUpPlanAccess QuickPack() =>
        new()
        {
            Key = QuickPackKey,
            DisplayName = "Quick Pack",
            HasBasicGarage = true,
            Has3DGarage = true,
            CanCustomizeExterior = true
        };

    private static PonyUpPlanAccess Pro() =>
        new()
        {
            Key = ProKey,
            DisplayName = "Pro",
            HasBasicGarage = true,
            Has3DGarage = true,
            CanCustomizeExterior = true
        };

    private static PonyUpPlanAccess FullThrottle() =>
        new()
        {
            Key = FullThrottleKey,
            DisplayName = "Full Throttle",
            UnlimitedStandardAnalyses = true,
            HasBasicGarage = true,
            Has3DGarage = true,
            CanCustomizeExterior = true,
            CanCustomizeInterior = true,
            CanCustomizeWheels = true,
            CanUseSpecialTrims = true
        };

    private static PonyUpPlanAccess Redline() =>
        new()
        {
            Key = RedlineKey,
            DisplayName = "Redline",
            UnlimitedStandardAnalyses = true,
            HasBasicGarage = true,
            Has3DGarage = true,
            CanCustomizeExterior = true,
            CanCustomizeInterior = true,
            CanCustomizeWheels = true,
            CanUseSpecialTrims = true,

            // Working allowance. Redline remains non-purchasable until
            // external report pricing is finalized.
            RevUpReportsPerBillingCycle = 5
        };

    private static PonyUpPlanAccess RedlinePlus() =>
        new()
        {
            Key = RedlinePlusKey,
            DisplayName = "Redline+",
            UnlimitedStandardAnalyses = true,
            HasBasicGarage = true,
            Has3DGarage = true,
            CanCustomizeExterior = true,
            CanCustomizeInterior = true,
            CanCustomizeWheels = true,
            CanUseSpecialTrims = true,

            // Planned entitlement only. Do not expose as purchasable until
            // the paid data-provider economics are locked.
            UnlimitedRevUpReports = true
        };
}
