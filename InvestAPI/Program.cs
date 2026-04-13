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
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Reflection;

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
});
builder.Services.AddHttpClient<ICoinGeckoClient, CoinGeckoClient>((sp, client) =>
{
    var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<QuoteSettings>>().Value;
    client.BaseAddress = new Uri(settings.CoinGeckoBaseUrl);
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
    options.HeadContent = """
        <style>
            :root {
                color-scheme: light;
            }

            html, body, .swagger-ui {
                background: #ffffff !important;
                color: #1f2937 !important;
            }

            .swagger-ui .topbar {
                background-color: #ffffff !important;
                border-bottom: 1px solid #e5e7eb;
            }

            .swagger-ui .info .title,
            .swagger-ui .opblock-tag,
            .swagger-ui .opblock .opblock-summary-description,
            .swagger-ui .opblock .opblock-summary-path,
            .swagger-ui .opblock .opblock-summary-path span,
            .swagger-ui .opblock-description-wrapper p,
            .swagger-ui .response-col_status,
            .swagger-ui .response-col_description,
            .swagger-ui table thead tr th,
            .swagger-ui .parameter__name,
            .swagger-ui .parameter__type,
            .swagger-ui .parameter__in {
                color: #1f2937 !important;
            }

            .swagger-ui .scheme-container,
            .swagger-ui .opblock,
            .swagger-ui .btn,
            .swagger-ui .dialog-ux .modal-ux,
            .swagger-ui .responses-inner,
            .swagger-ui .response,
            .swagger-ui .parameters-container,
            .swagger-ui section.models,
            .swagger-ui .model-box,
            .swagger-ui .model-box .model,
            .swagger-ui .renderedMarkdown,
            .swagger-ui .tab,
            .swagger-ui .opblock .opblock-section-header {
                background-color: #ffffff !important;
            }

            .swagger-ui .opblock.opblock-get .opblock-summary {
                border-color: #c7d2fe !important;
            }

            .swagger-ui .btn {
                box-shadow: none !important;
            }
        </style>
        """;
});
app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

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