using System.Text;
using System.Text.Json.Nodes;
using FluentValidation;
using FluentValidation.AspNetCore;
using InvestAPI.Data;
using InvestAPI.Filters;
using InvestAPI.Middleware;
using InvestAPI.Models;
using InvestAPI.Repositories.Assets;
using InvestAPI.Repositories.Common;
using InvestAPI.Repositories.Quotes;
using InvestAPI.Repositories.Transactions;
using InvestAPI.Repositories.Users;
using InvestAPI.Services.Assets;
using InvestAPI.Services.Auth;
using InvestAPI.Services.Dashboard;
using InvestAPI.Services.Portfolio;
using InvestAPI.Services.Quotes;
using InvestAPI.Services.Transactions;
using InvestAPI.Services.Users;
using InvestAPI.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerUI;
using Swashbuckle.AspNetCore.Annotations;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    options.OrderActionsBy(api =>
    {
        var methodOrder = api.HttpMethod?.ToUpperInvariant() switch
        {
            "POST" => "1",
            "GET" => "2",
            "PUT" => "3",
            "PATCH" => "4",
            "DELETE" => "5",
            _ => "6"
        };

        var group = api.GroupName ?? api.ActionDescriptor.RouteValues["controller"];
        return $"{group}_{methodOrder}_{api.RelativePath}";
    });

    options.UseInlineDefinitionsForEnums();
    options.MapType<AssetType>(() => new OpenApiSchema
    {
        Type = JsonSchemaType.String,
        Enum = Enum
            .GetNames(typeof(AssetType))
            .Select(n => (JsonNode)JsonValue.Create(n)!)
            .ToList()
    });
    options.MapType<TransactionType>(() => new OpenApiSchema
    {
        Type = JsonSchemaType.String,
        Enum = Enum
            .GetNames(typeof(TransactionType))
            .Select(n => (JsonNode)JsonValue.Create(n)!)
            .ToList()
    });

    options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

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

    options.OperationFilter<SecurityOperationFilter>();
    options.DocumentFilter<AlphabeticalTagsDocumentFilter>();
});

// Registrar o DbContext com SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
builder.Services.AddScoped<IQuoteService, DbCachedQuoteService>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<IUsersRepository, UsersRepository>();
builder.Services.AddScoped<IAssetsRepository, AssetsRepository>();
builder.Services.AddScoped<IAssetQuotesRepository, AssetQuotesRepository>();
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DocExpansion(DocExpansion.List);
        options.DefaultModelsExpandDepth(1);
        options.EnableFilter();
        options.EnableTryItOutByDefault();
        options.DisplayRequestDuration();
        options.ShowExtensions();
        options.ShowCommonExtensions();
        options.ConfigObject.AdditionalItems["persistAuthorization"] = true;
        options.InjectStylesheet("/swagger-ui-extras.css");
        options.InjectJavascript("/swagger-ui-extras.js");
    });
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();