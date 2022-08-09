using AspNetCore.Identity.CosmosDb.Extensions;
using AspNetCore.Identity.Services.SendGrid;
using AspNetCore.Identity.Services.SendGrid.Extensions;
using Cosmos.IdentityManagement.Website.Data;
using Cosmos.IdentityManagement.Website.Models;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection") ?? throw new InvalidOperationException("Connection string 'ApplicationDbContextConnection' not found.");

// Name of the Cosmos database to use
var cosmosIdentityDbName = builder.Configuration.GetValue<string>("CosmosIdentityDbName");

// If this is set, the Cosmos identity provider will:
// 1. Create the database if it does not already exist.
// 2. Create the required containers if they do not already exist.

// IMPORTANT: Remove this setting if after first run. It will improve startup performance.
var setupDb = builder.Configuration.GetValue<string>("SetupDb");

// Google Analytics Tag
var gTag = builder.Configuration.GetValue<string>("GTag");

// Sandbox mode?
var sendGridSandbox = builder.Configuration.GetValue<string>("SendGridSandbox");

var marsRunMode = new MarsRunMode(setupDb, sendGridSandbox, gTag);

// Supported values "Cosmos" or "mssql";
var dbProvider = builder.Configuration.GetValue<string>("DbProvider");

if (!string.IsNullOrEmpty(dbProvider) && dbProvider.Equals("mssql", StringComparison.InvariantCultureIgnoreCase))
{
    // Use SQL Server database
    // If the following is set, it will create the Cosmos database and
    //  required containers.
    if (marsRunMode.Setup)
    {
        var builder1 = new DbContextOptionsBuilder<CosmosDbContext>();
        builder1.UseSqlServer(connectionString: connectionString);

        using (var dbContext = new CosmosDbContext(builder1.Options))
        {
            var pending = await dbContext.Database.GetPendingMigrationsAsync();

            if (!pending.Any())
            {
                await dbContext.Database.MigrateAsync();
            }
        }
    }

    _ = builder.Services.AddDbContext<MsSqlDbContext>(options =>
        options.UseSqlServer(connectionString: connectionString));

    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    builder.Services.AddDefaultIdentity<IdentityUser>(
          options =>
          {
              options.SignIn.RequireConfirmedAccount = true;
          } // Always a good idea :)
        )
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<MsSqlDbContext>();
}
else
{
    // Use Cosmos database
    // If the following is set, it will create the Cosmos database and
    //  required containers.
    if (marsRunMode.Setup)
    {
        var builder1 = new DbContextOptionsBuilder<CosmosDbContext>();
        builder1.UseCosmos(connectionString, cosmosIdentityDbName);

        using (var dbContext = new CosmosDbContext(builder1.Options))
        {
            dbContext.Database.EnsureCreated();
        }
    }

    builder.Services.AddDbContext<CosmosDbContext>(options =>
        options.UseCosmos(connectionString: connectionString, databaseName: cosmosIdentityDbName));

    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    builder.Services.AddCosmosIdentity<CosmosDbContext, IdentityUser, IdentityRole>(
          options =>
          {
              options.SignIn.RequireConfirmedAccount = true;
          } // Always a good idea :)
        )
        .AddDefaultUI()
        .AddDefaultTokenProviders();
}

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var sendGridApiKey = builder.Configuration.GetValue<string>("SendGridApiKey");
// Modify 'from' email address to your own.
var sendGridOptions = new SendGridEmailProviderOptions(sendGridApiKey, "eric@moonrise.net");

sendGridOptions.SandboxMode = marsRunMode.SendGridSandboxMode;

// Run mode
builder.Services.AddSingleton(marsRunMode);

// Add SendGrid IEmail sender
builder.Services.AddSendGridEmailProvider(sendGridOptions);

// Add Kendo Services
builder.Services.AddKendo();

// Need to add this for Telerik - Grids for example won't work because
// JSON object properties will start with lower case letters instead of upper.
// https://docs.telerik.com/aspnet-core/getting-started/prerequisites/environment-support#json-serialization
builder.Services.AddMvc()
    .AddNewtonsoftJson(options =>
        options.SerializerSettings.ContractResolver =
            new DefaultContractResolver())
    // Add this so the login paths are made relative to the website.
    .AddRazorPagesOptions(options =>
    {
        // This section docs are here: https://docs.microsoft.com/en-us/aspnet/core/security/authentication/scaffold-identity?view=aspnetcore-3.1&tabs=visual-studio#full
        //options.AllowAreas = true;
        options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
        options.Conventions.AuthorizeAreaPage("Identity", "/Account/Logout");
    });

// https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-2.1&tabs=visual-studio#http-strict-transport-security-protocol-hsts
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
        //options.ExcludedHosts.Add("example.com");
        //options.ExcludedHosts.Add("www.example.com");
});

builder.Services.ConfigureApplicationCookie(options =>
{
    // This section docs are here: https://docs.microsoft.com/en-us/aspnet/core/security/authentication/scaffold-identity?view=aspnetcore-3.1&tabs=visual-studio#full
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// BEGIN
// When deploying to a Docker container, the OAuth redirect_url
// parameter may have http instead of https.
// Providers often do not allow http because it is not secure.
// So authentication will fail.
// Article below shows instructions for fixing this.
//
// NOTE: There is a companion secton below in the Configure method. Must have this
// app.UseForwardedHeaders();
//
// https://seankilleen.com/2020/06/solved-net-core-azure-ad-in-docker-container-incorrectly-uses-an-non-https-redirect-uri/
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                               ForwardedHeaders.XForwardedProto;
    // Only loopback proxies are allowed by default.
    // Clear that restriction because forwarders are enabled by explicit
    // configuration.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
// END

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// BEGIN
// https://seankilleen.com/2020/06/solved-net-core-azure-ad-in-docker-container-incorrectly-uses-an-non-https-redirect-uri/
app.UseForwardedHeaders();
// END

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
