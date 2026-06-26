using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserSubscription> UserSubscriptions { get; set; }

        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

        public DbSet<AnalysisUsage> AnalysisUsages { get; set; }

        public DbSet<AnalysisHistory> AnalysisHistories { get; set; }

        public DbSet<GarageVehicle> GarageVehicles { get; set; }

        public DbSet<StripePurchase> StripePurchases { get; set; }
    }
}
