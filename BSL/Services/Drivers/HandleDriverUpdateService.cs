using BL.Helpers;
using Common.DTO;
using DAL;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using telegramB.Menus;
using telegramB.Services;
using telegramB;
using Common.Services;
using telegramB.Objects;
using BL.Services.Drivers.Functionalities;

namespace BL.Services.Drivers
{
    public class HandleDriverUpdateService
    {
        UserRepository userRepository = new UserRepository();
        DriverRepository driverRepository = new DriverRepository();
        DBCommands dBCommands = new DBCommands();
        private Dictionary<string, long> _userChatIds;
        OrderRepository orderRepository = new OrderRepository();
        HandleDriverInput handleDriverInput = new HandleDriverInput();
        DriverRegistration driverRegistration = new DriverRegistration();
        ConfirmationHandler confirmationHandler = new ConfirmationHandler();

        public HandleDriverUpdateService(Dictionary<string, long> userChatIds)
        {
            _userChatIds = userChatIds; // Assign userChatIds
        }

        public async Task HandleDriverUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var chatId = update.CallbackQuery?.Message.Chat.Id ?? update.Message?.Chat.Id ?? 0;

            // Retrieve session data for driver
            var driverState = SessionManager.GetSessionData<string>(chatId, "DriverUserState"); //************ Comments

            long bidId = 0;
            if (update.CallbackQuery != null && update.CallbackQuery.Data.StartsWith("accept_order:"))
            {
                var parts = update.CallbackQuery.Data.Split(':');
                bidId = long.Parse(parts[1]);
            }
            else if (!string.IsNullOrEmpty(driverState) && driverState.Contains(":"))
            {
                bidId = long.Parse(driverState.Split(':')[1]);
            }

            long clientChatId = SessionManager.GetClientChatIdForDriver(chatId, bidId) ?? 0;
            var userOrder = SessionManager.GetSessionData<UserOrder>(clientChatId, "UserOrder");

            if (userOrder == null || string.IsNullOrEmpty(driverState))
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "No active order found. Please start a new order.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            bool isApprovedDriver = false;
            bool driverExists = false;

            if ((update.CallbackQuery == null) && (update.Message == null))
            {
                Console.WriteLine("all nulls checkpoint");
                return;
            }

            driverExists = update.CallbackQuery != null ? await driverRepository.checkIfDriverExists(update.CallbackQuery.From.Id) : await driverRepository.checkIfDriverExists(update.Message.From.Id);
            isApprovedDriver = update.CallbackQuery != null ? await driverRepository.isApprovedDriver(update.CallbackQuery.From.Id) : await driverRepository.isApprovedDriver(update.Message.From.Id);

            var buser = update.CallbackQuery != null ? update.CallbackQuery.From.Id : update.Message.From.Id;
            if (buser == 5164987026)
            {
                return;
            }

