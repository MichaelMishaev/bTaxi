using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using telegramB.Helpers;
using telegramB.Menus;
using telegramB.Objects;

namespace telegramB
{
    public class HandleUpdate
    {
        private static Dictionary<long, UserOrder> userOrders = new Dictionary<long, UserOrder>();
        private UserOrder _userOrder;
        private HandleUser _handleUser;
        public HandleUpdate(UserOrder userOrder, HandleUser handleUser)
        {

            _userOrder = userOrder;
            _handleUser = handleUser;
        }
        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message)
            {
                var message = update.Message;

                if ((message?.Type == MessageType.Text) || (message?.Type == MessageType.Location))
                {
                    var chatId = message.Chat.Id;
                    var messageText = message?.Text;

                    if (messageText == "/start")
                    {
                        //****************************************************************//
                        var mainMenuButtons = MenuMethods.mainMenuButtons();

                        //****************************************************************//

                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Welcome! Choose an option:",
                            replyMarkup: mainMenuButtons,
                            cancellationToken: cancellationToken

                        );
                    }
                    else if (userOrders.ContainsKey(chatId))
                    {
                        await _handleUser.HandleUserInput(chatId, messageText, cancellationToken, botClient, userOrders, message);
                    }
                }
            }

            else if ((update.Type == UpdateType.CallbackQuery) || userOrders.Count == 0)
            {
                var callbackQuery = update.CallbackQuery;

                if (callbackQuery != null)
                {
                    var chatId = callbackQuery.Message.Chat.Id;
                    var callbackData = callbackQuery.Data;

                    if (callbackData == "order_taxi")
                    {
                        userOrders[chatId] = new UserOrder();

                        //****************************************************************//
                        var orderTaxiButtons = MenuMethods.orderTaxiButtons();
                        //****************************************************************//

                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "להזמנת הסעה, אנא הזן פרטי נסיעה:",
                            replyMarkup: orderTaxiButtons,
                            cancellationToken: cancellationToken

                        );
                    }
                    else if (callbackData == "from" || callbackData == "to" || callbackData == "passengers" || callbackData == "phone" || callbackData == "remarks")
                    {
                        if (userOrders.Count == 0)
                        {
                            var mainMenuButtons = MenuMethods.mainMenuButtons();

                            //****************************************************************//

                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Welcome! Choose an option:",
                                replyMarkup: mainMenuButtons,
                                cancellationToken: cancellationToken
                            );
                        }
                        else
                        {
                            userOrders[chatId].CurrentStep = callbackData;

                            string promptText = callbackData switch
                            {
                                "from" => "נא להזין נקודת איסוף:",
                                "to" => "נא להזין יעד:",
                                "passengers" => "יש להזין מספר נוסעים:",
                                "phone" => "מספר טלפון:",
                                "remarks" => "הערות",
                                _ => ""
                            };

                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: promptText,
                                cancellationToken: cancellationToken

                            );
                        }
                    }
                    else if (callbackData == "submit")
                    {
                        if (userOrders.Count == 0)
                        {
                            var mainMenuButtons = MenuMethods.mainMenuButtons();
                            return;
                        }
                        if ((string.IsNullOrWhiteSpace(userOrders[chatId].PhoneNumber) || userOrders[chatId].PhoneNumber == "0") ||
                            (string.IsNullOrWhiteSpace(userOrders[chatId].FromAddress)) || string.IsNullOrWhiteSpace(userOrders[chatId].ToAddress))
                        {
                            await botClient.SendTextMessageAsync(
                           chatId: chatId,
                           text: @"לא הוזן יעד\מקום איסוף\ מספר טלפון",
                           cancellationToken: cancellationToken
                       );
                            return;
                        }
                        await DisplayAndSubmitOrder.DisplayOrderSummary(chatId, botClient, userOrders[chatId], cancellationToken);
                    }
                }
            }
        }
    }
}
