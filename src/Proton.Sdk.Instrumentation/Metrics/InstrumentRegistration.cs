using System.Text.Json.Nodes;

namespace Proton.Sdk.Instrumentation.Metrics;

internal record InstrumentRegistration(string Name, int Version, JsonNode Labels, Instrument Instrument);
