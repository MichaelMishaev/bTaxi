using BL.Helpers;
using BL.Services.Customers.Handlers;
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

        public UserMenuHandle(GetAddressFromLocationService getAddressFromLocation, OrderRepository orderRepository, DriverRepository driverRepository)
        {
            _getAddressFromLocation = getAddressFromLocation;
            _orderRepository = orderRepository;
            _driverRepository = driverRepository;
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

                await UpdateTypeMessage.ResetSessionData(chatId, cancellationToken, botClient);
                return;
            }

            if (userState == null)
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "No active state found. Please start a new order.",
                    cancellationToken: cancellationToken
                );

                await UpdateTypeMessage.ResetSessionData(chatId, cancellationToken, botClient);
                return;
            }

            var confirmationButtons = MenuMethods.YesNoAnswer();

            if (!isDriver)
            {
                // Handling address input steps
                switch (userOrder.CurrentStep)
                {
                    case "entering_origin_city":
                        userOrder.FromAddress = new AddressDTO { City = input };
                        userOrder.CurrentStep = "entering_origin_street";
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "הכנס רחוב מוצא:",
                            cancellationToken: cancellationToken
                        );
                        break;

                    case "entering_origin_street":
                        userOrder.FromAddress.Street = input;
                        userOrder.CurrentStep = "entering_origin_street_number";
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "הכנס מספר בית מוצא:",
                            cancellationToken: cancellationToken
                        );
                        break;

                    case "entering_origin_street_number":
                        if (int.TryParse(input, out int originStreetNumber))
                        {
                            userOrder.FromAddress.StreetNumber = originStreetNumber;
                            userOrder.CurrentStep = "confirm_origin_address";
                            var formattedAddress = userOrder.FromAddress.GetFormattedAddress();

                             confirmationButtons =MenuMethods.YesNoAnswer();

                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: $"כתובת המוצא שנשמרה היא: {formattedAddress}. האם זה נכון? (כן/לא)",
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
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "תשובה לא תקינה. האם הכתובת נכונה? (כן/לא):",
                                cancellationToken: cancellationToken
                            );
                        }
                        break;

                    case "entering_destination_city":
                        userOrder.ToAddress = new AddressDTO { City = input };
                        userOrder.CurrentStep = "entering_destination_street";
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "הכנס רחוב יעד:",
                            cancellationToken: cancellationToken
                        );
                        break;

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
                            userOrder.CurrentStep = "enter_passengers"; // Update this to the next appropriate step
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
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "תשובה לא תקינה. האם הכתובת נכונה? (כן/לא):",
                                cancellationToken: cancellationToken
                            );
                        }
                        break;

                    // Handle other cases...

                    default:
                        await StepIdentifier.UserLogic(chatId, input, cancellationToken, botClient, userOrder, userState, message, _getAddressFromLocation, false);
                        break;
                }

                // Update the current step in the database
                await _orderRepository.UpdateOrderStepAsync(chatId, userOrder.CurrentStep);
            }

            // Save the updated session data
            SessionManager.SetSessionData(chatId, "UserOrder", userOrder);
            SessionManager.SetSessionData(chatId, "UserState", userState);
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
