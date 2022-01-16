using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalAisle_Server
{
    public static class ConsoleLog
    {
        private static void WriteRaw(ConsoleColor colour, string tag, string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("[{0:MM/dd/yy H:mm:ss}]", DateTime.Now);
            Console.ForegroundColor = colour;
            Console.WriteLine("[" + tag + "] " + message);
        }

        /**
         * Call for output related to server status/condition
         */
        public static void WriteServerGeneral(string message)
        {
            WriteRaw(ConsoleColor.White, "Server", message);
        }

        public static void WriteServerImportant(string message)
        {
            WriteRaw(ConsoleColor.Green, "Server", message);
        }

        /**
         * Call for output related to data processing/storage
         */
        public static void WriteDataGeneral(string message)
        {
            WriteRaw(ConsoleColor.White, "Data", message);
        }

        public static void WriteDataImportant(string message)
        {
            WriteRaw(ConsoleColor.Cyan, "Data", message);
        }

        /**
         * Call for output related to user connections with the server
         */
        public static void WriteIOGeneral(string message)
        {
            WriteRaw(ConsoleColor.White, "I/O", message);
        }

        public static void WriteIOImportant(string message)
        {
            WriteRaw(ConsoleColor.Yellow, "I/O", message);
        }

        /**
         * Call for errors
         */
        public static void WriteErrorGeneral(string message)
        {
            WriteRaw(ConsoleColor.White, "Error", message);
        }

        public static void WriteErrorImportant(string message)
        {
            WriteRaw(ConsoleColor.Red, "Error", message);
        }

        /**
         * Call for output related to console commands 
         */
        public static void WriteInfo(string message)
        {
            WriteRaw(ConsoleColor.Gray, "Info", message);
        }
    }
}
