namespace Proton.Sdk.Instrumentation;

internal record CounterDefinition(string Name, int Version, object Labels, Counter Counter);
