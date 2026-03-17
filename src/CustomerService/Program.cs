using CustomerService.Data;
using CustomerService.Filters;
using CustomerService.HealthChecks;
using CustomerService.Models;
using CustomerService.Services.Consumers;
using CustomerService.Services.CustomerCreation;
using CustomerService.Services.Messaging;
using CustomerService.Validators;
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
    .AddCheck<CustomerDatabaseHealthCheck>("database", tags: new[] { "ready" });

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
builder.Services.AddScoped<IValidator<CustomerRequest>, CustomerRequestValidator>();

// Entity Framework Configuration
var connectionString = builder.Configuration.GetConnectionString("CustomerDatabase");
builder.Services.AddDbContext<CustomerDbContext>(options =>
    options.UseSqlServer(connectionString));

// RabbitMQ Configuration
var rabbitMqSettings = builder.Configuration.GetSection("RabbitMq").Get<RabbitMqSettings>()
    ?? throw new InvalidOperationException("RabbitMQ settings not configured");
builder.Services.AddSingleton(rabbitMqSettings);
builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();

// Customer Creation Application Services
builder.Services.AddScoped<ICustomerFactory, CustomerFactory>();
builder.Services.AddScoped<ICustomerCreationService, CustomerCreationService>();

// Background Services
builder.Services.AddHostedService<CustomerConsumerService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CustomerService API v1");
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

public partial class Program
{
}