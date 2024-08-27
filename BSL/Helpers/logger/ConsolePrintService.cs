using System;
using System.Collections.Generic;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace BL.Helpers.logger
{
    public static class ConsolePrintService
    {
        public static void addressErrorPrint(string address)
        {
            errorPring($"Did not found address: {address}");
        }

        public static void exceptionErrorPrint(string error)
        {
            errorPring(error);
        }

        public static void driverRegestration(string message)
        {
            driverMessages(message);
        }
        private static void errorPring(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("######################################################################################");
            Console.WriteLine($"{text}");
            Console.WriteLine("######################################################################################");
            Console.ResetColor();
        }

        private static void driverMessages(string text)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("######################################################################################");
            Console.WriteLine(text);
            Console.WriteLine("######################################################################################");
            Console.ResetColor();
        }
    }
}
