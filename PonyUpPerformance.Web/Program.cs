using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PonyUpPerformance.Web.Data;
using PonyUpPerformance.Web.Models;
using PonyUpPerformance.Web.Services;
using PonyUpPerformance.Web.Services.Scoring;
using Stripe;
using Npgsql;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

bool validationMode =
    builder.Environment.IsEnvironment("Validation");

if (validationMode)
{
    builder.Services.AddDbContext<ApplicationDbContext>(
        options =>
            options.UseInMemoryDatabase(
                "PonyUpRuntimeValidation"));
}
else
{
    var databaseUrl =
        builder.Configuration
            .GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "DefaultConnection is missing.");

    string connectionString;

    if (databaseUrl.StartsWith(
            "postgresql://",
            StringComparison.OrdinalIgnoreCase) ||
        databaseUrl.StartsWith(
            "postgres://",
            StringComparison.OrdinalIgnoreCase))
    {
        var databaseUri =
            new Uri(databaseUrl);

        var userInfo =
            databaseUri.UserInfo.Split(
                ':',
                2);

        connectionString =
            new NpgsqlConnectionStringBuilder
            {
                Host =
                    databaseUri.Host,
                Port =
                    databaseUri.IsDefaultPort
                        ? 5432
                        : databaseUri.Port,
                Database =
                    databaseUri.AbsolutePath.TrimStart('/'),
                Username =
                    Uri.UnescapeDataString(
                        userInfo[0]),
                Password =
                    Uri.UnescapeDataString(
                        userInfo[1]),
                SslMode =
                    SslMode.Require
            }.ConnectionString;
    }
    else
    {
        connectionString =
            databaseUrl;
    }

    builder.Services.AddDbContext<ApplicationDbContext>(
        options =>
            options.UseNpgsql(
                connectionString));
}

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan =
        TimeSpan.FromDays(365);

    options.SlidingExpiration =
        true;
});

var authenticationBuilder =
    builder.Services.AddAuthentication();

string? googleClientId =
    builder.Configuration["Authentication:Google:ClientId"];

string? googleClientSecret =
    builder.Configuration["Authentication:Google:ClientSecret"];

if (!string.IsNullOrWhiteSpace(googleClientId) &&
    !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authenticationBuilder.AddGoogle(options =>
    {
        options.ClientId =
            googleClientId;

        options.ClientSecret =
            googleClientSecret;
    });
}

string? facebookAppId =
    builder.Configuration["Authentication:Facebook:AppId"];

string? facebookAppSecret =
    builder.Configuration["Authentication:Facebook:AppSecret"];

if (!string.IsNullOrWhiteSpace(facebookAppId) &&
    !string.IsNullOrWhiteSpace(facebookAppSecret))
{
    authenticationBuilder.AddFacebook(options =>
    {
        options.AppId =
            facebookAppId;

        options.AppSecret =
            facebookAppSecret;
    });
}

builder.Services.AddDataProtection()
    .SetApplicationName("PonyUpPerformance.Web")
    .PersistKeysToDbContext<ApplicationDbContext>();


builder.Services.AddRazorPages();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter(
        "owner-login",
        limiter =>
        {
            limiter.PermitLimit = 10;
            limiter.Window =
                TimeSpan.FromMinutes(1);
            limiter.QueueLimit = 0;
            limiter.QueueProcessingOrder =
                QueueProcessingOrder.OldestFirst;
            limiter.AutoReplenishment = true;
        });
});

builder.Services.AddScoped<IRepairScoringService, RepairScoringService>();
builder.Services.AddScoped<AnalysisHistoryService>();
builder.Services.AddScoped<RepairCostEstimatorService>();
builder.Services.AddScoped<RepairEstimateCreditTokenService>();
builder.Services.AddScoped<VehiclePaintPaletteService>();
builder.Services.AddScoped<VehicleRenderService>();
builder.Services.AddHttpClient<IVinDecoderService, NhtsaVehicleService>();
builder.Services.AddHttpClient<IVehicleSpecEnrichmentService, FuelEconomyVehicleSpecService>();
builder.Services.AddHttpClient<IMarketValueService, MarketValueService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddScoped<StripeCheckoutService>();
builder.Services.AddScoped<PlanEntitlementService>();
builder.Services.AddScoped<StripeFulfillmentService>();
builder.Services.AddScoped<PonyUpIdentityEmailSender>();
builder.Services.AddScoped<IEmailSender<ApplicationUser>, PonyUpIdentityEmailSender>();
builder.Services.AddScoped<UsageCreditService>();
builder.Services.AddScoped<BetaAccessCodeService>();
builder.Services.AddHostedService<BetaAccessBootstrapService>();
builder.Services.AddScoped<UserVinHistoryService>();
builder.Services.AddHostedService<VinHistoryBootstrapService>();
builder.Services.AddScoped<IRevUpReportProvider, UnavailableRevUpReportProvider>();
builder.Services.AddScoped<RevUpReportService>();
builder.Services.AddScoped<IBuyScoringService, BuyScoringService>();
builder.Services.AddScoped<ISellScoringService, SellScoringService>();
builder.Services.AddScoped<ITradeScoringService, TradeScoringService>();
builder.Services.AddScoped<IUpgradeScoringService, UpgradeScoringService>();

