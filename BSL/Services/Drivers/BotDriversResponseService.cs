using Common.DTO;
using DAL;
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
        private static Dictionary<long, DriverDTO> driverRegistrations = new Dictionary<long, DriverDTO>();
        private static Dictionary<long, string> userStates = new Dictionary<long, string>();
        static DriverRepository driverRepository = new DriverRepository();

        public static async Task StartRegistration(ITelegramBotClient botClient, long chatId, int messageId, CancellationToken cancellationToken)
        {
            driverRegistrations[chatId] = new DriverDTO();
            userStates[chatId] = "awaiting_name";

            await botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: "מה השם המלא שלך?",
                cancellationToken: cancellationToken
            );
        }

        public static async Task HandleUserInput(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken)
        {
            switch (userStates[chatId])
            {
                case "awaiting_name":
                    driverRegistrations[chatId].FullName = messageText;
                    await BotDriversResponseService.SendTextMessageAsync(botClient, chatId, "דגם וצבע הרכב? (כדי שהלקוח יזהה אותך, כן?)", cancellationToken);
                    userStates[chatId] = "awaiting_car_details";
                    break;

                case "awaiting_car_details":
                    driverRegistrations[chatId].CarDetails = messageText;
                    await BotDriversResponseService.SendTextMessageAsync(botClient, chatId, "מה מספר הטלפון שלך?", cancellationToken);
                    userStates[chatId] = "awaiting_phone_number";
                    break;

                case "awaiting_phone_number":
                    driverRegistrations[chatId].PhoneNumber = messageText;
                    await SendRegistrationSummary(botClient, chatId, cancellationToken);
                    break;

                default:
                    await BotDriversResponseService.SendTextMessageAsync(botClient, chatId, "משהו השתבש. אנא התחל מחדש עם /start", cancellationToken);
                    userStates.Remove(chatId);
                    break;
            }
        }

        public static async Task SendRegistrationSummary(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var registration = driverRegistrations[chatId];
            var summaryText = $"סיכום הרשמה:\n" +
                              $"שם מלא: {registration.FullName}\n" +
                              $"פרטי רכב: {registration.CarDetails}\n" +
                              $"מספר טלפון: {registration.PhoneNumber}\n\n" +
                              "האם כל הפרטים נכונים?";

            await BotDriversResponseService.SendRegistrationSummaryAsync(botClient, chatId, summaryText, cancellationToken);

            userStates[chatId] = "awaiting_confirmation";
        }

        public static async Task HandleConfirmation(ITelegramBotClient botClient, long chatId, CallbackQuery callbackData, CancellationToken cancellationToken)
        {
            if (callbackData.Data == "confirm_yes")
            {
                var DriverRegData = driverRegistrations[chatId];
                DriverDTO driver = new DriverDTO
                {
                    CarDetails = DriverRegData.CarDetails,
                    FullName = DriverRegData.FullName,
                    finishedReg = 1,
                    PhoneNumber = DriverRegData.PhoneNumber,
                    UserName = callbackData.From.Username,
                    DriverId = callbackData.From.Id.ToString(),
                };

                await driverRepository.InsertDriverAsync(driver);

                await BotDriversResponseService.SendTextMessageAsync(botClient, chatId, "תודה! הפרטים שלך נשמרו בהצלחה.", cancellationToken);
                await BotDriversResponseService.SendMainMenuAsync(botClient, chatId, cancellationToken);

                driverRegistrations.Remove(chatId);
                userStates.Remove(chatId);
            }
            else if (callbackData.Data == "confirm_no")
            {
                await BotDriversResponseService.SendTextMessageAsync(botClient, chatId, "בסדר, בוא נתחיל מחדש. מה השם המלא שלך?", cancellationToken);

                driverRegistrations[chatId] = new DriverDTO();
                userStates[chatId] = "awaiting_name";
            }
        }

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
                text: "להתחיל לקבל הזמנות?",
                replyMarkup: MenuMethods.StartGetOrdersMenu(),
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

        public static async Task SendTextMessageAsync(ITelegramBotClient botClient, long chatId, string text, CancellationToken cancellationToken, ParseMode? parseMode=ParseMode.Html)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: text,
                parseMode: parseMode ,
                cancellationToken: cancellationToken
            );
        }

        public static async Task SendStopReceivingOrdersMenuAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Do you want to stop receiving orders?",
                replyMarkup: MenuMethods.StopReceivingOrdersMenu(),
                cancellationToken: cancellationToken
            );
        }

    }
}
