using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Pages
{
    public class BuyAnalyzerModel : PageModel
    {
        [BindProperty]
        public BuyDecisionInput Input { get; set; } = new();

        public BuyDecisionResult? Result { get; set; }

        public void OnGet()
        {
        }

        public void OnPost()
        {
            int totalInvestmentScore = ScoreTotalInvestment();
            int askingPriceScore = ScoreAskingPrice();
            int repairCostScore = ScoreRepairCost();
            int mileageScore = ScoreMileage();
            int vehicleAgeScore = ScoreVehicleAge();

            int totalScore =
                totalInvestmentScore +
                askingPriceScore +
                repairCostScore +
                mileageScore +
                vehicleAgeScore;

            totalScore = Math.Clamp(totalScore, 0, 100);

            int confidenceScore = CalculateConfidenceScore();

            decimal maximumRecommendedPrice = CalculateMaximumRecommendedPrice();
            decimal fairPurchasePrice = CalculateFairPurchasePrice(maximumRecommendedPrice);
            decimal recommendedFirstOffer = CalculateRecommendedFirstOffer(fairPurchasePrice);
            decimal negotiatingRoom = Math.Max(0, Input.AskingPrice - recommendedFirstOffer);

            string recommendation =
                totalScore >= 75
                    ? "PONY UP"
                    : totalScore >= 50
                        ? "NEGOTIATE"
                        : "WALK AWAY";

            string purchaseRisk =
                totalScore >= 75
                    ? "LOW"
                    : totalScore >= 50
                        ? "MODERATE"
                        : "HIGH";

            string pricePosition = BuildPricePosition();
            string mileageAssessment = BuildMileageAssessment();

            List<string> strengths = BuildStrengths();
            List<string> concerns = BuildConcerns();
            List<string> nextSteps = BuildNextSteps(recommendation, maximumRecommendedPrice);

            string reasoning = BuildReasoning(
                recommendation,
                totalInvestmentScore,
                askingPriceScore,
                repairCostScore,
                mileageScore,
                vehicleAgeScore);

            string riskSummary = BuildRiskSummary(
                purchaseRisk,
                totalScore,
                totalInvestmentScore,
                repairCostScore,
                mileageScore);

            Result = new BuyDecisionResult
            {
                Recommendation = recommendation,
                Score = totalScore,
                ConfidenceScore = confidenceScore,
                PurchaseRisk = purchaseRisk,
                PonyUpFairPurchasePrice = fairPurchasePrice,
                RecommendedFirstOffer = recommendedFirstOffer,
                MaximumRecommendedPrice = maximumRecommendedPrice,
                NegotiatingRoom = negotiatingRoom,
                PricePosition = pricePosition,
                MileageAssessment = mileageAssessment,
                RiskSummary = riskSummary,
                Reasoning = reasoning,
                Strengths = strengths,
                Concerns = concerns,
                NextSteps = nextSteps
            };
        }

        private int ScoreTotalInvestment()
        {
            if (Input.MarketValue <= 0)
                return 0;

            decimal totalInvestment =
                Input.AskingPrice + Input.EstimatedRepairCost;

            decimal ratio = totalInvestment / Input.MarketValue;

            if (ratio <= 0.75m) return 35;
            if (ratio <= 0.85m) return 31;
            if (ratio <= 0.95m) return 26;
            if (ratio <= 1.00m) return 20;
            if (ratio <= 1.10m) return 12;
            if (ratio <= 1.20m) return 6;

            return 0;
        }

        private int ScoreAskingPrice()
        {
            if (Input.MarketValue <= 0)
                return 0;

            decimal ratio = Input.AskingPrice / Input.MarketValue;

            if (ratio <= 0.70m) return 25;
            if (ratio <= 0.80m) return 23;
            if (ratio <= 0.90m) return 20;
            if (ratio <= 1.00m) return 16;
            if (ratio <= 1.10m) return 9;
            if (ratio <= 1.20m) return 4;

            return 0;
        }

        private int ScoreRepairCost()
        {
            if (Input.EstimatedRepairCost <= 0)
                return 20;

            if (Input.MarketValue > 0)
            {
                decimal ratio =
                    Input.EstimatedRepairCost / Input.MarketValue;

                if (ratio <= 0.05m) return 20;
                if (ratio <= 0.10m) return 17;
                if (ratio <= 0.20m) return 13;
                if (ratio <= 0.30m) return 8;
                if (ratio <= 0.40m) return 4;

                return 0;
            }

            if (Input.EstimatedRepairCost <= 500) return 17;
            if (Input.EstimatedRepairCost <= 1000) return 13;
            if (Input.EstimatedRepairCost <= 2500) return 7;

            return 2;
        }

        private int ScoreMileage()
        {
            if (Input.Mileage <= 0)
                return 0;

            if (Input.Mileage <= 50000) return 10;
            if (Input.Mileage <= 100000) return 8;
            if (Input.Mileage <= 150000) return 6;
            if (Input.Mileage <= 200000) return 3;

            return 1;
        }

        private int ScoreVehicleAge()
        {
            if (Input.Year <= 0)
                return 0;

            int currentYear = DateTime.UtcNow.Year;
            int age = Math.Max(0, currentYear - Input.Year);

            if (age <= 5) return 10;
            if (age <= 10) return 8;
            if (age <= 15) return 6;
            if (age <= 20) return 4;
            if (age <= 25) return 2;

            return 1;
        }

        private int CalculateConfidenceScore()
        {
            int confidence = 0;

            if (Input.Year >= 1900 &&
                Input.Year <= DateTime.UtcNow.Year + 1)
            {
                confidence += 15;
            }

            if (!string.IsNullOrWhiteSpace(Input.Make))
                confidence += 10;

            if (!string.IsNullOrWhiteSpace(Input.Model))
                confidence += 10;

            if (Input.Mileage > 0)
                confidence += 15;

            if (Input.AskingPrice > 0)
                confidence += 20;

            if (Input.MarketValue > 0)
                confidence += 20;

            if (Input.EstimatedRepairCost >= 0)
                confidence += 10;

            return Math.Clamp(confidence, 0, 100);
        }

        private decimal CalculateMaximumRecommendedPrice()
        {
            if (Input.MarketValue <= 0)
                return 0;

            decimal mileageReserve = Input.Mileage switch
            {
                > 200000 => Input.MarketValue * 0.20m,
                > 150000 => Input.MarketValue * 0.15m,
                > 100000 => Input.MarketValue * 0.10m,
                _ => Input.MarketValue * 0.05m
            };

            decimal repairReserve =
                Math.Max(0, Input.EstimatedRepairCost);

            decimal maximumPrice =
                Input.MarketValue -
                repairReserve -
                mileageReserve;

            return Math.Round(
                Math.Max(0, maximumPrice),
                2);
        }

        private decimal CalculateFairPurchasePrice(
            decimal maximumRecommendedPrice)
        {
            if (maximumRecommendedPrice <= 0)
                return 0;

            decimal fairPrice =
                maximumRecommendedPrice * 0.95m;

            return Math.Round(fairPrice, 2);
        }

        private decimal CalculateRecommendedFirstOffer(
            decimal fairPurchasePrice)
        {
            if (fairPurchasePrice <= 0)
                return 0;

            decimal firstOffer =
                fairPurchasePrice * 0.90m;

            return Math.Round(firstOffer, 2);
        }

        private string BuildPricePosition()
        {
            if (Input.MarketValue <= 0)
                return "Market value was not provided.";

            decimal difference =
                Input.MarketValue - Input.AskingPrice;

            decimal percentage =
                Math.Abs(difference) / Input.MarketValue;

            if (Input.AskingPrice <= Input.MarketValue * 0.85m)
            {
                return
                    $"The asking price is approximately {percentage:P0} below the estimated market value.";
            }

            if (Input.AskingPrice <= Input.MarketValue)
            {
                return
                    "The asking price is below market value, but the repair cost and mileage still need to be considered.";
            }

            if (Input.AskingPrice <= Input.MarketValue * 1.10m)
            {
                return
                    "The asking price is slightly above market value and should be negotiated.";
            }

            return
                $"The asking price is approximately {percentage:P0} above the estimated market value.";
        }

        private string BuildMileageAssessment()
        {
            if (Input.Mileage <= 0)
                return "Mileage was not provided.";

            if (Input.Mileage <= 50000)
                return "Very low mileage for a used vehicle.";

            if (Input.Mileage <= 100000)
                return "Moderate mileage with reasonable remaining service life.";

            if (Input.Mileage <= 150000)
                return "Higher mileage that increases maintenance and repair exposure.";

            if (Input.Mileage <= 200000)
                return "Very high mileage with increased mechanical and ownership risk.";

            return
                "Extremely high mileage. Major component wear and future repair risk should be expected.";
        }

        private List<string> BuildStrengths()
        {
            List<string> strengths = new();

            if (Input.MarketValue > 0 &&
                Input.AskingPrice <= Input.MarketValue * 0.90m)
            {
                strengths.Add(
                    "The asking price is meaningfully below estimated market value.");
            }

            if (Input.MarketValue > 0 &&
                Input.AskingPrice + Input.EstimatedRepairCost
                    <= Input.MarketValue)
            {
                strengths.Add(
                    "The purchase price plus known repairs remains within estimated market value.");
            }

            if (Input.EstimatedRepairCost <= 500)
            {
                strengths.Add(
                    "Estimated immediate repair exposure is relatively low.");
            }

            if (Input.Mileage > 0 &&
                Input.Mileage <= 100000)
            {
                strengths.Add(
                    "Mileage is within a favorable range for a used vehicle.");
            }

            if (Input.Year >= DateTime.UtcNow.Year - 10)
            {
                strengths.Add(
                    "The vehicle is relatively recent.");
            }

            if (strengths.Count == 0)
            {
                strengths.Add(
                    "No major financial strength was identified from the entered information.");
            }

            return strengths;
        }

        private List<string> BuildConcerns()
        {
            List<string> concerns = new();

            if (Input.MarketValue <= 0)
            {
                concerns.Add(
                    "Market value was not provided, reducing pricing accuracy.");
            }
            else
            {
                decimal totalInvestment =
                    Input.AskingPrice +
                    Input.EstimatedRepairCost;

                if (totalInvestment > Input.MarketValue)
                {
                    concerns.Add(
                        "The asking price plus estimated repairs exceeds the vehicle's estimated market value.");
                }

                if (Input.AskingPrice > Input.MarketValue)
                {
                    concerns.Add(
                        "The seller is asking more than the estimated market value.");
                }
            }

            if (Input.EstimatedRepairCost > 2500)
            {
                concerns.Add(
                    "The vehicle requires substantial immediate repair investment.");
            }

            if (Input.Mileage > 150000)
            {
                concerns.Add(
                    "High mileage increases the risk of additional repairs after purchase.");
            }

            int age =
                Input.Year > 0
                    ? DateTime.UtcNow.Year - Input.Year
                    : 0;

            if (age > 20)
            {
                concerns.Add(
                    "Vehicle age increases the likelihood of age-related mechanical, electrical, and structural issues.");
            }

            if (concerns.Count == 0)
            {
                concerns.Add(
                    "No major financial concern was identified from the entered information.");
            }

            return concerns;
        }

        private List<string> BuildNextSteps(
            string recommendation,
            decimal maximumRecommendedPrice)
        {
            List<string> nextSteps = new()
            {
                "Verify the title, VIN, ownership history, and lien status.",
                "Have the vehicle inspected before purchase.",
                "Confirm the repair estimate with a qualified shop."
            };

            if (recommendation == "PONY UP")
            {
                nextSteps.Add(
                    $"Do not exceed approximately {maximumRecommendedPrice:C} without new supporting information.");
            }
            else if (recommendation == "NEGOTIATE")
            {
                nextSteps.Add(
                    $"Use the repair cost and mileage to negotiate below approximately {maximumRecommendedPrice:C}.");
            }
            else
            {
                nextSteps.Add(
                    "Walk away unless the seller significantly reduces the price or the repair risk is proven lower.");
            }

            return nextSteps;
        }

        private string BuildReasoning(
            string recommendation,
            int totalInvestmentScore,
            int askingPriceScore,
            int repairCostScore,
            int mileageScore,
            int vehicleAgeScore)
        {
            List<string> strongestFactors = new();

            if (Input.MarketValue > 0)
            {
                decimal totalInvestment =
                    Input.AskingPrice +
                    Input.EstimatedRepairCost;

                decimal investmentRatio =
                    totalInvestment / Input.MarketValue;

                strongestFactors.Add(
                    $"the total projected investment is approximately {investmentRatio:P0} of estimated market value");
            }

            if (repairCostScore <= 8)
            {
                strongestFactors.Add(
                    "the immediate repair exposure is substantial");
            }
            else if (repairCostScore >= 17)
            {
                strongestFactors.Add(
                    "the immediate repair exposure is relatively low");
            }

            if (mileageScore <= 3)
            {
                strongestFactors.Add(
                    "the mileage creates elevated future repair risk");
            }
            else if (mileageScore >= 8)
            {
                strongestFactors.Add(
                    "the mileage is favorable for a used vehicle");
            }

            if (vehicleAgeScore <= 2)
            {
                strongestFactors.Add(
                    "vehicle age increases the likelihood of additional problems");
            }

            string factorText =
                strongestFactors.Count > 0
                    ? string.Join(", while ", strongestFactors.Take(3))
                    : "the available information provides limited financial support";

            return recommendation switch
            {
                "PONY UP" =>
                    $"The purchase appears financially reasonable because {factorText}. Complete a title check and independent inspection before committing.",

                "NEGOTIATE" =>
                    $"The vehicle may be worth buying, but the current deal needs improvement because {factorText}. Negotiate the price or reduce the documented repair risk.",

                _ =>
                    $"The purchase does not currently make financial sense because {factorText}. Walk away unless the seller materially improves the deal."
            };
        }

        private string BuildRiskSummary(
            string purchaseRisk,
            int totalScore,
            int totalInvestmentScore,
            int repairCostScore,
            int mileageScore)
        {
            if (purchaseRisk == "LOW")
            {
                return
                    $"Overall purchase risk is low with a decision score of {totalScore}/100. The price, repairs, mileage, and market-value relationship are generally favorable.";
            }

            if (purchaseRisk == "MODERATE")
            {
                return
                    $"Overall purchase risk is moderate with a decision score of {totalScore}/100. At least one major factor—price, repairs, mileage, or total investment—requires negotiation or verification.";
            }

            return
                $"Overall purchase risk is high with a decision score of {totalScore}/100. The current price and ownership exposure do not provide enough financial protection.";
        }
    }
}
