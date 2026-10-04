using PonyUpPerformance.Web.Models;
using Xunit;

namespace PonyUpPerformance.Web.Tests;

public sealed class PlanCatalogTests
{
    [Fact]
    public void Free_HasBasicGarageOnly()
    {
        PonyUpPlanAccess plan =
            PonyUpPlanCatalog.Resolve(
                "free");

        Assert.True(plan.HasBasicGarage);
        Assert.False(plan.Has3DGarage);
        Assert.False(plan.CanCustomizeExterior);
        Assert.False(plan.UnlimitedStandardAnalyses);
    }

    [Fact]
    public void QuickPack_Unlocks3DAndExteriorOnly()
    {
        PonyUpPlanAccess plan =
            PonyUpPlanCatalog.Resolve(
                "quickpack");

        Assert.True(plan.Has3DGarage);
        Assert.True(plan.CanCustomizeExterior);
        Assert.False(plan.CanCustomizeInterior);
        Assert.False(plan.CanCustomizeWheels);
        Assert.False(plan.CanUseSpecialTrims);
    }

    [Fact]
    public void Pro_Unlocks3DAndExteriorOnly()
    {
        PonyUpPlanAccess plan =
            PonyUpPlanCatalog.Resolve(
                "pro");

        Assert.True(plan.Has3DGarage);
        Assert.True(plan.CanCustomizeExterior);
        Assert.False(plan.CanCustomizeInterior);
        Assert.False(plan.CanCustomizeWheels);
        Assert.False(plan.UnlimitedStandardAnalyses);
    }

    [Fact]
    public void FullThrottle_HasUnlimitedAnalysesAndFullGarage()
    {
        PonyUpPlanAccess plan =
            PonyUpPlanCatalog.Resolve(
                "fullthrottle");

        Assert.True(plan.UnlimitedStandardAnalyses);
        Assert.True(plan.HasFullGarage);
    }

    [Fact]
    public void LegacyUnlimited_MapsToFullThrottle()
    {
        PonyUpPlanAccess plan =
            PonyUpPlanCatalog.Resolve(
                "Unlimited");

        Assert.Equal(
            PonyUpPlanCatalog.FullThrottleKey,
            plan.Key);

        Assert.Equal(
            "Full Throttle",
            plan.DisplayName);
    }

    [Fact]
    public void Redline_IsFullGarageWithWorkingReportAllowance()
    {
        PonyUpPlanAccess plan =
            PonyUpPlanCatalog.Resolve(
                "redline");

        Assert.True(plan.HasFullGarage);
        Assert.True(plan.UnlimitedStandardAnalyses);
        Assert.Equal(
            5,
            plan.RevUpReportsPerBillingCycle);
        Assert.False(plan.UnlimitedRevUpReports);
    }

    [Fact]
    public void RedlinePlus_TargetsUnlimitedReports()
    {
        PonyUpPlanAccess plan =
            PonyUpPlanCatalog.Resolve(
                "redlineplus");

        Assert.True(plan.HasFullGarage);
        Assert.True(plan.UnlimitedStandardAnalyses);
        Assert.True(plan.UnlimitedRevUpReports);
    }

    [Fact]
    public void Owner_HasMaximumAccess()
    {
        PonyUpPlanAccess plan =
            PonyUpPlanCatalog.Resolve(
                "free",
                isOwner: true);

        Assert.Equal(
            PonyUpPlanCatalog.OwnerKey,
            plan.Key);

        Assert.True(plan.UnlimitedStandardAnalyses);
        Assert.True(plan.HasFullGarage);
        Assert.True(plan.UnlimitedRevUpReports);
    }

    [Fact]
    public void PriorQuickPackPurchase_PreservesPaidGarageAfterSubscriptionEnds()
    {
        PonyUpPlanAccess plan =
            PonyUpPlanCatalog.Resolve(
                "free",
                hasQuickPackPurchase: true);

        Assert.True(plan.Has3DGarage);
        Assert.True(plan.CanCustomizeExterior);
        Assert.False(plan.CanCustomizeInterior);
    }
}
