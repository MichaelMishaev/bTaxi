using BL.Helpers;
using Common.DTO;
using Common.Services;
using DAL;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using telegramB;
using telegramB.Menus;
using telegramB.Objects;
using telegramB.Services;

namespace BL.Services.Customers.Functions
{
    public static class StepIdentifier
    {
        private static readonly SessionManager _sessionManager;
        static StepIdentifier()
        {
            _sessionManager = new SessionManager("localhost:6379");
        }
        public static async Task UserLogic(long chatId, string input, CancellationToken cancellationToken, ITelegramBotClient botClient, UserOrder userOrder, string userState, Message message, GetAddressFromLocationService _getAddressFromLocation = null, bool isDriver = false)
        {
            OrderRepository _orderRepository = new OrderRepository();
            DriverRepository _driverRepository = new DriverRepository();
            string extraData = userState?.Split(':').Length > 1 ? userState.Split(':')[1] : null;

            switch (userOrder.CurrentStep)
            {
                case "from":
                case "from_confirm":
                    await HandleFromStep(chatId, input, cancellationToken, botClient, userOrder, message, _getAddressFromLocation);
                    break;

                case "to":
                case "to_confirm":
                    await HandleToStep(chatId, input, cancellationToken, botClient, userOrder);
                    break;

                case "passengers":
                case "passengers_confirm":
                    await HandlePassengersStep(chatId, input, cancellationToken, botClient, userOrder);
                    break;

                case "phone":
                case "phone_confirm":
                    await HandlePhoneStep(chatId, input, cancellationToken, botClient, userOrder);
                    break;

                case "remarks":
                case "remarks_confirm":
                    await HandleRemarksStep(chatId, input, cancellationToken, botClient, userOrder);
                    break;

                case "awaiting_new_customer_bid":
                    await HandleAwaitingNewCustomerBidStep(chatId, input, cancellationToken, botClient, userOrder, extraData, _orderRepository, _driverRepository, userState);
                    break;

                case "awaiting_bid":
                    await HandleAwaitingBidStep(chatId, input, cancellationToken, botClient, userOrder, userState, _orderRepository, _driverRepository);
                    break;

                default:
                    break;
            }
        }

