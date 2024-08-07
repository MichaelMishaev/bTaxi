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

namespace BL.Services.Customers.Handlers
{
    public class CustomerHandler
    {
        UserRepository userRepository = new UserRepository();
        DriverRepository driverRepository = new DriverRepository();
        OrderRepository orderRepository = new OrderRepository();
        public  async Task CallbackHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, UserMenuHandle _handleUser)
        {
                await UpdateTypeMessage.function(botClient, update, cancellationToken, _handleUser);

                var callbackQuery = update.CallbackQuery;

                if (callbackQuery != null)
                {
                    var chatId = callbackQuery.Message.Chat.Id;
                    var callbackData = callbackQuery.Data;

                    if (callbackData != "order_taxi" && SessionManager.GetSessionData<UserOrder>(chatId, "UserOrder") == null)
                    {
                        await MainMenuService.DisplayMainMenu(botClient, chatId, cancellationToken);
                        return;
                    }

                    else if (callbackData == "order_taxi")
                    {
                        var newOrder = new UserOrder();
                        newOrder.CurrentStep = "entering_origin_city"; // Initialize the order and set the current step to entering_origin_city
                        SessionManager.SetSessionData(chatId, "UserOrder", newOrder); // Save the order in the session

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
                        SessionManager.SetSessionData(chatId, "UserState", userState); // Save the state in the session

                        // Ask the user to enter the origin city
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "הכנס עיר מוצא:",
                            cancellationToken: cancellationToken
                        );
                    }



                    ////////////////////////// FOR TEST ONLY
                    else if (callbackData == "default_values")
                    {
                        var userOrder = SessionManager.GetSessionData<UserOrder>(chatId, "UserOrder") ?? new UserOrder();
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

                        SessionManager.SetSessionData(chatId, "UserOrder", userOrder); // Save session data

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
                        var userOrder = SessionManager.GetSessionData<UserOrder>(chatId, "UserOrder");
                        userOrder.CurrentStep = callbackData;
                        SessionManager.SetSessionData(chatId, "UserOrder", userOrder); // Save session data

                        var userState = SessionManager.GetSessionData<string>(chatId, "UserState") ?? string.Empty;
                        userState = callbackData;
                        SessionManager.SetSessionData(chatId, "UserState", userState); // Save session data

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
                        var userOrder = SessionManager.GetSessionData<UserOrder>(chatId, "UserOrder");
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

                        var LS = new LocationService();
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
                        SessionManager.SetSessionData(chatId, "UserOrder", userOrder); // Save session data
                        SessionManager.SetSessionData(chatId, "UserState", userState); // Save session data
                    }
                    else if (callbackData == "confirm_order" && SessionManager.GetSessionData<string>(chatId, "UserState") == "awaiting_confirmation")
                    {
                        var userOrder = SessionManager.GetSessionData<UserOrder>(chatId, "UserOrder");
                        userOrder.CurrentStep = "order_confirmed";
                        userOrder.userId = update.CallbackQuery.From.Id;

                        // Pass the bidId to the PlaceOrderAsync method
                        int orderId = await orderRepository.PlaceOrderAsync(userOrder, userOrder.BidId);

                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: @"ההזמנה נשלחה, עדכון יתקבל כאן לגבי זמן הגעה משואר",
                            cancellationToken: cancellationToken
                        );

                        var order = userOrder;
                        string separator = new string('-', 30); // Separator line
                        string dateTimeNow = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); // Formatted date-time
                        string orderNotification = $"{separator}\n" +
                                                   $"הודעה חדשה התקבלה בתאריך: {dateTimeNow}\n\n" + // Add formatted date-time
                                                   $"הזמנה חדשה התקבלה!\n" +
                                                   $"מ: {order.FromAddress}\n" +
                                                   $"אל: {order.ToAddress}\n" +
                        $"מחיר: {order.BidAmount:F2} ₪\n" +  // Updated to show bid amount
                                                   $"מספר טלפון: {order.PhoneNumber}\n" +
                                                   $"הערות: {order.Remarks}";

                        var workingDrivers = await driverRepository.GetWorkingDriversAsync();

                        foreach (var driver in workingDrivers)
                        {
                            long driverChatId = Convert.ToInt64(driver.DriverId);

                            try
                            {
                                var orderActionsMenu = new InlineKeyboardMarkup(new[]
                                {
                            new InlineKeyboardButton[]
                            {
                                InlineKeyboardButton.WithCallbackData("קבל הזמנה", $"accept_order:{orderId}"),
                                InlineKeyboardButton.WithCallbackData("הצע מחיר חדש", $"bid_order:{orderId}")
                            }
                        });

                                await TypesManual.botDriver.SendTextMessageAsync(
                                    chatId: driverChatId,
                                    text: orderNotification,
                                    replyMarkup: orderActionsMenu,
                                    cancellationToken: cancellationToken
                                );
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to send message to driver {driver.DriverId}: {ex.Message}");
                            }
                        }

