using CustomerService.Data;
using CustomerService.Filters;
using CustomerService.Models;
using CustomerService.Services.Consumers;
using CustomerService.Services.CustomerCreation;
using CustomerService.Services.Messaging;
using CustomerService.Validators;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.Run();

public partial class Program
{
}