using BL.Helpers.FareCalculate;
using BL.Helpers;
using BL.Services.Customers.Functions;
using Common.DTO;
using Common.Services;
using DAL;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using telegramB.Menus;
using telegramB.Objects;
using telegramB;
using Telegram.Bot;
using Telegram.Bot.Types;
using BL.Helpers.logger;
using BL.Helpers.MessageSending;

namespace BL.Services.Customers.Handlers
{
    public class CustomerHandler
    {
        UserRepository userRepository = new UserRepository();
        DriverRepository driverRepository = new DriverRepository();
        OrderRepository orderRepository = new OrderRepository();
        private readonly SessionManager _sessionManager;
        private UpdateTypeMessage _updateTypeMessage;
        private SendMessage sendMessage = null;
        public CustomerHandler(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
            _updateTypeMessage = new UpdateTypeMessage(_sessionManager);
             sendMessage = new SendMessage();
        }
        public async Task CallbackHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, UserMenuHandle _handleUser)
        {
            //await UpdateTypeMessage.function(botClient, update, cancellationToken, _handleUser);

            var callbackQuery = update.CallbackQuery;

            if (callbackQuery != null)
            {
                var chatId = callbackQuery.Message.Chat.Id;
                var callbackData = callbackQuery.Data;

                if ((callbackData != "order_taxi" && await _sessionManager.GetSessionData<UserOrder>(chatId, "UserOrder") == null) && callbackData == null)
                {
                    await MainMenuService.DisplayMainMenu(botClient, chatId, cancellationToken);
                    return;
                }

                else if (callbackData == "order_taxi")
                {
                    var newOrder = new UserOrder();
                    newOrder.CurrentStep = "entering_origin_city"; // Initialize the order and set the current step to entering_origin_city
                    await _sessionManager.SetSessionData(chatId, "UserOrder", newOrder); // Save the order in the session

                    bool isPremium = callbackQuery.From.IsPremium ?? false;
                    UserDTO userDTO = new UserDTO
                    {
                        FirstName = string.IsNullOrWhiteSpace(callbackQuery.From.FirstName) ? "unknown" : callbackQuery.From.FirstName,
                        UserId = callbackQuery.From.Id.ToString(),
                        IsBot = callbackQuery.From.IsBot ? 1 : 0,
                        IsPremium = isPremium ? 1 : 0,
                        LastName = string.IsNullOrWhiteSpace(callbackQuery.From.LastName) ? "unknown" : callbackQuery.From.LastName,
                        UserName = string.IsNullOrWhiteSpace(callbackQuery.From.Username) ? "unknown" : callbackQuery.From.Username,
                        PhoneNumber = "RBD"
                    };
                    await userRepository.InsertUserAsync(userDTO);

                    // Set the user state indicating that the process has started
                    string userState = "entering_origin_city";
                    await _sessionManager.SetSessionData(chatId, "UserState", userState); // Save the state in the session

                    // Ask the user to enter the origin city
                    var replyMarkup = new ReplyKeyboardMarkup(new[]
                    {
                                new KeyboardButton("שלח מיקום") { RequestLocation = true }
                            })
                    {
                        ResizeKeyboard = true,
                        OneTimeKeyboard = true
                    };

                    await sendMessage.SafeSendMessageWithReplyMarkupAsync(
                         botClient,
                        chatId: chatId,
                        text: "הכנס עיר מוצא או שלח את המיקום שלך:",
                        replyMarkup: replyMarkup,
                        cancellationToken: cancellationToken
                    );
                    //await botClient.SendTextMessageAsync(
                    //    chatId: chatId,
                    //    text: "הכנס עיר מוצא או שלח את המיקום שלך:",
                    //    replyMarkup: replyMarkup,
                    //    cancellationToken: cancellationToken
                    //);


                }



                ////////////////////////// FOR TEST ONLY
                else if (callbackData == "default_values")
                {
                    var userOrder = await _sessionManager.GetSessionData<UserOrder>(chatId, "UserOrder") ?? new UserOrder();
                    userOrder.FromAddress = new AddressDTO
                    {
                        City = "אריאל",
                        Street = "דרך הציונות",
                        StreetNumber = 20
                    };

                    // Set default ToAddress
                    userOrder.ToAddress = new AddressDTO
                    {
                        City = "נתניה",
                        Street = "ויצמן",
                        StreetNumber = 1
                    };

                    //userOrder.FromAddress = "דרך הציונות 20 אריאל"; // Default From Address
                    //userOrder.ToAddress = "ויצמן 1 נתניה"; // Default To Address
                    userOrder.PhoneNumber = "0544654456"; // Default Phone Number
                    userOrder.Remarks = "אין הערות"; // Default Remarks
                    userOrder.CurrentStep = "default_values"; // Update current step

                    await _sessionManager.SetSessionData(chatId, "UserOrder", userOrder); // Save session data

                    var summaryText = $"כתובת נקודת האיסוף: {userOrder.FromAddress}\n" +
                                      $"כתובת יעד: {userOrder.ToAddress}\n" +
                                      $"מספר טלפון: {userOrder.PhoneNumber}\n" +
                                      $"הערות: {userOrder.Remarks}";

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "ברירת המחדל נקבעה:\n" + summaryText,
                        replyMarkup: MenuMethods.ShowUpdatedMenu(userOrder),
                        cancellationToken: cancellationToken
                    );
                }



                else if (callbackData == "from" || callbackData == "to" || callbackData == "passengers" || callbackData == "phone" || callbackData == "remarks")
                {
                    var userOrder = await _sessionManager.GetSessionData<UserOrder>(chatId, "UserOrder");
                    userOrder.CurrentStep = callbackData;
                    await _sessionManager.SetSessionData(chatId, "UserOrder", userOrder); // Save session data

                    var userState = await _sessionManager.GetSessionData<string>(chatId, "UserState") ?? string.Empty;
                    userState = callbackData;
                    await _sessionManager.SetSessionData(chatId, "UserState", userState); // Save session data

                    string promptText = callbackData switch
                    {
                        "from" => "היכן לאסוף:",
                        "to" => "נא להזין יעד:",
                        "passengers" => "יש להזין מספר נוסעים:",
                        "phone" => "מספר טלפון:",
                        "remarks" => "הערות",
                        _ => ""
                    };

                    IReplyMarkup replyMarkup = null;
                    if (callbackData == "from")
                    {
                        replyMarkup = new ReplyKeyboardMarkup(new[]
                        {
                        new KeyboardButton[] { KeyboardButton.WithRequestLocation("שלח מיקום") }
                    })
                        {
                            ResizeKeyboard = true,
                            OneTimeKeyboard = true
                        };
                    }

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: promptText,
                        replyMarkup: replyMarkup,
                        cancellationToken: cancellationToken
                    );
                }
                else if (callbackData == "submit")
                {
                    var userOrder = await _sessionManager.GetSessionData<UserOrder>(chatId, "UserOrder");
                    if ((string.IsNullOrWhiteSpace(userOrder.PhoneNumber) || userOrder.PhoneNumber == "0") ||
                        (string.IsNullOrWhiteSpace($"{userOrder.FromAddress.City}")) || string.IsNullOrWhiteSpace(userOrder.ToAddress.City))
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                        text: @"לא הוזן יעד\מקום איסוף\ מספר טלפון",
                            cancellationToken: cancellationToken
                        );
                        return;
                    }

                    var LS = new LocationService(_sessionManager);
                    double distanceKm = await LS.CalculateDistance(userOrder.FromAddress, userOrder.ToAddress, botClient, cancellationToken, chatId);

                    var fareCalculator = new TaxiFareCalculate(new FareStructure
                    {
                        BookingFee = 5.63,
                        InitialMeter = 12.2,
                        FarePerMinuteA = 1.91,
                        FarePerKmA = 1.91,
                        FarePerMinuteB = 2.29,
                        FarePerKmB = 2.29,
                        FarePerMinuteC = 2.67,
                        FarePerKmC = 2.67
                    });

                    var fareType = fareCalculator.DetermineFareType(DateTime.Now);
                    double averageSpeedKmh = 60.0;
                    double rideDurationHours = distanceKm / averageSpeedKmh;
                    double rideDurationMinutes = rideDurationHours * 60.0;

                    double fare = fareCalculator.CalculateFare(fareType, distanceKm, rideDurationMinutes);

                    string fareDetails = $"המרחק הינו {distanceKm}, מחיר משוער של מונית רגילה הינו: {fare:F2} ₪\n";

                    var orderSummary = $"סיכום ההזמנה שלך:\n" +
                                       $"נקודת איסוף: {userOrder.FromAddress}\n" +
                                       $"יעד: {userOrder.ToAddress}\n" +
                                       $"מחיר משוער: {fare:F2} ₪\n" + // Added approximate price
                                       $"מספר טלפון: {userOrder.PhoneNumber}\n" +
                    $"הערות: {userOrder.Remarks}\n" +
                                       $" {fareDetails}\n";

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: orderSummary,
                        cancellationToken: cancellationToken
                    );

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "אנא הזן את המחיר שאתה מציע עבור הנסיעה:",
                        cancellationToken: cancellationToken
                    );

                    var userState = $"awaiting_bid: {chatId}";
                    await _sessionManager.SetSessionData(chatId, "UserOrder", userOrder); // Save session data
                    await _sessionManager.SetSessionData(chatId, "UserState", userState); // Save session data
                }

                else if (callbackData == "enter_bid" && await _sessionManager.GetSessionData<string>(chatId, "UserState") == "awaiting_bid")
                {
                    //callbackQuery.Message.Text

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: callbackQuery.Message.Text,
                        cancellationToken: cancellationToken
                    );

                    await botClient.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: "<b>----------------------------</b>",
                                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                    cancellationToken: cancellationToken
                                );

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "הכנס את המחיר שאתה מציע עבור הנסיעה:",
                        cancellationToken: cancellationToken
                    );

                    bool isDeleted = await Validators.DeleteMessage(botClient,chatId, callbackQuery.Message.MessageId, cancellationToken);
                    //await botClient.DeleteMessageAsync(chatId, callbackQuery.Message.MessageId, cancellationToken);
                    return; // Break after sending the bid entry prompt
                }


                else if (callbackData == "confirm_order" && await _sessionManager.GetSessionData<string>(chatId, "UserState") == "awaiting_confirmation")
                {
                    var userOrder = await _sessionManager.GetSessionData<UserOrder>(chatId, "UserOrder");
                    userOrder.OrderId = userOrder.OrderId == 0 ? userOrder.Id : userOrder.OrderId;
                    userOrder.CurrentStep = "order_confirmed";
                    userOrder.userId = update.CallbackQuery.From.Id;


                    decimal BidAmount = userOrder.BidAmount;
                    //Check if there is bidId
                    //Get the order by bidId cos BidId transfered as contextual param
                    if (userOrder.BidId > 0 && userOrder.FromAddress == null)
                    {
                        userOrder = await orderRepository.GetOrderByBidIdAsync(userOrder.BidId);
                        userOrder.BidAmount = BidAmount;

                    }

                    // Pass the bidId to the PlaceOrderAsync method
                    else if ((userOrder.OrderId == 0 && userOrder.Id == 0) || userOrder.BidId==0)
                    {
                        userOrder.OrderId = await orderRepository.PlaceOrderAsync(userOrder, userOrder.BidId);
                        //         SessionManager.SetSessionData(chatId, "UserOrder", userOrder);
                    }
                    else
                    {
                        await orderRepository.UpdateOrderWithNewBidAsync(userOrder);
                    }
                    userOrder.OrderId = userOrder.OrderId == 0 ? userOrder.Id : userOrder.OrderId;
                    await _sessionManager.SetSessionData(chatId, "UserOrder", userOrder);

                    //await botClient.DeleteMessageAsync(chatId, callbackQuery.Message.MessageId, cancellationToken);
                    bool isDeleted = await Validators.DeleteMessage(botClient, chatId, callbackQuery.Message.MessageId, cancellationToken);
                    var order = userOrder;
                    string separator = new string('-', 30); // Separator line
                    string dateTimeNow = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); // Formatted date-time
                    string orderNotification = $"{separator}\n" +
                                               $"הודעה חדשה התקבלה בתאריך: {dateTimeNow}\n\n" + // Add formatted date-time
                                               $"הזמנה חדשה התקבלה!\n" +
                                               $"מ: {order.FromAddress.GetFormattedAddress()}\n" +
                                               $"אל: {order.ToAddress.GetFormattedAddress()}\n" +
                    $"מחיר: {order.BidAmount:F2} ₪\n" +  // Updated to show bid amount
                                               $"מספר טלפון: {order.PhoneNumber}\n" +
                                               $"הערות: {order.Remarks}";

                    var workingDrivers = await driverRepository.GetWorkingDriversAsync();

                    if (workingDrivers.Count == 0)
                    {
                        string textNoDrivers = $"אין כרגע נהגים זמינים 😔 \n" +
                                                "נסו במועד מאוחר יותר 🕰 \n" +
                                                "שלכם,  bTrip 🚕";
                        Console.WriteLine($"There is order but no Drivers: {DateTime.Now}");
                        await botClient.SendTextMessageAsync(
                                       chatId: chatId,
                                       text: textNoDrivers,
                                       cancellationToken: cancellationToken
                                   );
                        await _updateTypeMessage.ResetSessionData(chatId, cancellationToken, botClient);
                        await MainMenuService.DisplayMainMenu(botClient, chatId, cancellationToken);

                        return;

                    }

                    await botClient.SendTextMessageAsync(
                     chatId: chatId,
                     text: @"הודעה נשלחה לנהגים, עדכון יתקבל כאן...",
                     cancellationToken: cancellationToken
                 );

                    long? sessionDriverChatId = null;

                    bool isSentTodriver = false;
                    //check if the driver that gave a new bid is working
                    
                    foreach (var driver in workingDrivers)
                    {

                        string sessionKey = $"{userOrder.OrderId}:driver:{driver.DriverId}";

                        sessionDriverChatId = await _sessionManager.GetSessionData<long?>(sessionKey, "DriverChatId");
                        long? IbidId = await _sessionManager.GetSessionData<long?>(sessionKey, "BidId");
                        if (IbidId == null) continue;
                        var driverId = await orderRepository.GetDriverIdByBidIdAsync((int)IbidId);
                        if (sessionDriverChatId.HasValue && driver.DriverId == driverId.ToString())
                        {
                            //string[] parts = sessionKey.Split(':');
                            //string extractedNumber = parts[2];

                            await TypesManual.botDriver.SendTextMessageAsync(
                            chatId: driverId,
                            text: orderNotification,
                            replyMarkup: MenuMethods.AcceptbidOrMakeNewMenu(userOrder),
                            cancellationToken: cancellationToken
                        );
                            _ = await Validators.DeleteMessage(TypesManual.botDriver, (long)driverId, (int)callbackQuery.Message.MessageId, cancellationToken);
                            isSentTodriver = true;
                            await _sessionManager.RemoveSessionData(sessionKey, "DriverChatId");
                            break; // Found the correct driver
                        }

                    }



                    foreach (var driver in workingDrivers)
                    {
                        if (isSentTodriver) break;
                        long driverChatId = Convert.ToInt64(driver.DriverId);
                        try
                        {

                            await TypesManual.botDriver.SendTextMessageAsync(
                                chatId: driverChatId,
                                text: orderNotification,
                                replyMarkup: MenuMethods.AcceptbidOrMakeNewMenu(userOrder),
                                cancellationToken: cancellationToken
                            );
                        }
                        catch (Exception ex)
                        {


                            string err = "######################   SERVICE WORKS   ################################" +
                                $"Failed to send message to driver {driver.DriverId}: {ex.Message}";
                            ConsolePrintService.exceptionErrorPrint("");

                        }
                    }

                    await _sessionManager.RemoveSessionData(chatId, "UserState"); // Clear session data
                }
                else if (callbackData == "cancel_order")
                {
                    await _sessionManager.RemoveSessionData(chatId, "UserState");
                    await _sessionManager.RemoveSessionData(chatId, "UserOrder");

                    await botClient.SendTextMessageAsync(
                         chatId: chatId,
                         text: $"❌ ההזמנה בוטלה ",
                         cancellationToken: cancellationToken
                     );
                    await MainMenuService.DisplayMainMenu(botClient, chatId, cancellationToken);
                }
                else if (callbackData == "confirm_yes" || callbackData == "confirm_no")
                {
                    var userOrder = await _sessionManager.GetSessionData<UserOrder>(chatId, "UserOrder");
                    var userState = await _sessionManager.GetSessionData<string>(chatId, "UserState");

                    if (callbackData == "confirm_yes")
                    {
                        await _handleUser.HandleUserInput(chatId, "yes", cancellationToken, botClient, userOrder, userState, callbackQuery.Message);
                    }
                    else if (callbackData == "confirm_no")
                    {
                        await _handleUser.HandleUserInput(chatId, "no", cancellationToken, botClient, userOrder, userState, callbackQuery.Message);
                    }

                    await botClient.DeleteMessageAsync(chatId, callbackQuery.Message.MessageId, cancellationToken); //Delete the 'yes/no message'
                    await _sessionManager.SetSessionData(chatId, "UserOrder", userOrder); // Save session data
                    await _sessionManager.SetSessionData(chatId, "UserState", userState); // Save session data
                }
                else if (callbackQuery.Data.StartsWith("finish_ride:"))
                {
                    var orderId = int.Parse(callbackQuery.Data.Split(':')[1]);

                    // Update the order status to closed
                    await orderRepository.CloseOrderAsync(orderId);

                    // Notify the user that the ride has been finished
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "הנסיעה סומנה כהושלמה. תודה שנסעת איתנו!",
                        cancellationToken: cancellationToken
                    );

                    // Check and remove the "ride finished" button message
                    var userState = await _sessionManager.GetSessionData<string>(chatId, "UserState");
                    if (userState != null && userState.StartsWith("awaiting_finish:"))
                    {
                        var stateData = userState.Split(':');
                        if (stateData.Length == 3)
                        {
                            var messageId = int.Parse(stateData[2]);
                            await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
                            await _sessionManager.RemoveSessionData(chatId, "UserState");  // Clear the state after processing
                        }
                    }

                    // Prompt the user to rate the driver using inline buttons
                    var ratingButtons = MenuMethods.GetRatingButtons(orderId);
                    var ratingMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                                        text: "אנא דרג את הנהג (1-5):",
                                        replyMarkup: ratingButtons,
                                        cancellationToken: cancellationToken
                    );

                    // Set the user state to awaiting_rating
                    userState = $"awaiting_rating:{orderId}:{ratingMessage.MessageId}";
                    await _sessionManager.SetSessionData(chatId, "UserState", userState); // Save session data
                }
                else if (callbackData.StartsWith("rate_"))
                {

                    // Extract order ID and rating from callback data
                    var data = callbackData.Split('_');
                    var orderId = int.Parse(data[1]);
                    var rating = int.Parse(data[2]);

                    ConsolePrintService.simpleConsoleMessage($"Rating driver for order: {orderId}");
                    // Save the rating in the database
                    await orderRepository.SaveDriverRatingAsync(orderId, rating);

                    // Notify the user that the rating has been saved
                    var thankYouMessage = await botClient.SendTextMessageAsync(
                         chatId: chatId,
                         text: $"תודה על הדירוג 😍! נשמח לראות אותך שוב 🚕",
                         cancellationToken: cancellationToken
                     );

                    var orderTaxiMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                        text: "להזמנת הסעה, אנא בחר באפשרות הבאה:",
                        replyMarkup: MenuMethods.mainMenuButtons(),
                        cancellationToken: cancellationToken
                    );

                    var userState = await _sessionManager.GetSessionData<string>(chatId, "UserState");
                    if (userState != null)
                    {
                        var stateData = userState.Split(':');
                        if (stateData.Length == 3 && stateData[0] == "awaiting_rating")
                        {
                            var messageId = int.Parse(stateData[2]);

                            // Delete the previous messages (rating buttons and thank you message)
                            await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
                            //await botClient.DeleteMessageAsync(chatId, thankYouMessage.MessageId, cancellationToken);

                            // Remove state
                            await _sessionManager.RemoveSessionData(chatId, "UserState");
                        }
                    }

                    else if (userState != null && userState.StartsWith("awaiting_rating:"))
                    {
                        orderId = int.Parse(userState.Split(':')[1]);
                        if (int.TryParse(update.Message.Text, out rating) && rating >= 1 && rating <= 5)
                        {
                            // Save the rating in the database
                            await orderRepository.InsertRatingAsync(orderId, rating);

                            // Notify the user that the rating has been saved
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "תודה על הדירוג! נשמח לראות אותך שוב.",
                                cancellationToken: cancellationToken
                            );

                            // Clear the state
                            await _sessionManager.RemoveSessionData(chatId, "UserState");
                        }
                        else
                        {
                            // Notify the user that the rating is invalid and ask again
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "דירוג לא תקין. אנא דרג את הנהג (1-5 כוכבים):",
                                cancellationToken: cancellationToken
                            );
                        }
                    }
                }
                else if (callbackQuery.Data.StartsWith("new_bid:"))
                {
                    var customerId = callbackQuery.From.Id;
                    chatId = callbackQuery.Message.Chat.Id;

                    // Check if there's already an active bid
                    long? parentId = await orderRepository.GetParentBidIdAsync(chatId, customerId);

                    if (parentId.HasValue)
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "אנא הזן את המחיר החדש שאתה מציע:",
                            cancellationToken: cancellationToken
                        );

                        var userState = $"awaiting_new_customer_bid:{parentId.Value}";
                        await _sessionManager.SetSessionData(chatId, "UserState", userState); // Save session data
                    }
                    else
                    {
                        // This is the first bid, insert it and get the parentId

                        //HOW CAME HERE? WE HAVE ANOTHER iNSERTbID
                        var initialBidId = await orderRepository.InsertAndThenUpdateCustomerBidAsync(chatId, customerId, 0); // 0 is placeholder
                        var userOrder = await _sessionManager.GetSessionData<UserOrder>(chatId, "UserOrder");
                        userOrder.ParentId = initialBidId;

                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "אנא הזן את המחיר שאתה מציע עבור הנסיעה:",
                            cancellationToken: cancellationToken
                        );

                        var userState = $"awaiting_bid:{initialBidId}";
                        await _sessionManager.SetSessionData(chatId, "UserOrder", userOrder); // Save session data
                        await _sessionManager.SetSessionData(chatId, "UserState", userState); // Save session data
                    }
                }
                else if (callbackQuery.Data.StartsWith("accept_bid:"))
                {

                    var parts = callbackQuery.Data.Split(':');
                    var orderId = int.Parse(parts[1]);
                    var driverId = await orderRepository.GetDriverIdByBidIdAsync(int.Parse(parts[2]));
                    //callbackQuery.From.Id;
                    ConsolePrintService.CheckPointMessage($"Customer excepted order {orderId} , time: {DateTime.Now} , " +
                        $"Driver details: {driverId}");
                    if (driverId == null)
                    {
                        ConsolePrintService.exceptionErrorPrint($"For biID: {parts[2]} driver null exception");
                        return;
                    }
                    // Update the bid as accepted in the database
                    await orderRepository.AssignOrderToDriverAsync(orderId, (long)driverId);

                    // Retrieve the customer order
                    var customerOrder = await orderRepository.GetOrderByIdAsync(orderId);



                    //################################################################
                    //              Send messages to Driver
                    //################################################################

                    // Notify the driver that the bid was accepted and provide customer details
                    await TypesManual.botDriver.SendTextMessageAsync(
                        chatId: driverId,
                        text: "הצעת המחיר שלך התקבלה. להלן פרטי הלקוח:",
                        cancellationToken: cancellationToken
                    );




                    await TypesManual.botDriver.SendTextMessageAsync(
                        chatId: driverId,
                        text: $"פרטי לקוח:\n" +
                              $"טלפון: {customerOrder.PhoneNumber}\n" +
                              $"כתובת איסוף: {customerOrder.FromAddress.GetFormattedAddress()}\n" +
                              $"כתובת יעד: {customerOrder.ToAddress.GetFormattedAddress()}",
                        cancellationToken: cancellationToken
                    );
                    var localDriverState = $"awaiting_eta:{orderId}"; //************ Comments
                    await _sessionManager.SetSessionData((long)driverId, "DriverUserState", localDriverState); //************ Comments

                    var cancelOption = MenuMethods.cancelOrder(orderId);

                    await TypesManual.botDriver.SendTextMessageAsync(
                        chatId: driverId,
                        text: "אם ברצונך לבטל 🫣 ",
                        replyMarkup: cancelOption,
                        cancellationToken: cancellationToken
                    );

                    await TypesManual.botDriver.SendTextMessageAsync(
                        chatId: driverId,
                        text: "שימו ❤️, עד שלא שלחתם זמן הגעה, נהג אחר עלול לקחת את הנסיעה.",
                        cancellationToken: cancellationToken
                    );

                    await TypesManual.botDriver.SendTextMessageAsync(
                        chatId: driverId,
                        text: "כמה זמן ייקח לך להגיע? ⌛️(בזמן בדקות)",
                        cancellationToken: cancellationToken
                    );
                    //################################################################
                    //################################################################
                    //################################################################


                    // Notify the customer about the accepted bid
                    await TypesManual.botClient.SendTextMessageAsync(
                        chatId: customerOrder.userId,
                        text: "האישור הועבר לנהג, ממתינים לאישור......",
                        cancellationToken: cancellationToken
                    );
                    await TypesManual.botClient.SendTextMessageAsync(
                        chatId: customerOrder.userId,
                        text: "שימו ❤️, עד שהנהג לא אישר, יכול להיות שתקבלו הצעת מחיר חדשה מנהג אחר.",
                        cancellationToken: cancellationToken
                    );


                    // Optionally update user state or any other necessary actions
                    await _sessionManager.RemoveSessionData(chatId, "UserState"); // Clear session data
                }
            }


        }
    }
}
