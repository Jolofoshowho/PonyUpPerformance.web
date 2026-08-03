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

var builder = WebApplication.CreateBuilder(args);

StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

var databaseUrl = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is missing.");

string connectionString;

if (databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
    databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
{
    var databaseUri = new Uri(databaseUrl);

    var userInfo = databaseUri.UserInfo.Split(':', 2);

    connectionString = new NpgsqlConnectionStringBuilder
    {
        Host = databaseUri.Host,
        Port = databaseUri.IsDefaultPort ? 5432 : databaseUri.Port,
        Database = databaseUri.AbsolutePath.TrimStart('/'),
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = Uri.UnescapeDataString(userInfo[1]),
        SslMode = SslMode.Require,
        TrustServerCertificate = true
    }.ConnectionString;
}
else
{
    connectionString = databaseUrl;
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>();

builder.Services.AddRazorPages();

builder.Services.AddScoped<IRepairScoringService, RepairScoringService>();
builder.Services.AddScoped<AnalysisHistoryService>();
builder.Services.AddScoped<RepairCostEstimatorService>();
builder.Services.AddScoped<VehiclePaintPaletteService>();
builder.Services.AddScoped<VehicleRenderService>();
builder.Services.AddHttpClient<NhtsaVehicleService>();
builder.Services.AddScoped<StripeCheckoutService>();
builder.Services.AddScoped<UsageCreditService>();
builder.Services.AddScoped<IBuyScoringService, BuyScoringService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    db.Database.EnsureCreated();
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

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
