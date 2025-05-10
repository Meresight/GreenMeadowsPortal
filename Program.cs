using GreenMeadowsPortal.Data;
using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Database Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity Services - Use our ApplicationUser and point to our AppDbContext
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Add MVC Controllers and Views
builder.Services.AddControllersWithViews();

// Register services
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<FeedbackService>();
builder.Services.AddScoped<IForumService, ForumService>();
builder.Services.AddScoped<ServiceRequestService>();
builder.Services.AddScoped<PollService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<INotificationService>(provider =>
    provider.GetRequiredService<NotificationService>());
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AnnouncementService>();
builder.Services.AddScoped<ContactService>();
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = int.MaxValue; // Allow large file uploads
});// Add API controllers with JSON formatting
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.WriteIndented = true;
    });

var app = builder.Build();

// Seed Default Roles and Admin on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    await SeedRolesAndAdmin(roleManager, userManager);
}

// Configure Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Configure Default Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");
app.MapControllerRoute(
    name: "userManagement",
    pattern: "{controller=UserManagement}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "events-redirect",
    pattern: "Events/{*catchall}",
    defaults: new { controller = "Event", action = "Index" });
// Add API controller routes
app.MapControllers();

app.Run();

// Function to Seed Default Roles & Admin
async Task SeedRolesAndAdmin(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
{
    string[] roles = { "Admin", "Staff", "Homeowner" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Create Default Admin User
    string adminEmail = "admin@greenmeadows.com";
    string adminPassword = "Admin@123"; // Change in production
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        var newAdmin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FirstName = "Admin",
            LastName = "User",
            Status = "Active",
            MemberSince = DateTime.Now
        };
        var result = await userManager.CreateAsync(newAdmin, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(newAdmin, "Admin");
        }

        // Removed nested scope to avoid variable name conflict
        var users = await userManager.Users.ToListAsync();
        Console.WriteLine($"Number of users in database: {users.Count}");

        foreach (var user in users)
        {
            Console.WriteLine($"User: {user.Email}, Roles: {string.Join(", ", await userManager.GetRolesAsync(user))}");
        }
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var dbContext = services.GetRequiredService<AppDbContext>();

                // Check if database exists and create it if not
                dbContext.Database.EnsureCreated();

                // Apply any pending migrations
                if (dbContext.Database.GetPendingMigrations().Any())
                {
                    dbContext.Database.Migrate();
                }

                // Check if AspNetUsers table exists and has records
                var usersCount = await dbContext.Users.CountAsync();
                Console.WriteLine($"AspNetUsers table has {usersCount} records");
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while migrating or initializing the database.");
            }
        }
        app.Use(async (context, next) =>
        {
            var routeData = context.GetRouteData();
            if (routeData != null)
            {
                var controller = routeData.Values["controller"]?.ToString();
                var action = routeData.Values["action"]?.ToString();
                Console.WriteLine($"Route: {controller}/{action}");
            }
            await next();
        });
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }
    }
}