using DAL;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using telegramB.Menus;
using telegramB.Objects;
using Common.Services;
using Common.DTO;

namespace BL.Helpers
{
    public class DummyOrder
    {
        private static Dictionary<long, UserOrder> userOrders = new Dictionary<long, UserOrder>();
        DriverRepository driverRepository = new DriverRepository();
        private readonly SessionManager _sessionManager;

        public DummyOrder(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }
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
                NumberOfPassengers = 2,  // Default number of passengers
                userId = chatId,
                CurrentStep = "awaiting_bid"
            };

            userOrders[chatId] = dummyOrder;

            // Save session data for user order and state
            await _sessionManager.SetSessionData(chatId, "UserOrder", dummyOrder);
            await _sessionManager.SetSessionData(chatId, "UserState", "awaiting_bid"); // Set the state to prompt for phone number

            // Inform the user to enter the phone number
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "הכנס מספר טלפון להשלמת ההזמנה:",
                cancellationToken: cancellationToken
            );

            return dummyOrder;
        }

        public async Task AddDummyAddressesAsync(long chatId, ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            // Retrieve userOrder and userState from session storage
            var userOrder = await _sessionManager.GetSessionData<UserOrder>(chatId, "UserOrder");
            var userState = await _sessionManager.GetSessionData<string>(chatId, "UserState");

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
            userOrder.NumberOfPassengers = 2;  // Default number of passengers

            // Update the user state to indicate that phone number needs to be inserted
            userOrder.CurrentStep = "enter_phone";
            userState = "enter_phone";

            // Save the updated userOrder and userState to session storage
            await _sessionManager.SetSessionData(chatId, "UserOrder", userOrder);
            await _sessionManager.SetSessionData(chatId, "UserState", userState);

            // Notify the user to insert phone number
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "הכנס מספר טלפון להשלמת ההזמנה:",
                cancellationToken: cancellationToken
            );
        }
    }
}
