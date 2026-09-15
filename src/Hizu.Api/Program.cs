using Hizu.Api.Middleware;
using Hizu.Application;
using Hizu.Domain.Entities;
using Hizu.Infrastructure;
using Hizu.Infrastructure.Persistence;
using Hizu.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Layer Services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 2. Controllers & JSON Options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// 3. CORS Configuration
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000", "http://127.0.0.1:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// 4. Swagger / OpenAPI Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hizu Fabrication Lab & E-Commerce API",
        Version = "v1",
        Description = "Modular Monolith Web API for Hizu (هیزو) Digital Fabrication Platform built with Clean Architecture, CQRS (MediatR), EF Core, and PostgreSQL."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid JWT token.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsIn...\""
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 5. Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("Database");

var app = builder.Build();

// 6. Database Initialization & Seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        if (context.Database.IsRelational())
        {
            logger.LogInformation("Applying EF Core database migrations...");
            await context.Database.MigrateAsync();
        }

        logger.LogInformation("Seeding database data...");
        await DatabaseSeeder.SeedDatabaseAsync(context, userManager, roleManager, logger);
        logger.LogInformation("Database initialization complete.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database migration/seeding. API will start, but database may not be ready.");
    }
}

// 7. HTTP Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("EnableSwaggerInProd", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hizu Fabrication API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("DefaultCorsPolicy");

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

// For Integration Tests
public partial class Program { }
