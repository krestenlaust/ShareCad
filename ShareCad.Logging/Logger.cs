using System;
using System.Text;

namespace ShareCad.Logging
{
    public class Logger
    {
        private const char ParameterStart = '[';
        private const char ParameterEnd = ']';

        private readonly string prefix;
        private bool printTime;
        
        public Logger(string prefix, bool printTime)
        {
            this.prefix = prefix;
            this.printTime = printTime;
        }

        public void LogError(object value) => LogError(value.ToString());

        public void LogError(string value) => Log(value.ToString(), ConsoleColor.Red);

        public void Log(object value) => Log(value.ToString());

        public void Log(string value) => Log(value, ConsoleColor.White);

        private void Log(string value, ConsoleColor color)
        {
            StringBuilder sb = new StringBuilder(value.Length + 1);

            if (prefix != string.Empty)
            {
                sb.Append(ParameterStart);
                sb.Append(prefix);
                sb.Append(ParameterEnd);
            }

            if (printTime)
            {
                sb.Append(ParameterStart);
                sb.Append($"{DateTime.Now:HH:mm:ss}");
                sb.Append(ParameterEnd);
            }

            sb.Append(' ');
            sb.Append(value);

            Console.ForegroundColor = color;
            Console.WriteLine(sb);
        }
    }
}
