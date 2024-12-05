using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FluentEmail.Core;
using FluentEmail.Smtp;
using System.Net.Mail;
using tupadportal.Data;
using tupadportal.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure services for FluentEmail
builder.Services.AddFluentEmail(builder.Configuration["SmtpSettings:From"], builder.Configuration["SmtpSettings:SenderName"])
    .AddRazorRenderer()
    .AddSmtpSender(() => new SmtpClient(builder.Configuration["SmtpSettings:Host"])
    {
        Port = int.Parse(builder.Configuration["SmtpSettings:Port"]),
        Credentials = new System.Net.NetworkCredential(
            builder.Configuration["SmtpSettings:User"],
            builder.Configuration["SmtpSettings:Password"]),
        EnableSsl = bool.Parse(builder.Configuration["SmtpSettings:EnableSsl"])
    });


// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

// Configure the database context using SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString)
        .EnableSensitiveDataLogging(); // Enable sensitive data logging for debugging
});

// Configure Identity services with role support
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // Enforce HTTPS in production
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Ensure authentication middleware is in place
app.UseAuthorization();  // Ensure authorization middleware is in place

// Map Razor Pages and Controllers
app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed roles into the database at application startup
await SeedRoles(app);

app.Run();

// Method to seed roles into the database
async Task SeedRoles(IApplicationBuilder app)
{
    using (var scope = app.ApplicationServices.CreateScope())
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var roles = new[] { "admin", "brgy", "peso" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
