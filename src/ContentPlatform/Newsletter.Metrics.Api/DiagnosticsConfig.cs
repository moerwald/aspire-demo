using System.Diagnostics.Metrics;

namespace Newsletter.Metrics.Api;

public static class DiagnosticsConfig
{
    public const string ServiceName = "Newsletter.Metrics.Api";

    public static Meter Meter = new(ServiceName);

    public static Counter<int> ArticlesCreatedCounter = Meter.CreateCounter<int>("articles_created.count");
}

