using BL.Helpers;
using Common.DTO;
using Common.Services;
using DAL;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using telegramB;
using telegramB.Menus;
using telegramB.Objects;
using telegramB.Services;

namespace BL.Services.Drivers.Functionalities
{
    public class HandleDriverInput
    {
        OrderRepository orderRepository = new OrderRepository();
        NotifyCustomer notifyCustomer = new NotifyCustomer(); // Rename to follow naming conventions

        public async Task HandleUserInput(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken)
        {
            // Retrieve the state from the session storage
            var driverState = SessionManager.GetSessionData<string>(chatId, "DriverUserState"); //************ Comments

            // Check if the state is null or empty
            if (string.IsNullOrEmpty(driverState))
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "No active state found. Please start a new order or registration process.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            // Extract state and extraData from driverState
            string state = driverState.Split(':')[0]; //************ Comments
            string extraData = driverState.Split(':').Length > 1 ? driverState.Split(':')[1] : null; //************ Comments

            switch (state)
            {
                case "awaiting_name":
                    var driverRegistration = SessionManager.GetSessionData<DriverDTO>(chatId, "DriverRegistration"); //************ Comments
                    driverRegistration.FullName = messageText;
                    SessionManager.SetSessionData(chatId, "DriverRegistration", driverRegistration); //************ Comments

                    await BotDriversResponseService.SendTextMessageAsync(botClient, chatId, "דגם וצבע הרכב? (כדי שהלקוח יזהה אותך, כן?)", cancellationToken);
                    driverState = "awaiting_car_details"; //************ Comments
                    break;

                case "awaiting_car_details":
                    driverRegistration = SessionManager.GetSessionData<DriverDTO>(chatId, "DriverRegistration"); //************ Comments
                    driverRegistration.CarDetails = messageText;
                    SessionManager.SetSessionData(chatId, "DriverRegistration", driverRegistration); //************ Comments

                    await BotDriversResponseService.SendTextMessageAsync(botClient, chatId, "מה מספר הטלפון שלך?", cancellationToken);
                    driverState = "awaiting_phone_number"; //************ Comments
                    break;

                case "awaiting_phone_number":
                    driverRegistration = SessionManager.GetSessionData<DriverDTO>(chatId, "DriverRegistration"); //************ Comments
                    driverRegistration.PhoneNumber = messageText;
                    SessionManager.SetSessionData(chatId, "DriverRegistration", driverRegistration); //************ Comments

                    var isValidPhoneCopy = await Validators.PhoneValidator(messageText);
                    if (!isValidPhoneCopy)
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "אנא הזן מספר טלפון תקין.",
                            cancellationToken: cancellationToken
                        );
                    }
                    else
                    {
                        await SendRegistrationSummary(botClient, chatId, cancellationToken);
                    }
                    break;

                case "awaiting_eta":
                    if (int.TryParse(messageText, out int etaMinutes) && int.TryParse(extraData, out int orderId))
                    {
                        // Clear session data after processing
                        SessionManager.RemoveSessionData(chatId, "DriverUserState"); //************ Comments

                        bool isAssigned = await orderRepository.AssignOrderToDriverAsync(orderId, chatId);

                        if (isAssigned)
                        {
                            await notifyCustomer.NotifyCustomerAboutETA(orderId, etaMinutes, TypesManual.botClient, cancellationToken); // Use NotifyCustomer class

                            await BotDriversResponseService.SendTextMessageAsync(
                                botClient,
                                chatId,
                                $"הלקוח עודכן ומצפה לך בעוד {etaMinutes} דקות.\n *שים לב:* לכל עדכון יש ליצור קשר ישירות עם הלקוח.",
                                cancellationToken,
                                ParseMode.MarkdownV2
                            );
                        }
                        else
                        {
                            await BotDriversResponseService.SendTextMessageAsync(botClient, chatId, "התרחשה שגיאה במהלך שמירת ההזמנה.", cancellationToken);
                        }
                    }
                    else
                    {
                        await BotDriversResponseService.SendTextMessageAsync(botClient, chatId, "אנא הזן מספר תקין לזמן הגעה (בדקות).", cancellationToken: cancellationToken);
                    }
                    break;

