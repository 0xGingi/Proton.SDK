using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Proton.Sdk.CExports.Logging;

using EventId = Microsoft.Extensions.Logging.EventId;

[StructLayout(LayoutKind.Sequential)]
internal sealed class InteropLogger(InteropLogCallback logCallback, string categoryName) : ILogger
{
    private readonly InteropLogCallback _logCallback = logCallback;
    private readonly string _categoryName = categoryName;

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        // TODO: add support for scopes?
        throw new NotSupportedException();
    }

    public unsafe void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter.Invoke(state, exception);
        var logEvent = new InteropLogEvent((byte)logLevel, InteropArray.Utf8FromString(message), InteropArray.Utf8FromString(_categoryName));

        _logCallback.Invoke(_logCallback.State, logEvent);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }
}
