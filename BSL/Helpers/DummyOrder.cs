using DAL;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using telegramB.Menus;
using telegramB.Objects;
using telegramB;
using Common.Services;
using Common.DTO;

namespace BL.Helpers
{
    public class DummyOrder
    {
        private static Dictionary<long, UserOrder> userOrders = new Dictionary<long, UserOrder>();
        DriverRepository driverRepository = new DriverRepository();
        public async Task<UserOrder> CreateDummyOrderAsync(long chatId, ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            OrderRepository orderRepository = new OrderRepository();
            var dummyOrder = new UserOrder
            {
                FromAddress = new AddressDTO
                {
                    Street = "ויצמן",
                    StreetNumber = 5,
                    City = "נתניה"
                },
                ToAddress = new AddressDTO
                {
                    Street = "דרך הציונות",
                    StreetNumber = 5,
                    City = "אריאל"
                },
                PhoneNumber = "123456789",
                Remarks = "This is a test order",
                userId = chatId,
                price = 100
            };

            userOrders[chatId] = dummyOrder;

            int orderId = await orderRepository.PlaceOrderAsync(dummyOrder, 666);

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Dummy order created successfully. Waiting for drivers to respond.",
                cancellationToken: cancellationToken
            );

            string separator = new string('-', 30); // Separator line
            string dateTimeNow = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); // Formatted date-time
            string orderNotification = $"{separator}\n" +
                                       $"הודעה חדשה התקבלה בתאריך: {dateTimeNow}\n\n" + // Add formatted date-time
                                       $"הזמנה חדשה התקבלה!\n" +
                                       $"מ: {dummyOrder.FromAddress}\n" +
                                       $"אל: {dummyOrder.ToAddress}\n" +
                                       $"מחיר: {dummyOrder.price}\n" +
                                       $"מספר טלפון: {dummyOrder.PhoneNumber}\n" +
                                       $"הערות: {dummyOrder.Remarks}";


            var workingDrivers = await driverRepository.GetWorkingDriversAsync();

            foreach (var driver in workingDrivers)
            {
                long driverChatId = Convert.ToInt64(driver.DriverId);

                try
                {
                    await TypesManual.botDriver.SendTextMessageAsync(
                        chatId: driverChatId,
                        text: orderNotification,
                        cancellationToken: cancellationToken
                    );

                    var orderActionsMenu = MenuMethods.GetOrderActionsMenu(orderId);
                    await TypesManual.botDriver.SendTextMessageAsync(
                        chatId: driverChatId,
                        text: "בחר פעולה:",
                        replyMarkup: orderActionsMenu,
                        cancellationToken: cancellationToken
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send message to driver {driver.DriverId}: {ex.Message}");
                }
            }
            return dummyOrder;
        }
        public async Task AddDummyAddressesAsync(long chatId, ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            // Retrieve userOrder and userState from session storage
            var userOrder = SessionManager.GetSessionData<UserOrder>(chatId, "UserOrder");
            var userState = SessionManager.GetSessionData<string>(chatId, "UserState");

            if (userOrder == null)
            {
                userOrder = new UserOrder();
            }

            userOrder.FromAddress = new AddressDTO
            {
                Street = "דרך הציונות",
                StreetNumber = 20,
                City = "אריאל"
            };

            userOrder.ToAddress = new AddressDTO
            {
                Street = "ויצמן",
                StreetNumber = 1,
                City = "נתניה"
            };

            userOrder.PhoneNumber = "0544654456";

            // Update the user state to indicate that addresses and phone number have been inserted
            userState = "addresses_phone_inserted";

            // Save the updated userOrder and userState to session storage
            SessionManager.SetSessionData(chatId, "UserOrder", userOrder); // Save session data
            SessionManager.SetSessionData(chatId, "UserState", userState); // Save session data

            // Notify the user that the addresses and phone number have been inserted
            var summaryText = $"כתובת נקודת האיסוף: {userOrder.FromAddress.Street} {userOrder.FromAddress.StreetNumber}, {userOrder.FromAddress.City}\n" +
                              $"כתובת יעד: {userOrder.ToAddress.Street} {userOrder.ToAddress.StreetNumber}, {userOrder.ToAddress.City}\n" +
                              $"מספר טלפון: {userOrder.PhoneNumber}";

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: summaryText,
                replyMarkup: MenuMethods.ShowUpdatedMenu(userOrder),
                cancellationToken: cancellationToken
            );
        }

    }
}
