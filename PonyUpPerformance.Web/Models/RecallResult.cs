namespace PonyUpPerformance.Web.Models
{
    public class RecallResult
    {
        public int OpenRecallCount { get; set; }

        public bool HasMajorSafetyRecall { get; set; }

        public string Status { get; set; } = "";

        public List<RecallItem> Recalls { get; set; } = new();

        public string Summary { get; set; } = "";
    }

    public class RecallItem
    {
        public string RecallNumber { get; set; } = "";

        public string Component { get; set; } = "";

        public string Summary { get; set; } = "";

        public bool IsSafetyRecall { get; set; }

        public bool IsOpen { get; set; }
    }
}
