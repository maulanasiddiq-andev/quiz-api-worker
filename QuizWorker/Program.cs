using QuizWorker.Extensions;
using QuizWorker.Settings;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.Configure<EmailSetting>(builder.Configuration.GetSection("EmailSetting"));
builder.Services.Configure<PushNotificationSetting>(builder.Configuration.GetSection("PushNotificationSetting"));

builder.Services.RegisterRepositories();

// RabbitMQ
var rabbitMQConnectionString = builder.Configuration.GetConnectionString("RabbitMQ");
builder.Services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory
    {
        Uri = new Uri(rabbitMQConnectionString ?? "amqp://guest:guest@localhost:5672/"),
        AutomaticRecoveryEnabled = true,
        TopologyRecoveryEnabled = true,
        NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
    }
);

var app = builder.Build();

app.MapGet("/", () => "OK");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection();

app.Run();

