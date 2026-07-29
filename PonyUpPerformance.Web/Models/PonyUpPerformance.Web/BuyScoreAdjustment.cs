namespace PonyUpPerformance.Web.Models
{
    public class BuyScoreAdjustment
    {
        public string Influencer { get; set; } = string.Empty;

        public int Adjustment { get; set; }

        public int MaximumAdjustment { get; set; }

        public string Explanation { get; set; } = string.Empty;
    }
}
