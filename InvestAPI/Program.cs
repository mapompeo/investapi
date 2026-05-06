using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using InvestAPI.Data;
using InvestAPI.Middleware;
using InvestAPI.Repositories.Assets;
using InvestAPI.Repositories.Common;
using InvestAPI.Repositories.Transactions;
using InvestAPI.Services.Assets;
using InvestAPI.Services.Auth;
using InvestAPI.Services.Dashboard;
using InvestAPI.Services.Portfolio;
using InvestAPI.Services.Quotes;
using InvestAPI.Services.Transactions;
using InvestAPI.Services.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Reflection;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port) && int.TryParse(port, out var portNumber))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{portNumber}");
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        var origins = allowedOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (origins.Length == 0)
        {
            origins = new[]
            {
                "http://localhost:3000",
                "http://localhost:5173",
                "http://localhost:8080"
            };
        }

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddHealthChecks();
builder.Services.AddRateLimiter(options =>
{
    var rateLimitingSection = builder.Configuration.GetSection("RateLimiting");
    var permitLimit = rateLimitingSection.GetValue<int?>("PermitLimit") ?? 120;
    var windowMinutes = rateLimitingSection.GetValue<int?>("WindowMinutes") ?? 1;
    var queueLimit = rateLimitingSection.GetValue<int?>("QueueLimit") ?? 0;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(windowMinutes),
                QueueLimit = queueLimit,
                AutoReplenishment = true,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o token JWT no formato: Bearer {token}"
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", doc, null)] = new List<string>()
    });
});

var connectionString = ResolveSqliteConnectionString(builder.Configuration, builder.Environment);

// Registrar o DbContext com SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.Configure<QuoteSettings>(builder.Configuration.GetSection("QuoteSettings"));
builder.Services.AddHttpClient<IBrapiClient, BrapiClient>((sp, client) =>
{
    var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<QuoteSettings>>().Value;
    client.BaseAddress = new Uri(settings.BrapiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient<ICoinGeckoClient, CoinGeckoClient>((sp, client) =>
{
    var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<QuoteSettings>>().Value;
    client.BaseAddress = new Uri(settings.CoinGeckoBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<IAssetsRepository, AssetsRepository>();
builder.Services.AddScoped<ITransactionsRepository, TransactionsRepository>();
builder.Services.AddScoped<IQuotesManagementService, QuotesManagementService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<IAssetsService, AssetsService>();
builder.Services.AddScoped<ITransactionsService, TransactionsService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = ResolveJwtSecretKey(jwtSettings);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],

            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    try
    {
        dbContext.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Não foi possível inicializar o banco de dados no startup.");
    }
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
});
app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseCors("DefaultCors");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

    if (canConnect)
    {
        return Results.Ok(new
        {
            status = "Healthy",
            database = "Connected",
            timestamp = DateTime.UtcNow
        });
    }

    return Results.Json(new
    {
        status = "Unhealthy",
        database = "Unavailable",
        timestamp = DateTime.UtcNow
    }, statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.MapControllers();

app.Run();

static string ResolveSqliteConnectionString(IConfiguration configuration, IWebHostEnvironment environment)
{
    var rawConnectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=investapi.db";
    var builder = new SqliteConnectionStringBuilder(rawConnectionString);

    var dbPathOverride = Environment.GetEnvironmentVariable("SQLITE_DB_PATH");
    if (!string.IsNullOrWhiteSpace(dbPathOverride))
    {
        builder.DataSource = dbPathOverride;
    }
    else if (!Path.IsPathRooted(builder.DataSource))
    {
        builder.DataSource = Path.Combine(environment.ContentRootPath, builder.DataSource);
    }

    var directory = Path.GetDirectoryName(builder.DataSource);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }

    return builder.ConnectionString;
}

static string ResolveJwtSecretKey(IConfigurationSection jwtSettings)
{
    var configuredSecretKey = jwtSettings["SecretKey"];
    if (!string.IsNullOrWhiteSpace(configuredSecretKey) && Encoding.UTF8.GetByteCount(configuredSecretKey) >= 32)
    {
        return configuredSecretKey;
    }

    const string fallbackSecretKey = "SET_THIS_IN_ENVIRONMENT_MINIMUM_32_BYTES";
    if (Encoding.UTF8.GetByteCount(fallbackSecretKey) >= 32)
    {
        return fallbackSecretKey;
    }

    throw new InvalidOperationException("JwtSettings:SecretKey deve ter no mínimo 32 bytes para HS256.");
}