using System;
using System.Collections;
using System.Text;

namespace ShareCad.Logging
{
    public class Logger
    {
        private const char ParameterStart = '[';
        private const char ParameterEnd = ']';

        private readonly string prefix;
        private readonly bool printTime;

        public Logger(string prefix, bool printTime)
        {
            this.prefix = prefix;
            this.printTime = printTime;
        }

        public void PrintError(object value) => PrintError(value.ToString());

        public void PrintError(string value) => Print(value.ToString(), ConsoleColor.Red);

        public void PrintCollection(IEnumerable values)
        {
            Print("- Collection start");

            foreach (var item in values)
            {
                Print(item);
            }

            Print("- Collection end");
        }

        public void Print(object value) => Print(value.ToString());

        public void Print(string value) => Print(value, ConsoleColor.White);

        private void Print(string value, ConsoleColor color)
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
