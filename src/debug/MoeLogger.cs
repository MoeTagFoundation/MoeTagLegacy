using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoeTag.Debug
{
    public enum LogType
    {
        ERROR,
        LOG
    }

    public static class MoeLogger
    {
        public static LogType LogLevel = LogType.LOG;

        public static void Log(string source, string message, LogType type = LogType.LOG)
        {
            if (type == LogType.ERROR || type == LogType.LOG && LogLevel == LogType.LOG)
            {
                using (TextWriter writer = Console.Out)
                {
                    writer.WriteLine(type + ": [" + source + "] " + message);
                }
            }
        }

        public static void Log(object source, string message, LogType type = LogType.LOG)
        {
            Log(source.GetType().Name, message, type);
        }
    }
}
