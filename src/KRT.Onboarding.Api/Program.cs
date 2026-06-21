using System.Text.Json.Serialization;
using KRT.Onboarding.Api.Middlewares;
using KRT.Onboarding.Application;
using KRT.Onboarding.Infrastructure;
using KRT.Onboarding.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Logging estruturado lido da configuração.
builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

// Enums serializados como texto ("Ativa"/"Inativa") em vez de número.
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new() { Title = "KRT Onboarding API", Version = "v1" }));

// Tratamento de erros padronizado (ProblemDetails / RFC 7807).
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Camadas da aplicação.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Health checks (inclui conectividade com o Postgres).
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<OnboardingDbContext>("postgres");

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Exposto para os testes de integração (WebApplicationFactory<Program>).
public partial class Program;
