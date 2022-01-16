using System;

/**
 * Utility class responsible for formatting the console
 */
namespace Final_Aisle_Server
{
    public static class ConsoleLog
    {
        // for printing messages related to the major events in the status of the server (e.g. starting)
        public static void WriteBigStatus(string message) => PrintWithTimestamp(ConsoleColor.Green, "Server", message);
        // for printing messages related to the minor events in the status of the server (e.g. generating keys)
        public static void WriteSmallStatus(string message) => PrintWithTimestamp(ConsoleColor.White, "Server", message);

        // for printing messages related to significant errors (need attention)
        public static void WriteBigError(string message) => PrintWithTimestamp(ConsoleColor.Red, "Error", message);

        // for printing messages related to minor errors
        public static void WriteSmallError(string message) => PrintWithTimestamp(ConsoleColor.White, "Error", message);

        // for printing major messages related to connections and clients (e.g. player joining or player leaving)
        public static void WriteBigIO(string message) => PrintWithTimestamp(ConsoleColor.Yellow, "I/O", message);

        // for printing minor messages related to connections and clients (e.g. player moved a certain way, or to a certain place)
        public static void WriteSmallIO(string message) => PrintWithTimestamp(ConsoleColor.White, "I/O", message);

        // for printing major messages related to data transfer
        public static void WriteBigData(string message) => PrintWithTimestamp(ConsoleColor.Cyan, "Data", message);

        // for printing minor messages related to data transfer
        public static void WriteSmallData(string message) => PrintWithTimestamp(ConsoleColor.White, "Data", message);

        // for printing messages related to command information
        public static void WriteInfo(string message) => PrintWithTimestamp(ConsoleColor.White, "Info", message);

        /**
         * Prints a message prefixed with the timestamp
         */
        private static void PrintWithTimestamp(ConsoleColor colour, string prefix, string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("[" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "]");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("[" + prefix + "]: ");
            Console.ForegroundColor = colour;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
