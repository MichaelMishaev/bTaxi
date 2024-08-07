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
using telegramB.Services;

namespace BL.Services.Drivers.Functionalities
{
    public class ConfirmationHandler
    {
        DriverRepository driverRepository = new DriverRepository();
        public async Task HandleConfirmation(ITelegramBotClient botClient, long chatId, CallbackQuery callbackData, CancellationToken cancellationToken)
        {
            if (callbackData.Data == "confirm_yes")
            {
                var driverRegistration = SessionManager.GetSessionData<DriverDTO>(chatId, "DriverRegistration"); //************ Comments
                DriverDTO driver = new DriverDTO
                {
                    CarDetails = driverRegistration.CarDetails,
                    FullName = driverRegistration.FullName,
                    finishedReg = 1,
                    PhoneNumber = driverRegistration.PhoneNumber,
                    UserName = callbackData.From.Username,
                    DriverId = callbackData.From.Id.ToString(),
                };

                await driverRepository.InsertDriverAsync(driver);

                await BotDriversResponseService.SendTextMessageAsync(botClient, chatId, "הפרטים נשמרו בהצלחה, אנו נעדכן בקרוב מאד אם הפרטים אומתו", cancellationToken);
                SessionManager.SetSessionData(callbackData.From.Id, "UserChatId", chatId); //************ Comments


                SessionManager.RemoveSessionData(chatId, "DriverRegistration"); //************ Comments
                SessionManager.RemoveSessionData(chatId, "DriverUserState"); //************ Comments
            }
            else if (callbackData.Data == "confirm_no")
            {
                await BotDriversResponseService.SendTextMessageAsync(botClient, chatId, "בסדר, בוא נתחיל מחדש. מה השם המלא שלך?", cancellationToken);

                var driverRegistration = new DriverDTO(); //************ Comments
                SessionManager.SetSessionData(chatId, "DriverRegistration", driverRegistration); //************ Comments
                var driverState = "awaiting_name"; //************ Comments
                SessionManager.SetSessionData(chatId, "DriverUserState", driverState); //************ Comments
            }
        }
    }
}
