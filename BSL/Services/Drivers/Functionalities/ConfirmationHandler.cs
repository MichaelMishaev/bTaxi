using BL.Services.Drivers.StaticFiles;
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
using telegramB;
using telegramB.Services;

namespace BL.Services.Drivers.Functionalities
{
    public class ConfirmationHandler
    {
        DriverRepository driverRepository = new DriverRepository();
        private readonly SessionManager _sessionManager;
        public ConfirmationHandler(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }
        public async Task HandleConfirmation(ITelegramBotClient botClient, long chatId, CallbackQuery callbackData, CancellationToken cancellationToken)
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

        // Refactored helper methods
        private async Task ConfirmDriverRegistration(ITelegramBotClient botClient, long chatId, CallbackQuery callbackData, CancellationToken cancellationToken)
        {
            var driverRegData = await _sessionManager.GetSessionData<DriverDTO>(chatId, "DriverRegistration");
            if (driverRegData == null) return;

            var driver = MapToDriverDTO(driverRegData, callbackData);
            await SaveDriverData(driver);

            await BotDriversResponseService.SendTextMessageAsync(botClient, chatId, "תודה! הפרטים שלך נשמרו בהצלחה.", cancellationToken);
            await BotDriversResponseService.SendTextMessageAsync(botClient, chatId, "יש להמתין לאימות הפרטים 🗽 ,\nהתהליך יקח עד 24 שעות ⌛️.\nאבל כמובן שננסה כמה שיותר מהר, הודעה תישלח בסיום.", cancellationToken);
            await TypesManual.botGudenko.SendTextMessageAsync(
                               chatId: "-1002194149620",
                               text: $"נהג חדש נרשם {DateTime.Now}",
                               cancellationToken: cancellationToken
                            );
            //await BotDriversResponseService.SendMainMenuAsync(botClient, chatId, cancellationToken);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("====================================================");
            Console.WriteLine($"Waiting driver DB Confirmation,  driver {chatId}");
            Console.WriteLine("====================================================");
            Console.ResetColor();

            ClearDriverSessionData(chatId);
        }

        private async Task RestartDriverRegistration(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            // Logic to restart the driver registration
            var driverRegistration = new DriverDTO();
            await _sessionManager.SetSessionData(chatId, "DriverRegistration", driverRegistration);
            await _sessionManager.SetSessionData(chatId, "DriverUserState", keywords.AwaitingNameState);

            await BotDriversResponseService.SendTextMessageAsync(botClient, chatId, "בסדר, בוא נתחיל מחדש. מה השם המלא שלך?", cancellationToken);
        }

        // Implementing the missing HandleInvalidConfirmation method
        private async Task HandleInvalidConfirmation(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            // Logic to handle invalid confirmation scenarios
            await BotDriversResponseService.SendTextMessageAsync(botClient, chatId, "Confirmation data not recognized. Please try again.", cancellationToken);
        }

        private DriverDTO MapToDriverDTO(DriverDTO driverRegData, CallbackQuery callbackData)
        {
            return new DriverDTO
            {
                CarDetails = driverRegData.CarDetails,
                FullName = driverRegData.FullName,
                finishedReg = 1,
                PhoneNumber = driverRegData.PhoneNumber,
                UserName = callbackData.From.Username,
                DriverId = callbackData.From.Id.ToString(),
            };
        }

        private async Task SaveDriverData(DriverDTO driver)
        {
            await driverRepository.InsertDriverAsync(driver);
        }

        private async void ClearDriverSessionData(long chatId)
        {
            await _sessionManager.RemoveSessionData(chatId, "DriverRegistration");
            await _sessionManager.RemoveSessionData(chatId, "DriverUserState");
        }
    }
}
