using AccountService.Data;
using AccountService.Filters;
using AccountService.HealthChecks;
using AccountService.Models;
using AccountService.Services.Consumers;
using AccountService.Services.AccountCreation;
using AccountService.Services.CustomerLookup;
using AccountService.Services.Messaging;
using AccountService.Services.Transactions;
using AccountService.Services.Transactions.Rules;
using AccountService.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddCheck<AccountDatabaseHealthCheck>("database", tags: new[] { "ready" });

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddOtlpExporter();
    });

// FluentValidation Configuration
builder.Services.AddScoped<IValidator<AccountRequest>, AccountRequestValidator>();
builder.Services.AddScoped<IValidator<TransactionRequest>, TransactionRequestValidator>();

// Entity Framework Configuration
var connectionString = builder.Configuration.GetConnectionString("AccountDatabase");
builder.Services.AddDbContext<AccountDbContext>(options =>
    options.UseSqlServer(connectionString));

// RabbitMQ Configuration
var rabbitMqSettings = builder.Configuration.GetSection("RabbitMq").Get<RabbitMqSettings>()
    ?? throw new InvalidOperationException("RabbitMQ settings not configured");
builder.Services.AddSingleton(rabbitMqSettings);
builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();

// Account Creation Application Services
var customerServiceOptions = builder.Configuration.GetSection("CustomerService").Get<CustomerServiceOptions>()
    ?? new CustomerServiceOptions();

builder.Services.AddHttpClient<ICustomerLookupService, CustomerLookupService>(client =>
{
    client.BaseAddress = new Uri(customerServiceOptions.BaseUrl);
});

builder.Services.AddScoped<IAccountFactory, AccountFactory>();
builder.Services.AddScoped<IAccountCreationService, AccountCreationService>();
builder.Services.AddScoped<CreditTransactionRuleHandler>();
builder.Services.AddScoped<DebitTransactionRuleHandler>();
builder.Services.AddScoped<ReserveTransactionRuleHandler>();
builder.Services.AddScoped<CaptureTransactionRuleHandler>();
builder.Services.AddScoped<ReversalTransactionRuleHandler>();
builder.Services.AddScoped<TransferTransactionRuleHandler>();
builder.Services.AddScoped<ITransactionRuleEngine, TransactionRuleEngine>();
builder.Services.AddScoped<ITransactionProcessor, TransactionProcessor>();

// Background Services
builder.Services.AddHostedService<AccountConsumerService>();
builder.Services.AddHostedService<TransactionConsumerService>();

var app = builder.Build();

await ApplyMigrationsWithRetryAsync(app.Services, app.Logger, app.Lifetime.ApplicationStopping);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AccountService API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

app.Run();

static async Task ApplyMigrationsWithRetryAsync(
    IServiceProvider services,
    ILogger logger,
    CancellationToken cancellationToken)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
    
    // Skip migrations for non-relational databases (e.g., in-memory for testing)
    if (!dbContext.Database.IsRelational())
    {
        logger.LogInformation("AccountService using non-relational database. Skipping migrations.");
        return;
    }

    const int maxAttempts = 10;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            using var retryScope = services.CreateScope();
            var retryDbContext = retryScope.ServiceProvider.GetRequiredService<AccountDbContext>();
            await retryDbContext.Database.MigrateAsync(cancellationToken);

            logger.LogInformation("AccountService database migrations applied successfully.");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(
                ex,
                "Failed to apply AccountService migrations on attempt {Attempt}/{MaxAttempts}. Retrying in 3 seconds.",
                attempt,
                maxAttempts);

            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
        }
    }

    using var finalScope = services.CreateScope();
    var finalDbContext = finalScope.ServiceProvider.GetRequiredService<AccountDbContext>();
    await finalDbContext.Database.MigrateAsync(cancellationToken);
}

public partial class Program
{
}
