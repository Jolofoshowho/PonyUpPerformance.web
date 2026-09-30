namespace PonyUpPerformance.Web.Models
{
    public class UsageCreditStatus
    {
        public bool IsLoggedIn { get; set; }

        public bool CanRunAnalysis { get; set; }

        public int RemainingCredits { get; set; }

        public int OneTimeCredits { get; set; }

        public int SubscriptionCredits { get; set; }

        public string CurrentPlan { get; set; } = "Free";

        public string PlanKey { get; set; } = PonyUpPlanCatalog.FreeKey;

        public bool UnlimitedStandardAnalyses { get; set; }

        public bool Has3DGarage { get; set; }

        public bool CanCustomizeExterior { get; set; }

        public bool CanCustomizeInterior { get; set; }

        public bool CanCustomizeWheels { get; set; }

        public bool CanUseSpecialTrims { get; set; }

        public int RevUpReportsPerBillingCycle { get; set; }

        public bool UnlimitedRevUpReports { get; set; }

        public string Message { get; set; } = "";
    }
}