            if (update.Type == UpdateType.Message)
            {
                var message = update.Message;

                if (message?.Type == MessageType.Text)
                {
                    var messageText = message.Text;

                    if (messageText == "/start")
                    {
                        await BotDriversResponseService.SendMainMenuAsync(TypesManual.botDriver, chatId, cancellationToken);
                    }
                    else if (driverExists && !isApprovedDriver)
                    {
                        await BotDriversResponseService.SendTextMessageAsync(TypesManual.botDriver, chatId, "הבקשה שלך נשלחה לאישור, אחרי אישורה תשלח אליך הודעה. תודה.", cancellationToken);
                    }
                    else if (driverState != null)
                    {
                        await handleDriverInput.HandleUserInput(TypesManual.botDriver, chatId, messageText, cancellationToken);
                    }
                    else if (driverExists && isApprovedDriver)
                    {
                        await BotDriversResponseService.SendStartOrdersMenuAsync(TypesManual.botDriver, chatId, cancellationToken);
                    }
                    else
                    {
                        await BotDriversResponseService.SendIntroMenuAsync(TypesManual.botDriver, chatId, cancellationToken);
                    }
                }
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                var callbackQuery = update.CallbackQuery;
                var messageId = callbackQuery.Message.MessageId;

                if (driverExists && !isApprovedDriver)
                {
                    await BotDriversResponseService.SendTextMessageAsync(TypesManual.botDriver, chatId, "המשתמש עדיין לא אושר", cancellationToken);
                }
                else if (callbackQuery.Data == "start_orders")
                {
                    await driverRepository.SetDriverRecieveJobs(callbackQuery.From.Id);
                    if (isApprovedDriver)
                    {
                        await BotDriversResponseService.SendTextMessageAsync(TypesManual.botDriver, chatId, "הינך פתוח לקבלת הזמנות, אנא המתן.....", cancellationToken);
                        await BotDriversResponseService.SendStopReceivingOrdersMenuAsync(TypesManual.botDriver, chatId, cancellationToken);
                    }
                    else
                    {
                        await BotDriversResponseService.SendTextMessageAsync(TypesManual.botDriver, chatId, "המשתמש עדיין לא אושר", cancellationToken);
                    }
                }
                else if (callbackQuery.Data == "no_orders")
                {
                    await driverRepository.SetDriverDeclineJobs(callbackQuery.From.Id);
                    await BotDriversResponseService.SendTextMessageAsync(TypesManual.botDriver, chatId, "אינך מקבל הזמנות כרגע.", cancellationToken);
                    await BotDriversResponseService.SendTextMessageAsync(TypesManual.botDriver, chatId, "ברגע שיהיה מצב רוח טוב להרוויח קצת, תלחצו על כן!!", cancellationToken);
                    await BotDriversResponseService.SendStartOrdersMenuAsync(TypesManual.botDriver, chatId, cancellationToken);
                }
                else if (callbackQuery.Data == "continue_orders")
                {
                    await BotDriversResponseService.SendTextMessageAsync(TypesManual.botDriver, chatId, "הינך ממשיך לקבל הזמנות.", cancellationToken);
                    await BotDriversResponseService.SendStopReceivingOrdersMenuAsync(TypesManual.botDriver, chatId, cancellationToken);
                }
                else if (callbackQuery.Data == "confirm_no")
                {
                    if (driverExists)
                    {
                        await BotDriversResponseService.SendTextMessageAsync(TypesManual.botDriver, chatId, "משתמש זה כבר קיים במערכת", cancellationToken);
                        await BotDriversResponseService.SendStartOrdersMenuAsync(TypesManual.botDriver, chatId, cancellationToken);
                        return;
                    }
                    await BotDriversResponseService.SendTextMessageAsync(TypesManual.botDriver, chatId, "אין אימות פרטים נתחיל מחדש? ", cancellationToken);
                    await BotDriversResponseService.SendRegistrationMenuAsync(TypesManual.botDriver, chatId, cancellationToken);
                }
                else if (callbackQuery.Data == "start_registration")
                {
                    if (driverExists)
                    {
                        await BotDriversResponseService.SendTextMessageAsync(TypesManual.botDriver, chatId, "משתמש זה כבר קיים במערכת", cancellationToken);
                        await BotDriversResponseService.SendStartOrdersMenuAsync(TypesManual.botDriver, chatId, cancellationToken);
                        return;
                    }
                    await driverRegistration.StartRegistration(TypesManual.botDriver, chatId, messageId, cancellationToken);
                }
                else if (callbackQuery.Data == "registered_driver")
                {
                    if (driverExists && isApprovedDriver)
                    {
                        await BotDriversResponseService.SendStartOrdersMenuAsync(TypesManual.botDriver, chatId, cancellationToken);
                    }
                    else
                    {
                        await BotDriversResponseService.SendTextMessageAsync(TypesManual.botDriver, chatId, "אינך רשום כנהג bDrive", cancellationToken);
                        await BotDriversResponseService.SendMainMenuAsync(TypesManual.botDriver, chatId, cancellationToken);
                    }
                }
                else if (driverState != null && driverState == "awaiting_confirmation") //************ Comments
                {
                    await confirmationHandler.HandleConfirmation(TypesManual.botDriver, chatId, callbackQuery, cancellationToken);
                }
                else if (callbackQuery.Data.StartsWith("accept_bid:"))
                {
                    var data = callbackQuery.Data.Split(':');
                    var bidChatId = long.Parse(data[1]);
                    var driverBid = decimal.Parse(data[2]);

                    await orderRepository.MarkBidAsTakenAsync(bidChatId);

                    var bidDetails = await orderRepository.GetBidDetailsAsync(bidChatId);
                    await botClient.SendTextMessageAsync(
                        chatId: bidDetails.DriverId,
                        text: $"הצעת המחיר שלך בסך {driverBid:F2} ₪ התקבלה! פרטי הלקוח:\n" +
                              $"טלפון: {bidDetails.CustomerPhoneNumber}",
                        cancellationToken: cancellationToken
                    );

                    var orderId = await orderRepository.CreateOrderAsync(bidDetails);

                    await TypesManual.botClient.SendTextMessageAsync(
                        chatId: bidDetails.CustomerId,
                        text: "הזמנתך אושרה! הנהג בדרכו אליך.",
                        cancellationToken: cancellationToken
                    );
                }
                else if (callbackQuery.Data.StartsWith("new_bid:"))
                {
                    var bidChatId = long.Parse(callbackQuery.Data.Split(':')[1]);

                    await botClient.SendTextMessageAsync(
                        chatId: bidChatId,
                        text: "אנא הזן את המחיר החדש שאתה מציע עבור הנסיעה:",
                        cancellationToken: cancellationToken
                    );

                    var userState = $"awaiting_bid:{bidChatId}";  //************ Comments
                    userOrder.CurrentStep = "awaiting_bid"; //************ Comments
                    SessionManager.SetSessionData(bidChatId, "UserState", userState); //************ Comments
                    SessionManager.SetSessionData(bidChatId, "UserOrder", userOrder); //************ Comments
                }
                else if (callbackQuery.Data.StartsWith("accept_order:"))
                {
                    var orderId = int.Parse(callbackQuery.Data.Split(':')[1]);
                    var driverId = callbackQuery.From.Id;

                    bool isAssigned = await orderRepository.CheckOrderAssignedAsync(orderId);

                    if (!isAssigned)
                    {
                        await orderRepository.AssignOrderToDriverAsync(orderId, driverId);

                        await botClient.SendTextMessageAsync(
                            chatId: callbackQuery.Message.Chat.Id,
                            text: "הזמנה התקבלה בהצלחה!",
                            cancellationToken: cancellationToken
                        );

                        var customerId = await orderRepository.GetCustomerIdByOrderId(orderId);
                        await botClient.SendTextMessageAsync(
                            chatId: customerId,
                            text: "נהג התקבל את הזמנתך. פרטי הנהג יישלחו אליך בקרוב.",
                            cancellationToken: cancellationToken
                        );
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: callbackQuery.Message.Chat.Id,
                            text: "מצטערים, ההזמנה כבר נלקחה על ידי נהג אחר.",
                            cancellationToken: cancellationToken
                        );
                    }
                }
                else if (callbackQuery.Data.StartsWith("bid_order:"))
                {
                    chatId = callbackQuery.Message.Chat.Id;
                    var driverId = callbackQuery.From.Id;
                    bidId = long.Parse(callbackQuery.Data.Split(':')[1]);

                    // Store the mapping between driver ID and client chat ID
                    SessionManager.SetDriverToClientMapping(driverId, chatId, bidId);

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "אנא הזן את המחיר שאתה מציע עבור הנסיעה:",
                        cancellationToken: cancellationToken
                    );

