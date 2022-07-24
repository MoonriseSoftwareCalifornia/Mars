using AspNetCore.Identity.CosmosDb.Extensions;
using AspNetCore.Identity.Services.SendGrid;
using AspNetCore.Identity.Services.SendGrid.Extensions;
using Cosmos.IdentityManagement.Website.Data;
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

// Supported values "Cosmos" or "mssql";
var dbProvider = builder.Configuration.GetValue<string>("DbProvider");

if (!string.IsNullOrEmpty(dbProvider) && dbProvider.Equals("mssql", StringComparison.InvariantCultureIgnoreCase))
{
    // Use SQL Server database
    // If the following is set, it will create the Cosmos database and
    //  required containers.
    if (bool.TryParse(setupDb, out var setup) && setup)
    {
        var builder1 = new DbContextOptionsBuilder<CosmosDbContext>();
        builder1.UseSqlServer(connectionString: connectionString);

        using (var dbContext = new CosmosDbContext(builder1.Options))
        {
            dbContext.Database.Migrate();
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
}
else
{
    // Use Cosmos database
    // If the following is set, it will create the Cosmos database and
    //  required containers.
    if (bool.TryParse(setupDb, out var setup) && setup)
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

// Sandbox mode?
var sendGridSandbox = builder.Configuration.GetValue<string>("SendGridSandbox");
if (!string.IsNullOrEmpty(sendGridSandbox) && bool.TryParse(sendGridSandbox, out var sandBoxMode))
{
    sendGridOptions.SandboxMode = sandBoxMode;
}


// Add SendGrid IEmail sender
builder.Services.AddSendGridEmailProvider(sendGridOptions);

// Need to add this for Telerik - Grids for example won't work because
// JSON object properties will start with lower case letters instead of upper.
// https://docs.telerik.com/aspnet-core/getting-started/prerequisites/environment-support#json-serialization
builder.Services.AddMvc()
    .AddNewtonsoftJson(options =>
        options.SerializerSettings.ContractResolver =
            new DefaultContractResolver());

// Add Kendo Services
builder.Services.AddKendo();

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
