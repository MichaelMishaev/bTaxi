using Common.DTO;
using Common.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;

namespace BL.Services.Drivers.Functionalities
{
    public  class DriverRegistration
    {
        public async Task StartRegistration(ITelegramBotClient botClient, long chatId, int messageId, CancellationToken cancellationToken)
        {
            var driverRegistration = new DriverDTO(); //************ Comments
            SessionManager.SetSessionData(chatId, "DriverRegistration", driverRegistration); //************ Comments
            SessionManager.SetSessionData(chatId, "DriverUserState", "awaiting_name"); //************ Comments

            await botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: "מה השם המלא שלך?",
                cancellationToken: cancellationToken
            );
        }
    }
}
