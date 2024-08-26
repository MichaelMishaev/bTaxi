using BL.Services.Drivers.StaticFiles;
using Common.DTO;
using Common.Services;
using DAL;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using telegramB.Menus;

namespace telegramB.Services
{
    public static class BotDriversResponseService
    {
        private static readonly Dictionary<long, DriverDTO> driverRegistrations = new Dictionary<long, DriverDTO>();
        private static readonly Dictionary<long, string> userStates = new Dictionary<long, string>();
        private static readonly DriverRepository driverRepository = new DriverRepository();
        private static readonly SessionManager _sessionManager;


        static BotDriversResponseService()
        {
            // Initialize SessionManager with your Redis connection string
            _sessionManager = new SessionManager("localhost:6379");
        }
        // Encapsulate dictionary access
        private static void AddOrUpdateDriverRegistration(long chatId, DriverDTO driver)
        {
            driverRegistrations[chatId] = driver;
        }

        private static DriverDTO GetDriverRegistration(long chatId)
        {
            return driverRegistrations.TryGetValue(chatId, out var driver) ? driver : null;
        }

        private static void RemoveDriverRegistration(long chatId)
        {
            driverRegistrations.Remove(chatId);
        }

        private static void SetUserState(long chatId, string state)
        {
            userStates[chatId] = state;
        }

        private static string GetUserState(long chatId)
        {
            return userStates.TryGetValue(chatId, out var state) ? state : null;
        }

        private static void RemoveUserState(long chatId)
        {
            userStates.Remove(chatId);
        }

        public static async Task StartRegistration(ITelegramBotClient botClient, long chatId, int messageId, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Driver {chatId} started regestration");
            AddOrUpdateDriverRegistration(chatId, new DriverDTO());
            SetUserState(chatId, keywords.AwaitingNameState);


            await _sessionManager.SetSessionData(chatId, "DriverUserState", keywords.AwaitingNameState);

            await botClient.SendTextMessageAsync(chatId: chatId, text: "ברוכים הבאים לתהליך הרישום בלה בלה אנחנו בלה בלה ---- TBD",cancellationToken: cancellationToken
            );

            await botClient.SendTextMessageAsync(
                //EditMessageTextAsync(
                chatId: chatId,
             //   messageId: messageId,
                text: "מה השם המלא שלך?",
                cancellationToken: cancellationToken
            );
        }

        public static async Task HandleDriverInput(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken)
        {
            var currentState = GetUserState(chatId);
            if (currentState == null)
            {
                await HandleInvalidState(botClient, chatId, cancellationToken); // Handle missing state
                return;
            }

            switch (currentState)
            {
                case keywords.AwaitingNameState:
                    await HandleAwaitingNameInput(botClient, chatId, messageText, cancellationToken);
                    break;

                case keywords.AwaitingCarDetailsState:
                    await HandleAwaitingCarDetailsInput(botClient, chatId, messageText, cancellationToken);
                    break;

                case keywords.AwaitingPhoneNumberState:
                    await HandleAwaitingPhoneNumberInput(botClient, chatId, messageText, cancellationToken);
                    break;

                default:
                    await HandleInvalidState(botClient, chatId, cancellationToken); // Handle unknown state
                    break;
            }
        }

        private static async Task HandleInvalidState(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            await SendTextMessageAsync(botClient, chatId, "משהו השתבש. אנא התחל מחדש עם /start", cancellationToken);
            RemoveUserState(chatId);
        }

        public static async Task SendRegistrationSummary(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var registration = GetDriverRegistration(chatId);
            if (registration == null) return;

            var summaryText = GenerateRegistrationSummaryText(registration); // Moved summary generation to a helper method

            await SendRegistrationSummaryAsync(botClient, chatId, summaryText, cancellationToken);
            SetUserState(chatId, keywords.AwaitingConfirmationState); // Use constant for state
        }

        private static string GenerateRegistrationSummaryText(DriverDTO registration)
        {
            return $"סיכום הרשמה:\n" +
                   $"שם מלא: {registration.FullName}\n" +
                   $"פרטי רכב: {registration.CarDetails}\n" +
                   $"מספר טלפון: {registration.PhoneNumber}\n\n" +
                   "האם כל הפרטים נכונים?";
        }

        #region Handle Confirmation Block
        public static async Task HandleConfirmation(ITelegramBotClient botClient, long chatId, CallbackQuery callbackData, CancellationToken cancellationToken)
        {
            switch (callbackData.Data)
            {
                case "confirm_yes":
                    await ConfirmDriverRegistration(botClient, chatId, callbackData, cancellationToken);
                    break;

                case "confirm_no":
                    await RestartDriverRegistration(botClient, chatId, cancellationToken);
                    break;

                default:
                    await HandleInvalidConfirmation(botClient, chatId, cancellationToken);
                    break;
            }
        }

