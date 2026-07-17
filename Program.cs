using System.Text.Json.Serialization;
using ComricFraudCalculatorBackend.Authentication;
using ComricFraudCalculatorBackend.Authorization;
using ComricFraudCalculatorBackend.Configuration;
using ComricFraudCalculatorBackend.Data;
using ComricFraudCalculatorBackend.Middleware;
using ComricFraudCalculatorBackend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddAppKeyVault();
KeyVaultConfigurationExtensions.EnsureSqlConnectionConfigured(
    builder.Configuration,
    builder.Environment.EnvironmentName);

if (builder.Environment.IsProduction()
    && !builder.Environment.IsEnvironment("Testing")
    && string.IsNullOrWhiteSpace(builder.Configuration[$"{PlatformOptions.SectionName}:Salt"]))
{
    throw new InvalidOperationException(
        "Platform:Salt is required in Production. Configure App Service setting " +
        "Platform__Salt with Key Vault secret PlatformSalt.");
}

builder.Services.Configure<AzureAdOptions>(builder.Configuration.GetSection(AzureAdOptions.SectionName));
builder.Services.Configure<PlatformOptions>(builder.Configuration.GetSection(PlatformOptions.SectionName));

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

if (builder.Environment.IsEnvironment("Testing"))
{
    var testDbName = builder.Configuration["Testing:DatabaseName"] ?? $"ComricFraud_Tests_{Guid.NewGuid()}";
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase(testDbName));
}
else
{
    builder.Services.AddSingleton<TenantSessionContextInterceptor>();
    builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
    {
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.EnableRetryOnFailure(3));
        options.AddInterceptors(sp.GetRequiredService<TenantSessionContextInterceptor>());
    });
}

// Dev auth when LocalDevelopment:UseDevAuth=true (local + PoC App Service). Not used in Testing.
var useDevAuth = !builder.Environment.IsEnvironment("Testing")
    && builder.Configuration.GetValue<bool>("LocalDevelopment:UseDevAuth");

if (useDevAuth)
{
    builder.Services.AddAuthentication(DevAuthenticationHandler.AuthScheme)
        .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(DevAuthenticationHandler.AuthScheme, _ => { });
}
else if (!builder.Environment.IsEnvironment("Testing"))
{
    var azureAdSection = builder.Configuration.GetSection(AzureAdOptions.SectionName);
    var apiClientId = builder.Configuration[$"{AzureAdOptions.SectionName}:ClientId"];
    var apiAudience = builder.Configuration[$"{AzureAdOptions.SectionName}:Audience"];

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(azureAdSection);

    builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        var audiences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(apiAudience))
            audiences.Add(apiAudience);
        if (!string.IsNullOrWhiteSpace(apiClientId))
        {
            audiences.Add(apiClientId);
            audiences.Add($"api://{apiClientId}");
        }

        if (audiences.Count > 0)
            options.TokenValidationParameters.ValidAudiences = audiences.ToList();
    });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.EventsRead, policy =>
        policy.RequireAuthenticatedUser()
            .RequireAssertion(ctx => ScopeAuthorization.HasScope(ctx.User, AuthScopes.EventsRead)));

    options.AddPolicy(AuthPolicies.EventsWrite, policy =>
        policy.RequireAuthenticatedUser()
            .RequireAssertion(ctx => ScopeAuthorization.HasScope(ctx.User, AuthScopes.EventsWrite)));

    options.AddPolicy(AuthPolicies.SignalsRead, policy =>
        policy.RequireAuthenticatedUser()
            .RequireAssertion(ctx => ScopeAuthorization.HasScope(ctx.User, AuthScopes.SignalsRead)));

    options.AddPolicy(AuthPolicies.AuditRead, policy =>
        policy.RequireAuthenticatedUser()
            .RequireAssertion(ctx => ScopeAuthorization.HasScope(ctx.User, AuthScopes.AuditRead)));

    options.AddPolicy(AuthPolicies.DashboardRead, policy =>
        policy.RequireAuthenticatedUser()
            .RequireAssertion(ctx => ScopeAuthorization.HasScope(ctx.User, AuthScopes.DashboardRead)));
});

builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddSingleton<IHashingService, HashingService>();
builder.Services.AddSingleton<IRiskScoreService, RiskScoreService>();
builder.Services.AddScoped<IHrEventService, HrEventService>();
builder.Services.AddScoped<IMnoEventService, MnoEventService>();
builder.Services.AddScoped<IFraudSignalService, FraudSignalService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Comric Fraud Calculator API",
        Version = "v1",
        Description = "Swagger UI for manual API testing."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a JWT bearer token. For local dev auth you can use: dev-token"
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
            []
        }
    });
});

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseStartup");

    try
    {
        logger.LogInformation("Applying EF Core migrations...");
        await db.Database.MigrateAsync();
        logger.LogInformation("Migrations applied successfully.");

        await DatabaseSeeder.SeedAsync(db);
        logger.LogInformation("Database seed completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex,
            "Database migration or seed failed. Verify ConnectionStrings:DefaultConnection and run 'dotnet ef database update' if needed.");
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Comric Fraud Calculator API v1");
        options.RoutePrefix = "swagger";
        options.DisplayRequestDuration();
    });
}

// App Service terminates TLS; honor X-Forwarded-Proto so redirects/links work.
var forwardedHeaders = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeaders.KnownIPNetworks.Clear();
forwardedHeaders.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaders);

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();

if (!useDevAuth && !app.Environment.IsEnvironment("Testing"))
    app.UseMiddleware<OrganizationEmailMiddleware>();

app.UseAuthorization();

if (app.Configuration.GetValue<bool>("LocalDevelopment:EnableRls"))
    app.UseMiddleware<TenantRlsMiddleware>();

app.UseMiddleware<ActivityLoggingMiddleware>();
app.MapControllers();

// SPA deep-links (React Router) → index.html; API routes stay on controllers.
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
