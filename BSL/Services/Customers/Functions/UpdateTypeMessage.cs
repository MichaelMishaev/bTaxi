using BL.Helpers;
using BL.Services.Customers.Handlers;
using Common.Services;
using DAL;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly SessionManager _sessionManager;
        public UpdateTypeMessage(SessionManager sessionManager)
        {
              _sessionManager = sessionManager;
        }

        public  async Task function(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, UserMenuHandle _handleUser, IServiceProvider services)
        {
            //SessionManager sessionManager = new SessionManager(redisConnectionString);
            var message = update.Message;
            var dummyOrder = services.GetRequiredService<DummyOrder>();
            OrderRepository orderRepository = new OrderRepository();
           

            if ((message?.Type == MessageType.Text) || (message?.Type == MessageType.Location))
            {
                var chatId = message.Chat.Id;
                var messageText = message?.Text;

                if (messageText == "/start")
                {
                    await _sessionManager.RemoveSessionData(chatId, "UserState");
                    await _sessionManager.RemoveSessionData(chatId, "UserOrder");
                    await _sessionManager.RemoveSessionData(chatId, "DriverUserState");
                    var mainMenuButtons = MenuMethods.mainMenuButtons();

                    await MainMenuService.DisplayMainMenu(botClient, chatId, cancellationToken);
                    //await botClient.SendTextMessageAsync(
                    //    chatId: chatId,
                    //    text: "ברוכים הבאים! אנא בחר:",
                    //    replyMarkup: mainMenuButtons,
                    //    cancellationToken: cancellationToken
                    //);
                }
                else if (messageText == "/test")
                {
                    var dummyOrderres = await dummyOrder.CreateDummyOrderAsync(chatId, botClient, cancellationToken);
                    await _sessionManager.SetSessionData(chatId, "UserOrder", dummyOrderres); // Save session data
                }
                else if (messageText == "/test2")
                {
                    await _sessionManager.RemoveSessionData(chatId, "UserState");
                    await _sessionManager.RemoveSessionData(chatId, "UserOrder");
                    await _sessionManager.RemoveSessionData(chatId, "DriverUserState");
                    await dummyOrder.AddDummyAddressesAsync(chatId, botClient, cancellationToken);
                    await _sessionManager.SetSessionData(chatId, "UserOrder", await _sessionManager.GetSessionData<UserOrder>(chatId, "UserOrder")); // Save session data
                }
                else
                {
                    var userState = await _sessionManager.GetSessionData<string>(chatId, "UserState");
                    var userOrder = await _sessionManager.GetSessionData<UserOrder>(chatId, "UserOrder");

                    if (userState != null && userState == "awaiting_bid")
                    {
                        if (decimal.TryParse(messageText, out decimal bidAmount))
                        {
                            userOrder.BidAmount = bidAmount;

                            // Insert the customer bid into the bids table and get the bidId
                            long bidId = await orderRepository.InsertAndThenUpdateCustomerBidAsync(chatId, chatId, bidAmount);

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
                            await _sessionManager.SetSessionData(chatId, "UserOrder", userOrder); // Save session data
                            await _sessionManager.SetSessionData(chatId, "UserState", userState); // Save session data
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



        public  async Task ResetSessionData(long chatId, CancellationToken cancellationToken, ITelegramBotClient botClient)
        {
            // Reset session data
            await _sessionManager.SetSessionData<UserOrder>(chatId, "UserOrder", null); // Specify the type argument
            await _sessionManager.SetSessionData<string>(chatId, "UserState", null); // Specify the type argument
        }
    }
}