        private static async Task ConfirmDriverRegistration(ITelegramBotClient botClient, long chatId, CallbackQuery callbackData, CancellationToken cancellationToken)
        {
            var driverRegData = GetDriverRegistration(chatId);
            if (driverRegData == null) return;

            var driver = new DriverDTO
            {
                CarDetails = driverRegData.CarDetails,
                FullName = driverRegData.FullName,
                finishedReg = 1,
                PhoneNumber = driverRegData.PhoneNumber,
                UserName = callbackData.From.Username,
                DriverId = callbackData.From.Id.ToString(),
            };

            await driverRepository.InsertDriverAsync(driver);

            await SendTextMessageAsync(botClient, chatId, "תודה! הפרטים שלך נשמרו בהצלחה.", cancellationToken);
            await SendMainMenuAsync(botClient, chatId, cancellationToken);

            RemoveDriverRegistration(chatId);
            RemoveUserState(chatId);
        }

        private static async Task RestartDriverRegistration(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            await SendTextMessageAsync(botClient, chatId, "בסדר, בוא נתחיל מחדש. מה השם המלא שלך?", cancellationToken);

            AddOrUpdateDriverRegistration(chatId, new DriverDTO());
            SetUserState(chatId, keywords.AwaitingNameState); // Use constant for state
        }

        private static async Task HandleInvalidConfirmation(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            await SendTextMessageAsync(botClient, chatId, "Confirmation data not recognized. Please try again.", cancellationToken);
        }
        #endregion

        public static async Task SendMainMenuAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "ברוכים הבאים!:",
                replyMarkup: MenuMethods.DriverMainMenu(),
                cancellationToken: cancellationToken
            );
        }

        public static async Task SendStartOrdersMenuAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "נא לבחור אופציה:",
                replyMarkup: MenuMethods.StopReceivingOrdersMenu(),
                cancellationToken: cancellationToken
            );
        }

        public static async Task SendIntroMenuAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "פקודה לא קיימת, התחל מחדש",
                replyMarkup: MenuMethods.RegistrationMenu(),
                cancellationToken: cancellationToken
            );
        }

        public static async Task SendRegistrationMenuAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "בואו נתחיל מחדש:",
                replyMarkup: MenuMethods.RegistrationMenu(),
                cancellationToken: cancellationToken
            );
        }

        public static async Task SendRegistrationSummaryAsync(ITelegramBotClient botClient, long chatId, string summaryText, CancellationToken cancellationToken)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: summaryText,
                replyMarkup: MenuMethods.ConfirmYesNo(),
                cancellationToken: cancellationToken
            );
        }

        public static async Task SendTextMessageAsync(ITelegramBotClient botClient, long chatId, string text, CancellationToken cancellationToken, ParseMode? parseMode = ParseMode.Html)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: text,
                parseMode: parseMode,
                cancellationToken: cancellationToken
            );
        }

        public static async Task SendStopReceivingOrdersMenuAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "להפסקת קבלה של הזמנות יש לבחור אופציה מתאימה:",
                replyMarkup: MenuMethods.StopReceivingOrdersMenu(),
                cancellationToken: cancellationToken
            );
        }

        private static async Task HandleAwaitingNameInput(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken)
        {
            var driverRegistration = GetDriverRegistration(chatId);
            if (driverRegistration == null) return;

            driverRegistration.FullName = messageText;
            AddOrUpdateDriverRegistration(chatId, driverRegistration);

            await SendTextMessageAsync(botClient, chatId, "דגם וצבע הרכב? (כדי שהלקוח יזהה אותך, כן?)", cancellationToken);
            SetUserState(chatId, keywords.AwaitingCarDetailsState);
        }

        private static async Task HandleAwaitingCarDetailsInput(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken)
        {
            var driverRegistration = GetDriverRegistration(chatId);
            if (driverRegistration == null) return;

            driverRegistration.CarDetails = messageText;
            AddOrUpdateDriverRegistration(chatId, driverRegistration);

            await SendTextMessageAsync(botClient, chatId, "מה מספר הטלפון שלך?", cancellationToken);
            SetUserState(chatId, keywords.AwaitingPhoneNumberState);
        }

        private static async Task HandleAwaitingPhoneNumberInput(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken)
        {
            var driverRegistration = GetDriverRegistration(chatId);
            if (driverRegistration == null) return;

            driverRegistration.PhoneNumber = messageText;
            AddOrUpdateDriverRegistration(chatId, driverRegistration);

            await SendRegistrationSummary(botClient, chatId, cancellationToken);
        }
    }
}

