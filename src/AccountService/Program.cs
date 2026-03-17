using AccountService.Data;
using AccountService.Filters;
using AccountService.Models;
using AccountService.Services.Consumers;
using AccountService.Services.AccountCreation;
using AccountService.Services.CustomerLookup;
using AccountService.Services.Messaging;
using AccountService.Services.Transactions;
using AccountService.Services.Transactions.Rules;
using AccountService.Validators;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.Run();

public partial class Program
{
}
