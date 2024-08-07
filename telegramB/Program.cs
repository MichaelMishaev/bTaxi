using BL.Services.Drivers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
//using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using telegramB.ErrorHandle;
using telegramB.Helpers;
using telegramB.Objects;
using telegramB.Services;
using DAL;
using BL.Services.Customers.Handlers;
using BL.Services.Customers.Functions; // Add this line to include the DAL namespace

namespace telegramB
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await root();
        }

        private static async Task root()
        {

            //var botClient = new TelegramBotClient(TypesManual.BotToken);
            //await botClient.DeleteWebhookAsync();
            //Console.WriteLine("Webhook deleted successfully.");

            //var botClientD = new TelegramBotClient(TypesManual.DriverBotToken);
            //await botClientD.DeleteWebhookAsync();
            //Console.WriteLine("Webhook deleted successfully.");



            var getAddressFromLocation = new GetAddressFromLocationService();
            var orderRepository = new OrderRepository(); // Create an instance of OrderRepository
            var driverRepository = new DriverRepository(); // Create an instance of DriverRepository

            var userOrder = new UserOrder();
            var handleUser = new UserMenuHandle(getAddressFromLocation, orderRepository, driverRepository); // Pass the required arguments
            var handleUpdate = new HandleUserUpdateService(userOrder, handleUser);

            var handleError = new HandleError();
            var botStarter = new starter(handleUpdate, handleError);

            await botStarter.StartAsync();
        }
    }
}
