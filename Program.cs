using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UserManagementAPI.Middleware;
using UserManagementAPI.Repositories;
using Microsoft.EntityFrameworkCore;
using UserManagementAPI.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System;
using System.Linq;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure DbContext (SQLite)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=users.db"));

// Register EF repository
builder.Services.AddScoped<IUserRepository, EfUserRepository>();

// JWT Authentication configuration (read from config/environment)
var jwtKey = builder.Configuration["Jwt:Key"] ?? Environment.GetEnvironmentVariable("JWT__KEY") ?? throw new InvalidOperationException("JWT key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? Environment.GetEnvironmentVariable("JWT__ISSUER") ?? "UserManagementAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? Environment.GetEnvironmentVariable("JWT__AUDIENCE") ?? "UserManagementClients";
var keyBytes = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateLifetime = true
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrator"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware order: Error handling -> Authentication/Authorization -> Logging
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.MapControllers();

// Ensure DB created and apply simple seed if needed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Apply migrations (if any) and ensure DB is created
    db.Database.Migrate();

    // Seed an admin user if missing. Use ADMIN_PASSWORD env var or generated secure password.
    var adminUsername = builder.Configuration["Admin:Username"] ?? Environment.GetEnvironmentVariable("ADMIN__USERNAME") ?? "admin";
    var adminUser = db.AppUsers.FirstOrDefault(u => u.Username == adminUsername);
    if (adminUser == null)
    {
        string adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? builder.Configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            // Generate a secure random password and log it (development only)
            adminPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
            Console.WriteLine($"[NOTICE] Generated admin password for '{adminUsername}': {adminPassword}");
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
        db.AppUsers.Add(new UserManagementAPI.Models.AppUser { Id = Guid.NewGuid(), Username = adminUsername, PasswordHash = hash, Role = "Administrator" });
        db.SaveChanges();
    }
}

app.Run();
