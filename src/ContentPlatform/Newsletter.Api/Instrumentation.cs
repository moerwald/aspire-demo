using System.Diagnostics;

namespace Newsletter.Api
{
    public class Instrumentation
    {
        public string SourceName { get; }
        public ActivitySource Source { get; }

        public Instrumentation(string appName)
        {
            SourceName = appName;
            Source = new(SourceName);
        }
    }
}
