using BL.Helpers;
using BL.Services.Drivers.StaticFiles;
using Common.DTO;
using Common.Services;
using DAL;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
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
        NotifyCustomer notifyCustomer; //= new NotifyCustomer(); 
        private readonly SessionManager _sessionManager;
        public HandleDriverInput(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
            notifyCustomer = new NotifyCustomer(_sessionManager);
        }
        public async Task HandleUserInput(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken, Update update)
        {
            // Retrieve the state from the session storage
            var driverState =  await _sessionManager.GetSessionData<string>(chatId, "DriverUserState"); 

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
            string extraData = driverState.Split(':').Length > 1 ? driverState.Split(':')[1] : null; 

            switch (state)
            {
                case keywords.AwaitingNameState:
                    var driverRegistration = await _sessionManager.GetSessionData<DriverDTO>(chatId, "DriverRegistration"); // Retrieve the current driver registration data from the session
                    if(driverRegistration == null) driverRegistration = new DriverDTO();
                    driverRegistration.FullName = messageText; // Set the full name from the user's input
                    await _sessionManager.SetSessionData(chatId, "DriverRegistration", driverRegistration); // Save the updated registration data back to the session

                    // Set the driver state to the next step in the registration process
                    driverState = keywords.AwaitingCarDetailsState;
                    await _sessionManager.SetSessionData(chatId, "DriverUserState", driverState); // Save the updated state to the session

                    // Prompt the driver for the next piece of information
                    await BotDriversResponseService.SendTextMessageAsync(botClient, chatId, "דגם וצבע הרכב? (כדי שהלקוח יזהה אותך, כן?)", cancellationToken);
                    break;

                case keywords.AwaitingCarDetailsState:
                    driverRegistration = await _sessionManager.GetSessionData<DriverDTO>(chatId, "DriverRegistration"); //************ Comments
                    driverRegistration.CarDetails = messageText;
                    await _sessionManager.SetSessionData(chatId, "DriverRegistration", driverRegistration); //************ Comments

                    await BotDriversResponseService.SendTextMessageAsync(botClient, chatId, "יש להזין מספר טלפון: ", cancellationToken);
                    driverState = "awaiting_phone_number"; //************ Comments
                    break;

                case "awaiting_phone_number":
                    driverRegistration = await _sessionManager.GetSessionData<DriverDTO>(chatId, "DriverRegistration"); //************ Comments
                    driverRegistration.PhoneNumber = messageText;
                    await _sessionManager.SetSessionData(chatId, "DriverRegistration", driverRegistration); //************ Comments

                    var isValidPhoneCopy = await Validators.PhoneValidator(messageText);
                    if (!isValidPhoneCopy)
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "אנא הזן מספר טלפון תקין:",
                            cancellationToken: cancellationToken
                        );
                    }
                    else
                    {
                        driverState = "awaiting_confirmation";
                        await SendRegistrationSummary(botClient, chatId, cancellationToken);
                    }
                    break;

                case "awaiting_eta":
                    if (int.TryParse(messageText, out int etaMinutes) && int.TryParse(extraData, out int orderId))
                    {
                        // Clear session data after processing
                        await _sessionManager.RemoveSessionData(chatId, "DriverUserState"); //************ Comments

                        bool isOrderTakken = await orderRepository.CheckOrderAssignedAsync(orderId);

                        if (isOrderTakken)
                        {
                            await BotDriversResponseService.SendTextMessageAsync(
                                botClient,
                                chatId,
                                "מצטערים, ההזמנה כבר נלקחה על ידי נהג אחר",
                                cancellationToken,
                                ParseMode.MarkdownV2
                            );
                            break;
                        }

                        bool isAssigned = await orderRepository.AssignOrderToDriverAsync(orderId, chatId);

                        if (isAssigned)
                        {
                            await notifyCustomer.NotifyCustomerAboutETA(orderId, etaMinutes, TypesManual.botClient, cancellationToken); // Use NotifyCustomer class

                            await BotDriversResponseService.SendTextMessageAsync(
                                botClient,
                                chatId,
                                $"הלקוח עודכן  📥 ומצפה לך בעוד {etaMinutes} דקות 🕑 \n *שים לב:* לכל עדכון יש ליצור קשר ישירות עם הלקוח ‼️",
                                cancellationToken,
                                ParseMode.MarkdownV2
                            );
                        }
                        else
                        {
                            await BotDriversResponseService.SendTextMessageAsync(botClient, chatId, "התרחשה שגיאה במהלך שמירת ההזמנה", cancellationToken);
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
                        if (decimal.TryParse(messageText, out decimal driverBidAmount))
                        {
                            var stateData = driverState.Split(':');
                            var orderIdFromState = long.Parse(stateData[1]);
                            var driverId = chatId;

                            // Retrieve the customer ID from the parent bid
                            var result = await orderRepository.GetCustomerChatIdAndBidIdByOrderIdAsync(orderIdFromState);

                            var customerChatId = result.UserId;
                            var parentBidID = result.BidId;

                            if (customerChatId.HasValue)
                            {
                                // Insert the driver's bid into the database
                                long bidId = await orderRepository.InsertBidAsync(parentBidID, driverId, driverId, customerChatId.Value, driverBidAmount, true);

                                //Save  in the same session the driverId and the BidId, the bidId used when customer 
                                //whats to send a new bid AND RETURN IT TO THE SAME DRIVER
                                string sessionKey = $"{orderIdFromState}:driver:{driverId}";
                                await _sessionManager.SetSessionData(sessionKey, "DriverChatId", driverId);
                                await _sessionManager.SetSessionData(sessionKey, "BidId", bidId);



                                bool isAssigned = await orderRepository.CheckOrderAssignedAsync((int)orderIdFromState);

                                if (isAssigned)
                                {
                                        await   botClient.SendTextMessageAsync(
                                                chatId: driverId,//customerChatId.Value,
                                                text: "מצטערים, ההזמנה כבר נלקחה על ידי נהג אחר.",
                                                cancellationToken: cancellationToken
                                           );
                                    break;
                                }



                                // Generate bid options with contextual parameters
                                //string driverIdString = driverId.ToString();
                                var bidOptions = MenuMethods.AwaitDriverCustomerBidResponse(orderIdFromState, driverBidAmount, bidId);


                                string maskedDriver = $"{driverId % 1000}******";

                                await TypesManual.botClient.SendTextMessageAsync(
                                    chatId: customerChatId.Value,
                                    text: $"נהג {maskedDriver} הציע מחיר חדש לנסיעה. הצעת מחיר: {driverBidAmount:F2} ₪",
                                    replyMarkup: bidOptions,
                                    cancellationToken: cancellationToken
                                );

                                bool res= await Validators.DeleteMessage(TypesManual.botClient, chatId, update.Message.MessageId, cancellationToken);

                                await botClient.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: "הודעה נשלחה ללקוח📲, אנא המתן.........",
                                    cancellationToken: cancellationToken
                                );

                                // Update the CurrentStep in the database for the customer order
                                //await orderRepository.UpdateOrderStepAsync(customerChatId.Value, "awaiting_bid");

                                // Update the userState and userOrder
                                var customerOrder = await _sessionManager.GetSessionData<UserOrder>(customerChatId.Value, "UserOrder");
                                if (customerOrder != null)
                                {
                                    customerOrder.CurrentStep = "awaiting_bid";
                                    var userState = $"awaiting_bid:{orderIdFromState}";

                                    await _sessionManager.SetSessionData(customerChatId.Value, "UserState", userState);
                                    await _sessionManager.SetSessionData(customerChatId.Value, "UserOrder", customerOrder);
                                }
                            }

                            // Clear driver's state after setting the customer state
                            await _sessionManager.RemoveSessionData(chatId, "DriverUserState");
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
                            await _sessionManager.SetSessionData(customerChatId.Value, "UserState", newUserState); // Save session data
                        }

                        // Clear driver's state after setting the customer state
                        await _sessionManager.RemoveSessionData(chatId, "DriverUserState"); // Clear session data for driver //************ Comments
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
            await _sessionManager.SetSessionData(chatId, "DriverUserState", driverState); //************ Comments
        }

        private async Task SendRegistrationSummary(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var driverRegistration = await _sessionManager.GetSessionData<DriverDTO>(chatId, "DriverRegistration"); //************ Comments
            var summaryText = $"סיכום הרשמה:\n" +
                              $"שם מלא: {driverRegistration.FullName}\n" +
                              $"פרטי רכב: {driverRegistration.CarDetails}\n" +
                              $"מספר טלפון: {driverRegistration.PhoneNumber}\n\n" +
                              $"שימו ❤️\n\n" +
                              $"אם הפרטים שגויים לא תוכלו לאסוף נוסעים 😔\n\n" +
                              $"האם כל הפרטים נכונים? ";

            await BotDriversResponseService.SendRegistrationSummaryAsync(botClient, chatId, summaryText, cancellationToken);

            //var driverState = "awaiting_confirmation"; //************ Comments
            //_sessionManager.SetSessionData(chatId, "DriverUserState", driverState); //************ Comments
        }
    }
}
