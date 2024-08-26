using BL.Helpers;
using BL.Services.Customers.Handlers;
using BL.Services.Drivers.StaticFiles;
using Common.DTO;
using Common.Services;
using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using telegramB.Helpers;
using telegramB.Menus;
using telegramB.Objects;
using telegramB.Services;

namespace BL.Services.Customers.Functions
{
    public class UserMenuHandle
    {
        private readonly GetAddressFromLocationService _getAddressFromLocation;
        private readonly OrderRepository _orderRepository;
        private readonly DriverRepository _driverRepository;
        private readonly UpdateTypeMessage _updateTypeMessage;
        private readonly SessionManager _sessionManager;

        public UserMenuHandle(GetAddressFromLocationService getAddressFromLocation, OrderRepository orderRepository, DriverRepository driverRepository, UpdateTypeMessage updateTypeMessage, SessionManager sessionManager)
        {
            _getAddressFromLocation = getAddressFromLocation;
            _orderRepository = orderRepository;
            _driverRepository = driverRepository;
            _updateTypeMessage = updateTypeMessage;
            _sessionManager = sessionManager;
        }

        public async Task HandleUserInput(long chatId, string input, CancellationToken cancellationToken, ITelegramBotClient botClient, UserOrder userOrder, string userState, Message message, bool isDriver = false)
        {
            if (userOrder == null)
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "No active order found. Please start a new order.",
                    cancellationToken: cancellationToken
                );

                await _updateTypeMessage.ResetSessionData(chatId, cancellationToken, botClient);
                return;
            }

