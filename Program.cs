using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RestoBooking.Data;
using RestoBooking.Models;
using RestoBooking.Services;
using System.Net;
using System.Net.Sockets;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

ConfigurePortBinding(builder);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Force the SQLite database to live under the app's content root so the same file
// is used across restarts and different launch contexts (e.g. CLI vs Visual Studio).
var sqliteBuilder = new SqliteConnectionStringBuilder(connectionString);
if (!Path.IsPathRooted(sqliteBuilder.DataSource))
{
    sqliteBuilder.DataSource = Path.Combine(builder.Environment.ContentRootPath, sqliteBuilder.DataSource);
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(sqliteBuilder.ToString()));


builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddOptions<EmailSettings>()
    .Bind(builder.Configuration.GetSection("EmailSettings"))
    .ValidateDataAnnotations()
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.Password), "Email password must be provided.")
    .Validate(settings => settings.EnableSSL, "SSL must be enabled for Gmail SMTP access.")
    .ValidateOnStart();
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// Seed des tables (une seule fois, AVANT le pipeline)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await db.Database.MigrateAsync();
    }
    catch (InvalidOperationException ex)
    {
        // If the database was previously created without migrations (e.g., via EnsureCreated), migration can fail with an
        // InvalidOperationException. In that case, recreate the database using the current migrations so the schema matches
        // the model and seeded data.
        app.Logger.LogWarning(ex, "Failed to apply migrations; recreating the database to align schema with current model.");
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    var defaultTables = new List<Table>
    {
        new Table { Name = "Table Standard 1", Capacity = 2, Category = TableCategory.Standard, BasePricePerPerson = 230m, IsActive = true },
        new Table { Name = "Table Standard 2", Capacity = 4, Category = TableCategory.Standard, BasePricePerPerson = 260m, IsActive = true },
        new Table { Name = "Table VIP 1", Capacity = 4, Category = TableCategory.VIPExclusive, BasePricePerPerson = 360m, IsActive = true },
        new Table { Name = "Espace privé 1", Capacity = 6, Category = TableCategory.EspacePrive, BasePricePerPerson = 320m, IsActive = true },
        new Table { Name = "Expérience gastronomique", Capacity = 4, Category = TableCategory.ExperienceGastronomique, BasePricePerPerson = 420m, IsActive = true },
        new Table { Name = "Emplacement premium terrasse", Capacity = 4, Category = TableCategory.EmplacementPremium, BasePricePerPerson = 340m, IsActive = true },
        new Table { Name = "Business haut de gamme", Capacity = 6, Category = TableCategory.BusinessHautDeGamme, BasePricePerPerson = 380m, IsActive = true },
        new Table { Name = "Table événementielle", Capacity = 8, Category = TableCategory.Evenementielle, BasePricePerPerson = 360m, IsActive = true }
    };

    // Ajoute des tables premium si aucune n'existe encore
    if (!db.Tables.Any())
    {
        db.Tables.AddRange(defaultTables);
    }
    else
    {
        foreach (var tableSeed in defaultTables)
        {
            var existingTable = db.Tables.FirstOrDefault(t => t.Name == tableSeed.Name);

            if (existingTable == null)
            {
                db.Tables.Add(tableSeed);
            }
            else
            {
                existingTable.Capacity = tableSeed.Capacity;
                existingTable.Category = tableSeed.Category;
                existingTable.BasePricePerPerson = tableSeed.BasePricePerPerson;
                existingTable.IsActive = true;
            }
        }
    }

    // S'assure que toutes les tables existantes ont un prix de base cohérent
    foreach (var table in db.Tables)
    {
        if (table.BasePricePerPerson <= 0)
        {
            table.BasePricePerPerson = 220m;
        }

        if (!table.IsActive)
        {
            table.IsActive = true;
        }
    }
    await db.SaveChangesAsync();
}

// Création des rôles et admin par défaut
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    string adminRoleName = "Admin";
    if (!await roleManager.RoleExistsAsync(adminRoleName))
    {
        await roleManager.CreateAsync(new IdentityRole(adminRoleName));
    }

    // Crée un compte admin par défaut si nécessaire
    string adminEmail = "admin@restobooking.com";
    string adminPassword = "Admin123!";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, adminRoleName);
        }
        // (sinon tu peux loguer les erreurs)
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
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

static void ConfigurePortBinding(WebApplicationBuilder builder)
{
    var configuredUrls = builder.Configuration["ASPNETCORE_URLS"];

    if (!string.IsNullOrWhiteSpace(configuredUrls))
    {
        return;
    }

    var preferredPort = builder.Configuration.GetValue<int?>("PORT") ?? 5273;
    var selectedPort = EnsureAvailablePort(preferredPort);

    if (selectedPort != preferredPort)
    {
        Console.WriteLine($"Port {preferredPort} already in use. Falling back to {selectedPort}.");
    }

    builder.WebHost.UseUrls($"http://localhost:{selectedPort}");
}

static int EnsureAvailablePort(int preferredPort)
{
    if (!IsPortInUse(preferredPort))
    {
        return preferredPort;
    }

    using var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();

    return port;
}

static bool IsPortInUse(int port)
{
    try
    {
        using var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        listener.Stop();
        return false;
    }
    catch (SocketException)
    {
        return true;
    }
}
