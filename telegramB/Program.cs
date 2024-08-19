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
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using telegramB.ErrorHandle;
using telegramB.Helpers;
using telegramB.Objects;
using telegramB.Services;
using DAL;
using BL.Services.Customers.Handlers;
using BL.Services.Customers.Functions;
using StackExchange.Redis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Common.Services;
using BL.Helpers;

namespace telegramB
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await root(args);
        }

        private static async Task root(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            // Resolve and use the SessionManager
            var sessionManager = host.Services.GetRequiredService<SessionManager>();

            var getAddressFromLocation = new GetAddressFromLocationService();
            var orderRepository = new OrderRepository(); // Create an instance of OrderRepository
            var driverRepository = new DriverRepository(); // Create an instance of DriverRepository

            var userOrder = new UserOrder();
            var handleUser = new UserMenuHandle(getAddressFromLocation, orderRepository, driverRepository); // Pass the required arguments

            // Pass the sessionManager to HandleUserUpdateService
            var handleUpdate = new HandleUserUpdateService(userOrder, handleUser, sessionManager);

            var handleError = new HandleError();
            var botStarter = new starter(handleUpdate, handleError);

            await botStarter.StartAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Register services here
                    services.AddSingleton<SessionManager>(sp => new SessionManager("localhost:6379"));
                    services.AddTransient<DummyOrder>();
                    // Add other services you need
                });
    }
}
