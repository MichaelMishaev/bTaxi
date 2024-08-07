using BL.Helpers;
using BL.Services.Customers.Handlers;
using Common.Services;
using DAL;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using telegramB.Menus;
using telegramB.Objects;

namespace BL.Services.Customers.Functions
{
    public class UpdateTypeMessage
    {

        public static async Task function(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, UserMenuHandle _handleUser)
        {
            var message = update.Message;
            DummyOrder dummyOrder = new DummyOrder();
            OrderRepository orderRepository = new OrderRepository();
           

            if ((message?.Type == MessageType.Text) || (message?.Type == MessageType.Location))
            {
                var chatId = message.Chat.Id;
                var messageText = message?.Text;

                if (messageText == "/start")
                {
                    var mainMenuButtons = MenuMethods.mainMenuButtons();

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Welcome! Choose an option:",
                        replyMarkup: mainMenuButtons,
                        cancellationToken: cancellationToken
                    );
                }
                else if (messageText == "/test")
                {
                    var dummyOrderres = await dummyOrder.CreateDummyOrderAsync(chatId, botClient, cancellationToken);
                    SessionManager.SetSessionData(chatId, "UserOrder", dummyOrderres); // Save session data
                }
                else if (messageText == "/test2")
                {
                    await dummyOrder.AddDummyAddressesAsync(chatId, botClient, cancellationToken);
                    SessionManager.SetSessionData(chatId, "UserOrder", SessionManager.GetSessionData<UserOrder>(chatId, "UserOrder")); // Save session data
                }
                else
                {
                    var userState = SessionManager.GetSessionData<string>(chatId, "UserState");
                    var userOrder = SessionManager.GetSessionData<UserOrder>(chatId, "UserOrder");

                    if (userState != null && userState == "awaiting_bid")
                    {
                        if (decimal.TryParse(messageText, out decimal bidAmount))
                        {
                            userOrder.BidAmount = bidAmount;

                            // Insert the customer bid into the bids table and get the bidId
                            long bidId = await orderRepository.InsertCustomerBidAsync(chatId, chatId, bidAmount);

                            // Update the bidId in the userOrder
                            userOrder.BidId = bidId;

                            // Set the current step to "awaiting_confirmation" for the order
                            await orderRepository.UpdateOrderStepAsync(chatId, "awaiting_confirmation");

                            var orderSummary = $"סיכום ההזמנה שלך:\n" +
                                               $"נקודת איסוף: {userOrder.FromAddress}\n" +
                                               $"יעד: {userOrder.ToAddress}\n" +
                                               $"מחיר מוצע: {userOrder.BidAmount:F2} ₪\n" +
                                               $"מספר טלפון: {userOrder.PhoneNumber}\n" +
                                               $"הערות: {userOrder.Remarks}\n";

                            var confirmationButtons = MenuMethods.OrderConfirmationButtons();

                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: orderSummary,
                                replyMarkup: confirmationButtons,
                                cancellationToken: cancellationToken
                            );

                            userState = "awaiting_confirmation";
                            SessionManager.SetSessionData(chatId, "UserOrder", userOrder); // Save session data
                            SessionManager.SetSessionData(chatId, "UserState", userState); // Save session data
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
                    else if (userOrder != null)
                    {
                        await  _handleUser.HandleUserInput(chatId, messageText, cancellationToken, botClient, userOrder, userState, message);

                        SessionManager.SetSessionData(chatId, "UserOrder", userOrder); // Save session data
                        SessionManager.SetSessionData(chatId, "UserState", userState); // Save session data
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "פקודה לא קיימת, התחל מחדש",
                            replyMarkup: MenuMethods.mainMenuButtons(),
                            cancellationToken: cancellationToken
                        );
                    }
                }
            }
        }



        public static async Task ResetSessionData(long chatId, CancellationToken cancellationToken, ITelegramBotClient botClient)
        {
            // Reset session data
            SessionManager.SetSessionData<UserOrder>(chatId, "UserOrder", null); // Specify the type argument
            SessionManager.SetSessionData<string>(chatId, "UserState", null); // Specify the type argument


            // Notify the user to start a new order
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "No active state found. Please start a new order.",
                cancellationToken: cancellationToken
            );

            // Provide instructions to start a new order
            var startOrderButton = MenuMethods.mainMenuButtons();

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "To start a new order, please click the button below.",
                replyMarkup: startOrderButton,
                cancellationToken: cancellationToken
            );
        }
    }
}
