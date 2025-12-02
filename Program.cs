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

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure DbContext (SQLite)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=users.db"));

// Register EF repository
builder.Services.AddScoped<IUserRepository, EfUserRepository>();

// JWT Authentication configuration
var jwtKey = builder.Configuration.GetValue<string>("Jwt:Key") ?? "supersecret_jwt_key_please_change";
var jwtIssuer = builder.Configuration.GetValue<string>("Jwt:Issuer") ?? "UserManagementAPI";
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
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateLifetime = true
    };
});

builder.Services.AddAuthorization();

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

    // Seed an admin user if missing
    var adminUser = db.AppUsers.FirstOrDefault(u => u.Username == "admin");
    if (adminUser == null)
    {
        var pwd = "password"; // change this in production
        var hash = BCrypt.Net.BCrypt.HashPassword(pwd);
        db.AppUsers.Add(new UserManagementAPI.Models.AppUser { Id = Guid.NewGuid(), Username = "admin", PasswordHash = hash, Role = "Administrator" });
        db.SaveChanges();
    }
}

app.Run();
