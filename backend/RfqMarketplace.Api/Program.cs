using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RfqMarketplace.Api.Common;
using RfqMarketplace.Api.Data;
using RfqMarketplace.Api.Middleware;
using RfqMarketplace.Api.Models;
using RfqMarketplace.Api.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration));

// ---------------------------------------------------------------------------
// Configuration
// ---------------------------------------------------------------------------
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(o => o.Key.Length >= 32, "Jwt:Key must be at least 32 characters. Set it via configuration or the JWT__KEY environment variable.")
    .ValidateOnStart();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(3)));

// ---------------------------------------------------------------------------
// Identity: owns credential storage, password hashing and the role tables
// ---------------------------------------------------------------------------
builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>();

// ---------------------------------------------------------------------------
// Authentication: JWT, delivered to the browser in an HTTP-only cookie.
// The scheme is configured from IOptions<JwtOptions> (see ConfigureJwtBearerOptions) so the
// key that signs tokens and the key that validates them can never diverge.
// ---------------------------------------------------------------------------
builder.Services.ConfigureOptions<ConfigureJwtBearerOptions>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------------
// MVC, validation and application services
// ---------------------------------------------------------------------------
builder.Services.AddControllers();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Model-binding and FluentValidation failures both arrive here, so the client sees one shape.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = "One or more fields are invalid.",
            Instance = context.HttpContext.Request.Path
        };
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
    };
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRfqService, RfqService>();
builder.Services.AddScoped<IQuotationService, QuotationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<DbSeeder>();

// ---------------------------------------------------------------------------
// CORS: credentialed requests require an explicit origin list, never AllowAnyOrigin
// ---------------------------------------------------------------------------
const string CorsPolicy = "AppCors";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];

builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

// ---------------------------------------------------------------------------
// Swagger
// ---------------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "B2B RFQ Marketplace API",
        Version = "v1",
        Description = "Buyers publish requirements (RFQs); suppliers discover them and submit quotations."
    });

    options.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date" });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the token returned by /api/auth/login. The browser app uses the HTTP-only cookie instead.",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };

    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = [] });

    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// ---------------------------------------------------------------------------
// Pipeline. Order matters: exceptions first so everything below is covered.
// ---------------------------------------------------------------------------
app.UseExceptionHandling();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment() || app.Configuration.GetBool("Swagger:Enabled", false))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "RFQ Marketplace API v1");
        options.DocumentTitle = "B2B RFQ Marketplace API";
    });
}

app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Apply migrations and seed demo data at startup so a fresh container is immediately usable.
if (app.Configuration.GetBool("Database:MigrateOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
}

app.Run();

/// <summary>Exposed so the integration tests can boot the real pipeline with WebApplicationFactory.</summary>
public partial class Program;