                // Adjusted section in HandleDriverUpdateAsync method
                case "awaiting_driver_bid":
                    {
                        // Avoid variable conflict by renaming
                        if (decimal.TryParse(messageText, out decimal driverBidAmount))
                        {
                            var stateData = driverState.Split(':'); //************ Comments
                            var parentId = long.Parse(stateData[1]); //************ Comments
                            var driverId = chatId;

                            // Retrieve the customer ID from the parent bid
                            var customerChatId = await orderRepository.GetCustomerChatIdByBidChatIdAsync(parentId);

                            if (customerChatId.HasValue)
                            {
                                // Insert the driver's bid into the database
                                long bidId = await orderRepository.InsertBidAsync(parentId, driverId, driverId, customerChatId.Value, driverBidAmount, true);

                                var bidOptions = MenuMethods.AwaitCustomerBidResponse(parentId, driverBidAmount);

                                await TypesManual.botClient.SendTextMessageAsync(
                                    chatId: customerChatId.Value,
                                    text: $"נהג הציע מחיר חדש לנסיעה. הצעת מחיר: {driverBidAmount:F2} ₪",
                                    replyMarkup: bidOptions,
                                    cancellationToken: cancellationToken
                                );

                                // Update the CurrentStep in the database for the customer order
                                await orderRepository.UpdateOrderStepAsync(customerChatId.Value, "awaiting_bid");

                                // Update the userState and userOrder
                                var customerOrder = SessionManager.GetSessionData<UserOrder>(customerChatId.Value, "UserOrder");
                                if (customerOrder != null)
                                {
                                    customerOrder.CurrentStep = "awaiting_bid";
                                    var userState = $"awaiting_bid:{parentId}"; //************ Comments

                                    SessionManager.SetSessionData(customerChatId.Value, "UserState", userState); // Save session data
                                    SessionManager.SetSessionData(customerChatId.Value, "UserOrder", customerOrder); // Save session data
                                }
                            }

                            // Clear driver's state after setting the customer state
                            SessionManager.RemoveSessionData(chatId, "DriverUserState"); // Clear session data for driver //************ Comments
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
                    }

                case "awaiting_bid": //************ Comments
                    if (decimal.TryParse(messageText, out decimal newDriverBid)) // Renamed to newDriverBid
                    {
                        var parentId = long.Parse(extraData);
                        var driverId = chatId; // Assuming chatId is the driverId

                        // Retrieve the customer ID from the parent bid
                        var customerChatId = await orderRepository.GetCustomerChatIdByBidChatIdAsync(chatId);

                        if (customerChatId.HasValue)
                        {
                            // Insert the driver's bid into the database
                            await orderRepository.InsertBidAsync(parentId, chatId, driverId, customerChatId.Value, newDriverBid, true); // true because it's a driver bid

                            var bidOptions = MenuMethods.AwaitCustomerBidResponse(parentId, newDriverBid);

                            await TypesManual.botClient.SendTextMessageAsync(
                                chatId: customerChatId.Value,
                                text: $"נהג הציע מחיר חדש לנסיעה. הצעת מחיר: {newDriverBid:F2} ₪",
                                replyMarkup: bidOptions,
                                cancellationToken: cancellationToken
                            );

                            // Update the CurrentStep in the database for the customer order
                            await orderRepository.UpdateOrderStepAsync(customerChatId.Value, "awaiting_bid");

                            // Update the userStates dictionary for customer
                            var newUserState = $"awaiting_bid:{parentId}"; // Update state to awaiting customer bid
                            SessionManager.SetSessionData(customerChatId.Value, "UserState", newUserState); // Save session data
                        }

                        // Clear driver's state after setting the customer state
                        SessionManager.RemoveSessionData(chatId, "DriverUserState"); // Clear session data for driver //************ Comments
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "הצעת מחיר לא תקינה. אנא הזן מספר תקין:",
                            cancellationToken: cancellationToken
                        );
                    }
                    break; //************ Comments
            }

            // Save the updated state in the session storage
            SessionManager.SetSessionData(chatId, "DriverUserState", driverState); //************ Comments
        }

        private async Task SendRegistrationSummary(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var driverRegistration = SessionManager.GetSessionData<DriverDTO>(chatId, "DriverRegistration"); //************ Comments
            var summaryText = $"סיכום הרשמה:\n" +
                              $"שם מלא: {driverRegistration.FullName}\n" +
                              $"פרטי רכב: {driverRegistration.CarDetails}\n" +
                              $"מספר טלפון: {driverRegistration.PhoneNumber}\n\n" +
                              "האם כל הפרטים נכונים?";

            await BotDriversResponseService.SendRegistrationSummaryAsync(botClient, chatId, summaryText, cancellationToken);

            var driverState = "awaiting_confirmation"; //************ Comments
            SessionManager.SetSessionData(chatId, "DriverUserState", driverState); //************ Comments
        }
    }
}
