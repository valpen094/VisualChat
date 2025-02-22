using System.Diagnostics;
using System.Reflection;

namespace ChatServer
{
    public enum LogLevel
    {
        Trace,
        Debug
    }

    public class Log : IDisposable
    {
        private string _className;
        private string _methodName;

        public Log(string className, string methodName)
        {
            _className = className;
            _methodName = methodName;
            WriteLine($"Start: {_className}.{_methodName}");
        }

        public void Dispose()
        {
            WriteLine($"End: {_className}.{_methodName}");
        }

        public void WriteLine(string message, LogLevel level = LogLevel.Debug)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logMessage = $"{timestamp} {message}";

            switch (level)
            {
                case LogLevel.Trace:
                    Trace.WriteLine(logMessage);
                    break;
                case LogLevel.Debug:
                    Debug.WriteLine(logMessage);
                    break;
            }
        }
    }

}