            if (userState == null)
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "No active state found. Please start a new order.",
                    cancellationToken: cancellationToken
                );

                await _updateTypeMessage.ResetSessionData(chatId, cancellationToken, botClient);
                return;
            }

            var confirmationButtons = MenuMethods.YesNoAnswer();

            if (!isDriver)
            {

                // Handling address input steps
                switch (userOrder.CurrentStep)
                {
                    case "entering_origin_city":
                        if (message.Type == MessageType.Location)
                        {
                            // Handle location input
                            var address = await _getAddressFromLocation.GetAddressFromLocationAsync(message.Location.Latitude, message.Location.Longitude);
                            userOrder.FromAddress = address;
                            userOrder.CurrentStep = "entering_destination_city"; // Move to next step


                            await botClient.SendTextMessageAsync(
                                            chatId: chatId,
                                            text: " מיקום התקבל", // Empty message or just a space
                                            replyMarkup: new ReplyKeyboardRemove(),
                                            cancellationToken: cancellationToken
                                        );


                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "הכנס עיר יעד:",
                                cancellationToken: cancellationToken
                            );
                        }
                        else
                        {
                            if (input == null) { await _updateTypeMessage.ResetSessionData(chatId, cancellationToken, botClient); break; }

                            if (keywords.knownLocations.Any(location => input.Trim().Equals(location, StringComparison.OrdinalIgnoreCase)))
                            {
                                userOrder.FromAddress = new AddressDTO { City = "נמל תעופה בן גוריון", Street = "טרמינל", StreetNumber = 3 }; // Default values for airport
                                userOrder.CurrentStep = "confirm_origin_address";
                                var formattedAddress = userOrder.FromAddress.GetFormattedAddress();
                                await botClient.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: $"כתובת מוצא שנשמרה היא: {formattedAddress}. האם זה נכון?",
                                    replyMarkup: confirmationButtons,
                                    cancellationToken: cancellationToken
                                );
                            }
                            else
                            {
                                // Handle manual city input
                                userOrder.FromAddress = new AddressDTO { City = input };
                                userOrder.CurrentStep = "entering_origin_street";
                                await botClient.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: "הכנס רחוב מוצא:",
                                    replyMarkup: new ReplyKeyboardRemove(), // Remove location button
                                    cancellationToken: cancellationToken
                                );
                            }
                        }
                        break;

                    case "entering_origin_street":
                        userOrder.FromAddress.Street = input;
                        userOrder.CurrentStep = "entering_origin_street_number";
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "הכנס מספר רחוב:",
                            cancellationToken: cancellationToken
                        );
                        break;

                    case "entering_origin_street_number":
                        if (int.TryParse(input, out int originStreetNumber))
                        {
                            userOrder.FromAddress.StreetNumber = originStreetNumber;
                            userOrder.CurrentStep = "confirm_origin_address";
                            var formattedAddress = userOrder.FromAddress.GetFormattedAddress();
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: $"כתובת המוצא שנשמרה היא: {formattedAddress}. האם זה נכון?",
                                replyMarkup: confirmationButtons,
                                cancellationToken: cancellationToken
                            );
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "מספר רחוב לא תקין. אנא הזן מספר תקין:",
                                cancellationToken: cancellationToken
                            );
                        }
                        break;

                    case "confirm_origin_address":
                        if (input.ToLower() == "yes")
                        {
                            userOrder.CurrentStep = "entering_destination_city";
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "הכנס עיר יעד:",
                                cancellationToken: cancellationToken
                            );
                        }
                        else if (input.ToLower() == "no")
                        {
                            userOrder.CurrentStep = "entering_origin_city";
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "בבקשה הזן את כתובת המוצא שוב. הכנס עיר:",
                                cancellationToken: cancellationToken
                            );
                        }
                        else
                        {

                            await _sessionManager.RemoveSessionData(chatId, "UserState");
                            await _sessionManager.RemoveSessionData(chatId, "UserOrder");


                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "אני עדיין נמצא בתהליכי לימוד, לא מצליח להבין מה אתם רוצים 😞",
                                cancellationToken: cancellationToken
                            );

                            await botClient.SendTextMessageAsync(
                             chatId: chatId,
                             text: "בואו נתחיל מהתחלה",
                             cancellationToken: cancellationToken
                         );

                            var mainMenuButtons = MenuMethods.mainMenuButtons();
                            await botClient.SendTextMessageAsync(
                             chatId: chatId,
                             text: "אנא בחר אפשרות",
                             replyMarkup: mainMenuButtons,
                             cancellationToken: cancellationToken
                 );
                        }
                        break;

                    case "entering_destination_city":
                        if (input == null) { await _updateTypeMessage.ResetSessionData(chatId, cancellationToken, botClient); break; }

                        if (keywords.knownLocations.Any(location => input.Trim().Equals(location, StringComparison.OrdinalIgnoreCase)))
                        {
                            userOrder.ToAddress = new AddressDTO { City = "נמל תעופה בן גוריון", Street = "טרמינל", StreetNumber = 3 }; // Default values for airport
                            userOrder.CurrentStep = "confirm_destination_address";
                            var formattedAddress = userOrder.ToAddress.GetFormattedAddress();
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: $"כתובת היעד שנשמרה היא: {formattedAddress}. האם זה נכון?",
                                replyMarkup: confirmationButtons,
                                cancellationToken: cancellationToken
                            );
                        }
                        else
                        {
                            userOrder.ToAddress = new AddressDTO { City = input };
                            userOrder.CurrentStep = "entering_destination_street";
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "הכנס רחוב יעד:",
                                cancellationToken: cancellationToken
                            );
                        }
                        break;
                    //userOrder.ToAddress = new AddressDTO { City = input };
                    //userOrder.CurrentStep = "entering_destination_street";
                    //await botClient.SendTextMessageAsync(
                    //    chatId: chatId,
                    //    text: "הכנס רחוב יעד:",
                    //    cancellationToken: cancellationToken
                    //);
                    //break;

                    case "entering_destination_street":
                        userOrder.ToAddress.Street = input;
                        userOrder.CurrentStep = "entering_destination_street_number";
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "הכנס מספר בית יעד:",
                            cancellationToken: cancellationToken
                        );
                        break;

                    case "entering_destination_street_number":
                        if (int.TryParse(input, out int destinationStreetNumber))
                        {
                            userOrder.ToAddress.StreetNumber = destinationStreetNumber;
                            userOrder.CurrentStep = "confirm_destination_address";
                            var formattedAddress = userOrder.ToAddress.GetFormattedAddress();
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: $"כתובת היעד שנשמרה היא: {formattedAddress}. האם זה נכון?",
                                replyMarkup: confirmationButtons,
                                cancellationToken: cancellationToken
                            );
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "מספר בית לא תקין. אנא הזן מספר תקין:",
                                cancellationToken: cancellationToken
                            );
                        }
                        break;


                    case "confirm_destination_address":
                        if (input.ToLower() == "yes")
                        {
                            userOrder.CurrentStep = "enter_passengers";
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "הכנס מספר נוסעים:",
                                cancellationToken: cancellationToken
                            );
                        }
                        else if (input.ToLower() == "no")
                        {
                            userOrder.CurrentStep = "entering_destination_city";
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "בבקשה הזן את כתובת היעד שוב. הכנס עיר:",
                                cancellationToken: cancellationToken
                            );
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"should not get here {DateTime.Now}");
                            Console.WriteLine($"The input is: {input}");
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "משהו פה הסתבך 🫣, אל יאוש, נתחיל מחדש 🫡",
                                cancellationToken: cancellationToken
                            );

                            Console.ResetColor();

                            await _updateTypeMessage.ResetSessionData(chatId, cancellationToken, botClient);
                        }
                        break;

                    case "enter_passengers":
                        if (int.TryParse(input, out int passengers))
                        {
                            userOrder.NumberOfPassengers = passengers;
                            userOrder.CurrentStep = "enter_phone";
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "הכנס מספר טלפון:",
                                cancellationToken: cancellationToken
                            );
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "מספר נוסעים לא תקין. אנא הזן מספר תקין:",
                                cancellationToken: cancellationToken
                            );
                        }
                        break;

                    case "enter_phone":
                        userOrder.PhoneNumber = input;

                        // Save session data before displaying the order summary
                        //SessionManager.SetSessionData(chatId, "UserOrder", userOrder);
                        //SessionManager.SetSessionData(chatId, "UserState", userState);

                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "רק רגע,  מחשב מחיר מונית רגילה......",
                            cancellationToken: cancellationToken
                        );

                        // Display order summary with confirmation buttons
                        var res = await DisplayAndSubmitOrder.DisplayOrderSummary(chatId, botClient, userOrder, cancellationToken);

                        if (!res) break; // if address wrong abort

                        // Set the current step to confirm the order
                        userOrder.CurrentStep = "enter_bid"; // Updated to prompt for bid
                        userState = "awaiting_bid"; // Updated to awaiting bid

                        // Save updated state to session
                        //####################### saves anyway in the end ####################
                        //SessionManager.SetSessionData(chatId, "UserOrder", userOrder);
                        //SessionManager.SetSessionData(chatId, "UserState", userState);
                        break;


                    case "confirm_order":
                        if (input.ToLower() == "confirm")
                        {
                            userOrder.CurrentStep = "enter_bid";
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "הכנס את המחיר שאתה מציע עבור הנסיעה:",
                                cancellationToken: cancellationToken
                            );
                        }
                        else if (input.ToLower() == "cancel")
                        {
                            await _updateTypeMessage.ResetSessionData(chatId, cancellationToken, botClient);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "תשובה לא תקינה. אנא בחר באפשרות אחת",
                                cancellationToken: cancellationToken
                            );
                        }
                        break;


                    case "enter_bid":
                        if (decimal.TryParse(input, out decimal bidAmount))
                        {
                            userOrder.BidAmount = bidAmount;

                            // Notify that the system is searching for a driver
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "מחפש נהג...",
                                cancellationToken: cancellationToken
                            );

                            // Proceed to the next step after bid amount is entered
                            userOrder.CurrentStep = "order_confirmed";

                            // Save session data
                            await _sessionManager.SetSessionData(chatId, "UserOrder", userOrder);
                            await _sessionManager.SetSessionData(chatId, "UserState", userState);

                            // Implement logic to find a driver and update the order status accordingly
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "הצעת מחיר לא תקינה. אנא הזן מספר תקין:",
                                cancellationToken: cancellationToken
                            );
                        }
                        break;

                    default:
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "פקודה לא קיימת, התחל מחדש",
                            replyMarkup: MenuMethods.mainMenuButtons(),
                            cancellationToken: cancellationToken
                        );
                        break;

                        //default:
                        //    await StepIdentifier.UserLogic(chatId, input, cancellationToken, botClient, userOrder, userState, message, _getAddressFromLocation, false);
                        //    break;
                }

                // Update the current step in the database
                //await _orderRepository.UpdateOrderStepAsync(chatId, userOrder.CurrentStep);
            }

            // Save the updated session data
            await _sessionManager.SetSessionData(chatId, "UserOrder", userOrder);
            await _sessionManager.SetSessionData(chatId, "UserState", userState);
        }

        private static string GetOrderSummary(UserOrder userOrder)
        {
            return $"סיכום ההזמנה שלך:\n" +
                   $"נקודת איסוף: {userOrder.FromAddress.GetFormattedAddress()}\n" +
                   $"יעד: {userOrder.ToAddress.GetFormattedAddress()}\n" +
                   $"מחיר מוצע: {userOrder.BidAmount:F2} ₪\n" +
                   $"מספר טלפון: {userOrder.PhoneNumber}\n" +
                   $"הערות: {userOrder.Remarks ?? "לא"}";
        }
    }

}

public static class AddressExtensions
{
    public static string GetFormattedAddress(this AddressDTO address)
    {
        return $"{address.Street} {address.StreetNumber}, {address.City}";
    }
}
