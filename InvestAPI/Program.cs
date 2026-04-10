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
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JwtSettings:SecretKey não configurado.");

if (Encoding.UTF8.GetByteCount(secretKey) < 32)
{
    throw new InvalidOperationException("JwtSettings:SecretKey deve ter no mínimo 32 bytes para HS256.");
}

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
app.UseSwaggerUI();
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