                        SessionManager.RemoveSessionData(chatId, "UserState"); // Clear session data
                    }
                    else if (callbackData == "cancel_order")
                    {
                        var newOrder = new UserOrder();
                        SessionManager.SetSessionData(chatId, "UserOrder", newOrder); // Save session data

                        var mainMenuButtons = MenuMethods.mainMenuButtons();
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Welcome! Choose an option:",
                            replyMarkup: mainMenuButtons,
                            cancellationToken: cancellationToken
                        );
                    }
                    else if (callbackData == "confirm_yes" || callbackData == "confirm_no")
                    {
                        var userOrder = SessionManager.GetSessionData<UserOrder>(chatId, "UserOrder");
                        var userState = SessionManager.GetSessionData<string>(chatId, "UserState");

                        if (callbackData == "confirm_yes")
                        {
                            await _handleUser.HandleUserInput(chatId, "yes", cancellationToken, botClient, userOrder, userState, callbackQuery.Message);
                        }
                        else if (callbackData == "confirm_no")
                        {
                            await _handleUser.HandleUserInput(chatId, "no", cancellationToken, botClient, userOrder, userState, callbackQuery.Message);
                        }

                        SessionManager.SetSessionData(chatId, "UserOrder", userOrder); // Save session data
                        SessionManager.SetSessionData(chatId, "UserState", userState); // Save session data
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
                        var userState = SessionManager.GetSessionData<string>(chatId, "UserState");
                        if (userState != null && userState.StartsWith("awaiting_finish:"))
                        {
                            var stateData = userState.Split(':');
                            if (stateData.Length == 3)
                            {
                                var messageId = int.Parse(stateData[2]);
                                await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
                                SessionManager.RemoveSessionData(chatId, "UserState");  // Clear the state after processing
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
                        SessionManager.SetSessionData(chatId, "UserState", userState); // Save session data
                    }
                    else if (callbackData.StartsWith("rate_"))
                    {
                        // Extract order ID and rating from callback data
                        var data = callbackData.Split('_');
                        var orderId = int.Parse(data[1]);
                        var rating = int.Parse(data[2]);

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

                        var userState = SessionManager.GetSessionData<string>(chatId, "UserState");
                        if (userState != null)
                        {
                            var stateData = userState.Split(':');
                            if (stateData.Length == 3 && stateData[0] == "awaiting_rating")
                            {
                                var messageId = int.Parse(stateData[2]);

                                // Delete the previous messages (rating buttons and thank you message)
                                await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
                                await botClient.DeleteMessageAsync(chatId, thankYouMessage.MessageId, cancellationToken);

                                // Remove state
                                SessionManager.RemoveSessionData(chatId, "UserState");
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
                                SessionManager.RemoveSessionData(chatId, "UserState");
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
                            SessionManager.SetSessionData(chatId, "UserState", userState); // Save session data
                        }
                        else
                        {
                            // This is the first bid, insert it and get the parentId
                            var initialBidId = await orderRepository.InsertCustomerFirstBidAsync(chatId, customerId, 0); // 0 is placeholder
                            var userOrder = SessionManager.GetSessionData<UserOrder>(chatId, "UserOrder");
                            userOrder.ParentId = initialBidId;

                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "אנא הזן את המחיר שאתה מציע עבור הנסיעה:",
                                cancellationToken: cancellationToken
                            );

                            var userState = $"awaiting_bid:{initialBidId}";
                            SessionManager.SetSessionData(chatId, "UserOrder", userOrder); // Save session data
                            SessionManager.SetSessionData(chatId, "UserState", userState); // Save session data
                        }
                    }
                    else if (callbackQuery.Data.StartsWith("accept_bid:"))
                    {
                        var parts = callbackQuery.Data.Split(':');
                        var orderId = long.Parse(parts[1]);
                        var driverId = callbackQuery.From.Id;

                        // Update the bid as accepted in the database
                        await orderRepository.UpdateBidStatusAsync(orderId, driverId, true);

                        // Retrieve the customer order
                        var customerOrder = await orderRepository.GetOrderByIdAsync(orderId);

                        // Notify the driver that the bid was accepted and provide customer details
                        await botClient.SendTextMessageAsync(
                            chatId: driverId,
                            text: "הצעת המחיר שלך התקבלה. הנה פרטי הלקוח:",
                            cancellationToken: cancellationToken
                        );

                        await botClient.SendTextMessageAsync(
                            chatId: driverId,
                            text: $"פרטי לקוח:\n" +
                        $"טלפון: {customerOrder.PhoneNumber}\n" +
                                  $"כתובת איסוף: {customerOrder.FromAddress}\n" +
                                  $"כתובת יעד: {customerOrder.ToAddress}",
                            cancellationToken: cancellationToken
                        );

                        // Notify the customer about the accepted bid
                        await TypesManual.botClient.SendTextMessageAsync(
                            chatId: customerOrder.userId,
                            text: "הצעת המחיר התקבלה. הנהג יקבל את פרטי ההזמנה שלך ויצור איתך קשר בקרוב.",
                            cancellationToken: cancellationToken
                        );

                        // Optionally update user state or any other necessary actions
                        SessionManager.RemoveSessionData(chatId, "UserState"); // Clear session data
                    }
                }
            

        }
    }
}