var app = builder.Build();

if (!validationMode)
{
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    db.Database.EnsureCreated();

    await db.Database.ExecuteSqlRawAsync(
        """
        ALTER TABLE "GarageVehicles"
        ADD COLUMN IF NOT EXISTS "AppearanceJson"
        text NOT NULL DEFAULT '{{}}';
        """);

    await db.Database.ExecuteSqlRawAsync(
        """
        ALTER TABLE "AspNetUsers"
        ADD COLUMN IF NOT EXISTS "SubscriptionCredits"
        integer NOT NULL DEFAULT 0;
        """);

    await db.Database.ExecuteSqlRawAsync(
        """
        ALTER TABLE "AspNetUsers"
        ADD COLUMN IF NOT EXISTS "StripeCustomerId"
        text NOT NULL DEFAULT '';
        """);

    await db.Database.ExecuteSqlRawAsync(
        """
        ALTER TABLE "AspNetUsers"
        ADD COLUMN IF NOT EXISTS "ActiveStripeSubscriptionId"
        text NOT NULL DEFAULT '';
        """);

    await db.Database.ExecuteSqlRawAsync(
        """
        ALTER TABLE "AspNetUsers"
        ADD COLUMN IF NOT EXISTS "SubscriptionBillingInterval"
        text NOT NULL DEFAULT '';
        """);

    await db.Database.ExecuteSqlRawAsync(
        """
        ALTER TABLE "AspNetUsers"
        ADD COLUMN IF NOT EXISTS "NextSubscriptionCreditRefreshOn"
        timestamp with time zone NULL;
        """);

    await db.Database.ExecuteSqlRawAsync(
        """
        ALTER TABLE "AspNetUsers"
        ADD COLUMN IF NOT EXISTS "RevUpReportsRemaining"
        integer NOT NULL DEFAULT 0;
        """);

    await db.Database.ExecuteSqlRawAsync(
        """
        ALTER TABLE "AspNetUsers"
        ADD COLUMN IF NOT EXISTS "NextRevUpReportRefreshOn"
        timestamp with time zone NULL;
        """);

    await db.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE IF NOT EXISTS "RevUpReports"
        (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "UserId" text NOT NULL,
            "Vin" text NOT NULL,
            "VehicleYear" integer NULL,
            "VehicleMake" text NOT NULL DEFAULT '',
            "VehicleModel" text NOT NULL DEFAULT '',
            "Status" text NOT NULL DEFAULT 'Pending',
            "ProviderName" text NOT NULL DEFAULT '',
            "ProviderReportId" text NOT NULL DEFAULT '',
            "ExternalCostCents" integer NULL,
            "ReportJson" text NOT NULL DEFAULT (CHR(123) || CHR(125)),
            "ErrorMessage" text NOT NULL DEFAULT '',
            "CreatedOn" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
            "CompletedOn" timestamp with time zone NULL
        );
        """);

    await db.Database.ExecuteSqlRawAsync(
        """
        CREATE INDEX IF NOT EXISTS "IX_RevUpReports_UserId_Vin_Status"
        ON "RevUpReports" ("UserId", "Vin", "Status");
        """);


    int totalAccounts =
        await db.Users.CountAsync();

    int freeAccounts =
        await db.Users.CountAsync(
            x =>
                string.IsNullOrEmpty(x.CurrentPlan) ||
                x.CurrentPlan == "Free" ||
                x.CurrentPlan == "free");

    int activeSubscriptions =
        await db.Users.CountAsync(
            x => x.ActiveStripeSubscriptionId != "");

    int quickPackBuyers =
        await db.StripePurchases
            .Where(x =>
                x.PlanKey ==
                PonyUpPlanCatalog.QuickPackKey)
            .Select(x => x.UserId)
            .Distinct()
            .CountAsync();

    app.Logger.LogInformation(
        "PONYUP_MEMBER_STATS TotalAccounts={TotalAccounts} FreeAccounts={FreeAccounts} ActiveSubscriptions={ActiveSubscriptions} QuickPackBuyers={QuickPackBuyers}",
        totalAccounts,
        freeAccounts,
        activeSubscriptions,
        quickPackBuyers);
}
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if (!validationMode)
{
    app.Use(
        async (context, next) =>
        {
            string host =
                context.Request.Host.Host;

            bool shouldRedirect =
                host.Equals(
                    "ponyupperformance.com",
                    StringComparison.OrdinalIgnoreCase) ||
                host.Equals(
                    "pony-up-performance.com",
                    StringComparison.OrdinalIgnoreCase) ||
                host.Equals(
                    "www.pony-up-performance.com",
                    StringComparison.OrdinalIgnoreCase);

            if (shouldRedirect)
            {
                string destination =
                    "https://www.ponyupperformance.com" +
                    context.Request.PathBase +
                    context.Request.Path +
                    context.Request.QueryString;

                context.Response.Redirect(
                    destination,
                    permanent: true,
                    preserveMethod: true);

                return;
            }

            await next();
        });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();

public partial class Program
{
}

