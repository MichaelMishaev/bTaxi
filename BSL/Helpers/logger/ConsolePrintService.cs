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
        public static void CheckPointMessage(string message)
        {
            approvalMassages(message);
        }
        public static void simpleConsoleMessage(string message)
        {
            simpleMessage(message);
        }

        public static void consoleBusinessErrorMessage(string message)
        {
            businessErrorMessage(message);
        }


        private static void errorPring(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("######################################################################################");
            Console.WriteLine($"{text} : {DateTime.Now}");
            Console.WriteLine("######################################################################################");
            Console.ResetColor();
        }
        private static void driverMessages(string text)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("######################################################################################");
            Console.WriteLine($"{text} : {DateTime.Now}");
            Console.WriteLine("######################################################################################");
            Console.ResetColor();
        }
        private static void approvalMassages(string text)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("######################################################################################");
            Console.WriteLine($"{text} : {DateTime.Now}");
            Console.WriteLine("######################################################################################");
            Console.ResetColor();
        }
        private static void simpleMessage(string text)
        {
            {
                //Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("######################################################################################");
                Console.WriteLine($"{text} : {DateTime.Now}");
                Console.WriteLine("######################################################################################");
                Console.ResetColor();
            }
        }
        private static void businessErrorMessage(string text)
        {
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("######################################################################################");
                Console.WriteLine($"{text} : {DateTime.Now}");
                Console.WriteLine("######################################################################################");
                Console.ResetColor();
            }
        }

    }
}
