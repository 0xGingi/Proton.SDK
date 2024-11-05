using System.Text.Json.Nodes;

namespace Proton.Sdk.Instrumentation.Metrics;

public abstract class Meter
{
    public abstract ICounter CreateCounter(string name, int version, JsonNode labels);
}
