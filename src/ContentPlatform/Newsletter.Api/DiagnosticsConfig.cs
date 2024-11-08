using System.Diagnostics;

namespace Newsletter.Api
{
    public class DiagnosticsConfig
    {
        public const string SourceName = "newsletter-api";
        public ActivitySource Source { get; } = new(SourceName);
    }
}
