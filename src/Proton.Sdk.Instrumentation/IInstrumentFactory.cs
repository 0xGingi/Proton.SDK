namespace Proton.Sdk.Instrumentation;

public interface IInstrumentFactory
{
    ICounter CreateCounter(string name, int version, object labels);
}
