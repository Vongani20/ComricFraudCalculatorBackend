using System.Text.Json.Serialization;
using ComricFraudCalculatorBackend.Authentication;
using ComricFraudCalculatorBackend.Authorization;
using ComricFraudCalculatorBackend.Data;
using ComricFraudCalculatorBackend.Middleware;
using ComricFraudCalculatorBackend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AzureAdOptions>(builder.Configuration.GetSection(AzureAdOptions.SectionName));

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

var useDevAuth = builder.Environment.IsDevelopment()
    && builder.Configuration.GetValue<bool>("LocalDevelopment:UseDevAuth");

if (useDevAuth)
{
    builder.Services.AddAuthentication(DevAuthenticationHandler.AuthScheme)
        .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(DevAuthenticationHandler.AuthScheme, _ => { });
}
else if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection(AzureAdOptions.SectionName));
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
    await db.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(db);
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

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

if (app.Configuration.GetValue<bool>("LocalDevelopment:EnableRls"))
    app.UseMiddleware<TenantRlsMiddleware>();

app.UseMiddleware<ActivityLoggingMiddleware>();
app.MapControllers();

app.Run();

public partial class Program { }
