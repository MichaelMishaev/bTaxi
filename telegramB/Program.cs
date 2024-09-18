using System.Threading.Tasks;
using telegramB.ErrorHandle;
using telegramB.Objects;
using telegramB.Services;
using DAL;
using BL.Services.Customers.Functions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Common.Services;
using BL.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using static Org.BouncyCastle.Math.EC.ECCurve;
using System;

namespace telegramB
{
    class Program
    {
        static IConfigurationRoot sessionProp ;
        static async Task Main(string[] args)
        {
            sessionProp = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();
            await root(args);
        }

         
        //_sessionManager = new SessionManager(sessionProp["sessionManager:redis"]);
        private static async Task root(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            var sessionManager = host.Services.GetRequiredService<SessionManager>();
            var updateTypeMessage = new UpdateTypeMessage(sessionManager);

            var getAddressFromLocation = new GetAddressFromLocationService();
            var orderRepository = new OrderRepository(); // Create an instance of OrderRepository
            var driverRepository = new DriverRepository(); // Create an instance of DriverRepository

            var userOrder = new UserOrder();
            var handleUser = new UserMenuHandle(getAddressFromLocation, orderRepository, driverRepository, updateTypeMessage, sessionManager);
            // Pass the sessionManager to HandleUserUpdateService
            var handleUpdate = new HandleUserUpdateService(userOrder, handleUser, sessionManager);

            var handleError = new HandleError();
            var botStarter = new starter(handleUpdate, handleError, sessionManager);

            await botStarter.StartAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                  .ConfigureLogging(logging =>
                  {
                      logging.ClearProviders(); // This removes all default logging providers
                      logging.AddConsole(); // Adds console logging
                  })
                .ConfigureServices((hostContext, services) =>
                {
                    // Register services here
                    services.AddSingleton<SessionManager>(sp => new SessionManager(sessionProp["sessionManager:redis"]));
                    services.AddTransient<DummyOrder>();
                    // Add other services you need
                });
    }
}