        private static async Task HandleFromStep(long chatId, string input, CancellationToken cancellationToken, ITelegramBotClient botClient, UserOrder userOrder, Message message, GetAddressFromLocationService _getAddressFromLocation)
        {
            var confirmationButtons = MenuMethods.YesNoAnswer();

            if (userOrder.CurrentStep == "from_confirm")
            {
                if (input.ToLower() == "yes")
                {
                    userOrder.FromAddress = userOrder.PendingAddress;
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"נקודת איסוף {userOrder.FromAddress.GetFormattedAddress()} נשמרה. אנא בחר באפשרות הבאה.",
                        replyMarkup: MenuMethods.ShowUpdatedMenu(userOrder),
                        cancellationToken: cancellationToken
                    );
                }
                else if (input.ToLower() == "no")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "השינוי בוטל.",
                        cancellationToken: cancellationToken
                    );
                }
                userOrder.CurrentStep = "from";
                userOrder.PendingAddress = null;
            }
            else
            {
                if (!string.IsNullOrEmpty(userOrder.FromAddress?.GetFormattedAddress()))
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "נקודת האיסוף כבר קיימת. האם אתה בטוח שברצונך להחליף?",
                        replyMarkup: confirmationButtons,
                        cancellationToken: cancellationToken
                    );

                    var address = message.Type == MessageType.Location
                        ? await _getAddressFromLocation.GetAddressFromLocationAsync(message.Location.Latitude, message.Location.Longitude)
                        : await GetAddressOrPromptUser(input, botClient, chatId, cancellationToken);

                    userOrder.PendingAddress = address;
                    userOrder.CurrentStep = "from_confirm";
                }
                else
                {
                    var address = message.Type == MessageType.Location
                        ? await _getAddressFromLocation.GetAddressFromLocationAsync(message.Location.Latitude, message.Location.Longitude)
                        : await GetAddressOrPromptUser(input, botClient, chatId, cancellationToken);

                    userOrder.FromAddress = address;
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"נקודת איסוף {userOrder.FromAddress.GetFormattedAddress()} נשמרה. אנא בחר באפשרות הבאה.",
                        replyMarkup: MenuMethods.ShowUpdatedMenu(userOrder),
                        cancellationToken: cancellationToken
                    );
                }
            }
        }

        private static async Task HandleToStep(long chatId, string input, CancellationToken cancellationToken, ITelegramBotClient botClient, UserOrder userOrder)
        {
            var confirmationButtons = MenuMethods.YesNoAnswer();

            if (userOrder.CurrentStep == "to_confirm")
            {
                if (input.ToLower() == "yes")
                {
                    userOrder.ToAddress = userOrder.PendingAddress;
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"היעד {userOrder.ToAddress.GetFormattedAddress()} נשמר. אנא בחר באפשרות הבאה.",
                        replyMarkup: MenuMethods.ShowUpdatedMenu(userOrder),
                        cancellationToken: cancellationToken
                    );
                }
                else if (input.ToLower() == "no")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "השינוי בוטל.",
                        cancellationToken: cancellationToken
                    );
                }
                userOrder.CurrentStep = "to";
                userOrder.PendingAddress = null;
            }
            else
            {
                if (!string.IsNullOrEmpty(userOrder.ToAddress?.GetFormattedAddress()))
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "היעד כבר קיים. האם אתה בטוח שברצונך להחליף?",
                        replyMarkup: confirmationButtons,
                        cancellationToken: cancellationToken
                    );

                    var address = ParseAddress(input);
                    userOrder.PendingAddress = address;
                    userOrder.CurrentStep = "to_confirm";
                }
                else
                {
                    userOrder.ToAddress = ParseAddress(input);
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"היעד {userOrder.ToAddress.GetFormattedAddress()} נשמר. אנא בחר באפשרות הבאה.",
                        replyMarkup: MenuMethods.ShowUpdatedMenu(userOrder),
                        cancellationToken: cancellationToken
                    );
                }
            }
        }

        private static async Task<AddressDTO> GetAddressOrPromptUser(string input, ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var address = ParseAddress(input);
            if (address == null)
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "הכתובת שהוזנה לא נמצאה. אנא הזן כתובת ידנית:",
                    cancellationToken: cancellationToken
                );
                // Here, you can handle retrying by setting a flag or step in the user state to prompt for manual address input
            }
            return address;
        }
        private static AddressDTO ParseAddress(string input)
        {
            // Implement your parsing logic here to convert the input string into an AddressDTO
            // This is a placeholder implementation
            var parts = input.Split(',');
            return new AddressDTO
            {
                City = parts.Length > 0 ? parts[0].Trim() : "Unknown City",
                Street = parts.Length > 1 ? parts[1].Trim() : "Unknown Street",
                StreetNumber = parts.Length > 2 && int.TryParse(parts[2].Trim(), out var number) ? number : 0
            };
        }

        private static async Task HandlePassengersStep(long chatId, string input, CancellationToken cancellationToken, ITelegramBotClient botClient, UserOrder userOrder)
        {
            var confirmationButtons = MenuMethods.YesNoAnswer();

            if (userOrder.CurrentStep == "passengers_confirm")
            {
                if (input.ToLower() == "yes")
                {
                    if (int.TryParse(userOrder.PendingValue, out int passengersCopy))
                    {
                        userOrder.NumberOfPassengers = passengersCopy;
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: $"מספר נוסעים {userOrder.NumberOfPassengers} נשמר. אנא בחר באפשרות הבאה.",
                            replyMarkup: MenuMethods.ShowUpdatedMenu(userOrder),
                            cancellationToken: cancellationToken
                        );
                    }
                }
                else if (input.ToLower() == "no")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "השינוי בוטל.",
                        cancellationToken: cancellationToken
                    );
                }
                userOrder.CurrentStep = "passengers";
                userOrder.PendingValue = null;
            }
            else
            {
                if (userOrder.NumberOfPassengers > 0)
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "מספר הנוסעים כבר קיים. האם אתה בטוח שברצונך להחליף?",
                        replyMarkup: confirmationButtons,
                        cancellationToken: cancellationToken
                    );
                    userOrder.PendingValue = input;
                    userOrder.CurrentStep = "passengers_confirm";
                }
                else
                {
                    if (int.TryParse(input, out int passengers))
                    {
                        userOrder.NumberOfPassengers = passengers;
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: $"מספר נוסעים {input} נשמר. אנא בחר באפשרות הבאה.",
                            replyMarkup: MenuMethods.ShowUpdatedMenu(userOrder),
                            cancellationToken: cancellationToken
                        );
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "אנא הזן מספר נוסעים תקין.",
                            replyMarkup: MenuMethods.ShowUpdatedMenu(userOrder),
                            cancellationToken: cancellationToken
                        );
                    }
                }
            }
        }

        private static async Task HandlePhoneStep(long chatId, string input, CancellationToken cancellationToken, ITelegramBotClient botClient, UserOrder userOrder)
        {
            var confirmationButtons = MenuMethods.YesNoAnswer();

            if (userOrder.CurrentStep == "phone_confirm")
            {
                if (input.ToLower() == "yes")
                {
                    userOrder.PhoneNumber = userOrder.PendingValue;
                    var isValidPhoneCopy = await Validators.PhoneValidator(userOrder.PhoneNumber);
                    if (!isValidPhoneCopy)
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "אנא הזן מספר טלפון תקין.",
                            cancellationToken: cancellationToken
                        );
                        userOrder.CurrentStep = "phone"; // Set the step back to phone to retry
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "מספר טלפון נשמר.",
                            cancellationToken: cancellationToken
                        );

                        // Transition to the bidding phase
                        userOrder.CurrentStep = "awaiting_bid"; // Move to the next step
                    }
                }
                else if (input.ToLower() == "no")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "השינוי בוטל.",
                        replyMarkup: MenuMethods.ShowUpdatedMenu(userOrder),
                        cancellationToken: cancellationToken
                    );
                    userOrder.CurrentStep = "phone"; // Set the step back to phone to retry
                }
                userOrder.PendingValue = null;
            }
            else
            {
                var isValidPhone = await Validators.PhoneValidator(input);
                if (!isValidPhone)
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "אנא הזן מספר טלפון תקין.",
                        cancellationToken: cancellationToken
                    );
                    userOrder.CurrentStep = "phone"; // Set the step back to phone to retry
                }
                else
                {
                    if (!string.IsNullOrEmpty(userOrder.PhoneNumber))
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "מספר הטלפון כבר קיים. האם אתה בטוח שברצונך להחליף?",
                            replyMarkup: confirmationButtons,
                            cancellationToken: cancellationToken
                        );
                        userOrder.PendingValue = input;
                        userOrder.CurrentStep = "phone_confirm";
                    }
                    else
                    {
                        userOrder.PhoneNumber = input;
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "מספר טלפון נשמר.",
                            cancellationToken: cancellationToken
                        );

                        // Transition to the bidding phase
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: $"הטלפון {input} נשמר. אנא בחר באפשרות הבאה.",
                            replyMarkup: MenuMethods.ShowUpdatedMenu(userOrder),
                            cancellationToken: cancellationToken
                        );
                        userOrder.CurrentStep = "awaiting_bid"; // Move to the next step
                    }
                }
            }
        }

        private static async Task HandleRemarksStep(long chatId, string input, CancellationToken cancellationToken, ITelegramBotClient botClient, UserOrder userOrder)
        {
            var confirmationButtons = MenuMethods.YesNoAnswer();

            if (userOrder.CurrentStep == "remarks_confirm")
            {
                if (input.ToLower() == "yes")
                {
                    userOrder.Remarks = userOrder.PendingValue;
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "ההערות נשמרו בהצלחה",
                        replyMarkup: MenuMethods.ShowUpdatedMenu(userOrder),
                        cancellationToken: cancellationToken
                    );
                }
                else if (input.ToLower() == "no")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "השינוי בוטל.",
                        cancellationToken: cancellationToken
                    );
                }
                userOrder.CurrentStep = "remarks";
                userOrder.PendingValue = null;
            }
            else
            {
                if (!string.IsNullOrEmpty(userOrder.Remarks))
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "ההערות כבר קיימות. האם אתה בטוח שברצונך להחליף?",
                        replyMarkup: confirmationButtons,
                        cancellationToken: cancellationToken
                    );
                    userOrder.PendingValue = input;
                    userOrder.CurrentStep = "remarks_confirm";
                }
                else
                {
                    userOrder.Remarks = input;
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "ההערות נשמרו בהצלחה",
                        replyMarkup: MenuMethods.ShowUpdatedMenu(userOrder),
                        cancellationToken: cancellationToken
                    );
                }
            }
        }

        private static async Task HandleAwaitingNewCustomerBidStep(long chatId, string input, CancellationToken cancellationToken, ITelegramBotClient botClient, UserOrder userOrder, string extraData, OrderRepository _orderRepository, DriverRepository _driverRepository, string userState)
        {
            if (decimal.TryParse(input, out decimal newCustomerBid))
            {
                var customerId = long.Parse(extraData);

                await _orderRepository.InsertAndThenUpdateCustomerBidAsync(chatId, customerId, newCustomerBid);

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "הצעת המחיר החדשה שלך נשמרה בהצלחה.",
                    cancellationToken: cancellationToken
                );

                var workingDrivers = await _driverRepository.GetWorkingDriversAsync();
                foreach (var driver in workingDrivers)
                {
                    Console.WriteLine($"Message sent to: {workingDrivers.Count} drivers");
                    long driverChatId = Convert.ToInt64(driver.DriverId);

                    var orderActionsMenu = MenuMethods.orderActionsMenu(chatId);

                    await TypesManual.botDriver.SendTextMessageAsync(
                        chatId: driverChatId,
                        text: $"לקוח הציע מחיר חדש עבור הנסיעה. הצעת מחיר: {newCustomerBid:F2} ₪",
                        replyMarkup: orderActionsMenu,
                        cancellationToken: cancellationToken
                    );
                }

                userOrder.BidAmount = newCustomerBid;
                userOrder.CurrentStep = "awaiting_confirmation";
                userState = "awaiting_confirmation";

                await _sessionManager.SetSessionData(chatId, "UserOrder", userOrder); //**********************************
                await _sessionManager.SetSessionData(chatId, "UserState", userState); //**********************************
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "הצעת מחיר לא תקינה. אנא הזן מספר תקין:",
                    cancellationToken: cancellationToken
                );
            }
        }

        private static async Task HandleAwaitingBidStep(long chatId, string input, CancellationToken cancellationToken, ITelegramBotClient botClient, UserOrder userOrder, string userState, OrderRepository _orderRepository, DriverRepository _driverRepository)
        {
            if (decimal.TryParse(input, out decimal customerBid))
            {
                var parentId = long.Parse(userState.Split(':')[1]);

                long bidId = await _orderRepository.InsertBidAsync(parentId, chatId, chatId, chatId, customerBid, false);

                userOrder.BidId = bidId;

                await _orderRepository.UpdateOrderStepAsync(chatId, "awaiting_driver_bid");

                var workingDrivers = await _driverRepository.GetWorkingDriversAsync();
                foreach (var driver in workingDrivers)
                {
                    long driverChatId = Convert.ToInt64(driver.DriverId);
                    var bidOptions = MenuMethods.AwaitDriverBidResponse(parentId, customerBid);

                    await TypesManual.botDriver.SendTextMessageAsync(
                        chatId: driverChatId,
                        text: $"לקוח הציע מחיר חדש עבור הנסיעה. הצעת מחיר: {customerBid:F2} ₪",
                        replyMarkup: bidOptions,
                        cancellationToken: cancellationToken
                    );
                }

                userOrder.CurrentStep = "awaiting_driver_bid";
                userState = $"awaiting_driver_bid:{parentId}";

                // Save session data
                await _sessionManager.SetSessionData(chatId, "UserOrder", userOrder); //**********************************
                await _sessionManager.SetSessionData(chatId, "UserState", userState); //**********************************
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "הצעת מחיר לא תקינה. אנא הזן מספר תקין:",
                    cancellationToken: cancellationToken
                );
            }
        }
    }
}