                    var localDriverState = $"awaiting_driver_bid:{bidId}"; //************ Comments
                    SessionManager.SetSessionData(chatId, "DriverUserState", localDriverState); //************ Comments
                }
                else if (driverState != null && driverState.StartsWith("awaiting_driver_bid:")) //************ Comments
                {
                    var driverId = long.Parse(driverState.Split(':')[1]); //************ Comments

                    if (decimal.TryParse(callbackQuery.Message.Text, out decimal driverBid))
                    {
                        await orderRepository.InsertDriverBidAsync(chatId, driverId, driverBid);

                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "הצעת המחיר שלך נשמרה בהצלחה.",
                            cancellationToken: cancellationToken
                        );

                        var customerChatId = await orderRepository.GetCustomerChatIdByBidChatIdAsync(chatId);
                        if (customerChatId.HasValue)
                        {
                            await TypesManual.botClient.SendTextMessageAsync(
                                chatId: customerChatId.Value,
                                text: $"נהג הציע מחיר חדש לנסיעה. הצעת מחיר: {driverBid:F2} ₪",
                                cancellationToken: cancellationToken
                            );

                            var userState = $"awaiting_bid:{chatId}"; //************ Comments
                            SessionManager.SetSessionData(customerChatId.Value, "UserState", userState); //************ Comments
                        }

                        // Clear session data for the driver
                        SessionManager.RemoveSessionData(chatId, "DriverUserState"); //************ Comments
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
                else if (callbackQuery.Data.StartsWith("take_order:"))
                {
                    var orderId = int.Parse(callbackQuery.Data.Split(':')[1]);
                    var driverId = callbackQuery.From.Id;

                    bool isAssigned = await orderRepository.CheckOrderAssignedAsync(orderId);

                    if (!isAssigned)
                    {
                        var localDriverState = $"awaiting_eta:{orderId}"; //************ Comments
                        SessionManager.SetSessionData(chatId, "DriverUserState", localDriverState); //************ Comments

                        var cancelOption = MenuMethods.cancelOrder(orderId);

                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "אם ברצונך לבטל 🫣 ",
                            replyMarkup: cancelOption,
                            cancellationToken: cancellationToken
                        );

                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "שימו ❤️, עד שלא שלחתם זמן הגעה, נהג אחר עלול לקחת את הנסיעה.",
                            cancellationToken: cancellationToken
                        );

                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "כמה זמן ייקח לך להגיע? ⌛️(בזמן בדקות)",
                            cancellationToken: cancellationToken
                        );
                    }
                    else
                    {
                        await BotDriversResponseService.SendTextMessageAsync(botClient, chatId, "מצטערים, הזמנה זו כבר נלקחה על ידי נהג אחר.", cancellationToken);
                    }
                }
                else if (callbackQuery.Data.StartsWith("finish_ride:"))
                {
                    var orderId = int.Parse(callbackQuery.Data.Split(':')[1]);

                    await orderRepository.CloseOrderAsync(orderId);

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "הנסיעה סומנה כהושלמה. תודה שנסעת איתנו!",
                        cancellationToken: cancellationToken
                    );

                    if (driverState != null && driverState.StartsWith("awaiting_finish:")) //************ Comments
                    {
                        var stateData = driverState.Split(':'); //************ Comments
                        if (stateData.Length == 3)
                        {
                            var localMessageId = int.Parse(stateData[2]);
                            await botClient.DeleteMessageAsync(chatId, localMessageId, cancellationToken);
                        }
                    }

                    // Clear session data after finishing the ride
                    SessionManager.RemoveSessionData(chatId, "DriverUserState"); //************ Comments
                }
                else if (callbackQuery.Data.StartsWith("cancel_order:"))
                {
                    var orderId = int.Parse(callbackQuery.Data.Split(':')[1]);
                    // Clear session data for canceling the order
                    SessionManager.RemoveSessionData(chatId, "DriverUserState"); //************ Comments
                    await BotDriversResponseService.SendTextMessageAsync(botClient, chatId, "ההזמנה בוטלה.", cancellationToken);
                }
                else if (callbackQuery.Data == "view_orders")
                {
                    var availableOrders = await orderRepository.GetAvailableOrdersAsync();
                    if (availableOrders.Count == 0)
                    {
                        await BotDriversResponseService.SendTextMessageAsync(botClient, chatId, "אין הזמנות זמינות כרגע.", cancellationToken);
                    }
                    else
                    {
                        foreach (var order in availableOrders)
                        {
                            string orderDetails = $"Order ID: {order.Id}\nFrom: {order.FromAddress}\nTo: {order.ToAddress}\nPrice: {order.price}\nRemarks: {order.Remarks}";
                            await BotDriversResponseService.SendTextMessageAsync(botClient, chatId, orderDetails, cancellationToken);
                            var orderActionsMenu = MenuMethods.GetOrderActionsMenu(order.Id);
                            await botClient.SendTextMessageAsync(chatId, "בחר פעולה:", replyMarkup: orderActionsMenu, cancellationToken: cancellationToken);
                        }
                    }
                }
                else
                {
                    await dBCommands.SaveDriverAsUserIfNotExists(callbackQuery);
                    if (driverExists && isApprovedDriver)
                    {
                        await BotDriversResponseService.SendStartOrdersMenuAsync(TypesManual.botDriver, chatId, cancellationToken);
                    }

                    try
                    {
                        await TypesManual.botDriver.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
                    }
                    catch (ApiRequestException apiEx) when (apiEx.ErrorCode == 400 && apiEx.Message.Contains("query is too old"))
                    {
                        Console.WriteLine($"Callback query expired: {apiEx.Message}");
                        await BotDriversResponseService.SendTextMessageAsync(TypesManual.botDriver, chatId, "The request has expired. Please try again.", cancellationToken);
                    }
                }
            }

            // Save session data before the method ends
            SessionManager.SetSessionData(chatId, "UserOrder", userOrder);
            SessionManager.SetSessionData(chatId, "DriverUserState", driverState); //************ Comments
        }
    }
}
