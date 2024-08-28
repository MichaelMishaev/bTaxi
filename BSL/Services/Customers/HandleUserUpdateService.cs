using BL.Helpers;
using BL.Helpers.FareCalculate;
using BL.Helpers.logger;
using BL.Menus;
using BL.Services.Customers.Functions;
using BL.Services.Customers.Handlers;
using Common.DTO;
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
using Telegram.Bot.Types.ReplyMarkups;
using telegramB.Menus;
using telegramB.Objects;
using telegramB.Services;

namespace telegramB
{
    public class HandleUserUpdateService
    {
        //*************************
        private UserOrder _userOrder;
        private ConfirmOrder CO = new ConfirmOrder();
        private UserMenuHandle _handleUser;
        OrderRepository orderRepository = new OrderRepository();
        UserRepository userRepository = new UserRepository();
        DriverRepository driverRepository = new DriverRepository();
        private readonly DummyOrder dummyOrder;
        private readonly SessionManager _sessionManager;
        public HandleUserUpdateService(UserOrder userOrder, UserMenuHandle handleUser, SessionManager sessionManager)
        {

            _userOrder = userOrder;
            _handleUser = handleUser;
            _sessionManager = sessionManager;
            dummyOrder = new DummyOrder(_sessionManager);
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {

            if (update.Type == UpdateType.Message)
            {
                var message = update.Message;
                Console.WriteLine($"incoming message: {message?.Text}");
                if ((message?.Type == MessageType.Text) || (message?.Type == MessageType.Location))
                {
                    var chatId = message.Chat.Id;
                    var messageText = message?.Text;

                    if (messageText == "/status")
                    {
                        await TypesManual.botGudenko.SendTextMessageAsync(
                           chatId: "-1002194149620",
                           text: "הכל יהיה בסדר",
                           cancellationToken: cancellationToken
                       );
                    }
                    if (messageText == "/start")
                    {
                        await _sessionManager.RemoveSessionData(chatId, "UserState");
                        await _sessionManager.RemoveSessionData(chatId, "UserOrder");
                        await _sessionManager.RemoveSessionData(chatId, "DriverUserState");

                        await MainMenuService.DisplayMainMenu(botClient, chatId, cancellationToken);
                    }
                    else if(messageText == "/help")
                    {
                        Console.WriteLine("HELPPPP");
                        await botClient.SendTextMessageAsync(
                             chatId: chatId,
                             text: "הכל יהיה בסדר",
                             cancellationToken: cancellationToken
                         );
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
                        //here gets the userOrder
                        var userState = await _sessionManager.GetSessionData<string>(chatId, "UserState");
                        var userOrder = await _sessionManager.GetSessionData<UserOrder>(chatId, "UserOrder");

                        if (userState != null && userState == "awaiting_bid" && userOrder != null)
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
                                                   $"נקודת איסוף: {userOrder.FromAddress.GetFormattedAddress()}\n" +
                                                   $"יעד: {userOrder.ToAddress.GetFormattedAddress()}\n" +
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

                        else if (userState != null && userState.StartsWith("awaiting_new_customer_bid:"))
                        {
                            await HandleNewCustomerBid(botClient, chatId, messageText, userState, cancellationToken);
                        }
                        else if (userOrder != null)
                        {
                            await _handleUser.HandleUserInput(chatId, messageText, cancellationToken, botClient, userOrder, userState, message);

                        //    _sessionManager.SetSessionData(chatId, "UserOrder", userOrder); // Save session data
                            //_sessionManager.SetSessionData(chatId, "UserState", userState); // Save session data
                        }

                        //else if (userState !=null)
                        //{
                        //    StepIdentifier.UserLogic(chatId,input,cancellationToken,botClient,userOrder,userState,message,new _getAddressFromLocation())
                        //}
                        else
                        {
                            ConsolePrintService.simpleConsoleMessage($"sent unknown message: {messageText}, User: {chatId} start from scratch");
                            await _sessionManager.RemoveSessionData(chatId, "DriverUserState"); // Clear session data for driver
                            await _sessionManager.RemoveSessionData(chatId, "UserOrder");
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


            else if (update.Type == UpdateType.CallbackQuery)
            {
                Console.WriteLine("incoming callback");
                CustomerHandler customerHandler = new CustomerHandler(_sessionManager);
                await customerHandler.CallbackHandler(botClient, update, cancellationToken, _handleUser);
            }

       
        }
        private async Task HandleNewCustomerBid(ITelegramBotClient botClient, long chatId, string messageText, string userState, CancellationToken cancellationToken)
        {
            // Extract the parentId from the userState
            long parentId = long.Parse(userState.Split(':')[1]);

            // Try to parse the bid amount
            if (decimal.TryParse(messageText, out decimal bidAmount))
            {
                // Insert the new customer bid into the bids table
                long bidId = await orderRepository.InsertAndThenUpdateCustomerBidAsync(chatId, chatId, bidAmount);

                await orderRepository.UpdateBidParentIdAsync(bidId, parentId);

                // Retrieve or create the UserOrder object
                var userOrder = await _sessionManager.GetSessionData<UserOrder>(chatId, "UserOrder") ?? new UserOrder();
                //userOrder.BidId = bidId;
                //userOrder.BidAmount = bidAmount;

                //Get the order by bidId cos BidId transfered as contextual param
                if (userOrder.BidId > 0 && userOrder.FromAddress == null)
                {
                    userOrder = await orderRepository.GetOrderByBidIdAsync(bidId);
                }
                userOrder.BidAmount = bidAmount;
                userOrder.BidId = bidId;


                // Update the order step
                //TODO/: PROBMEM: updates all the orders when customer input counter bid SUM
          //      await orderRepository.UpdateOrderStepAsync(chatId, "awaiting_confirmation");

                // Create an order summary
                var orderSummary = $"סיכום ההזמנה שלך:\n" +
                                   $"נקודת איסוף: {userOrder.FromAddress?.GetFormattedAddress()}\n" +
                                   $"יעד: {userOrder.ToAddress?.GetFormattedAddress()}\n" +
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

                // Update userState to awaiting confirmation
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


    }
}
