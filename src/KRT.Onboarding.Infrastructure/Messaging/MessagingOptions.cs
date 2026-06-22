namespace KRT.Onboarding.Infrastructure.Messaging;

/// <summary>Configuração da mensageria AWS (seção "Aws" do appsettings).</summary>
public sealed class MessagingOptions
{
    public const string SectionName = "Aws";

    /// <summary>Endpoint customizado (ex.: LocalStack http://localhost:4566). Vazio = AWS real.</summary>
    public string? ServiceUrl { get; init; }

    public string Region { get; init; } = "us-east-1";

    public string EventBusName { get; init; } = "krt-onboarding-bus";

    public string EventSource { get; init; } = "krt.onboarding";
}
