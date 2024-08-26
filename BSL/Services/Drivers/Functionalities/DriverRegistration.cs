using BL.Services.Drivers.StaticFiles;
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
        private const string DriverRegistrationKey = "DriverRegistrationData"; // Added constant for session key
        private const string DriverCurrentStateKey = "DriverCurrentState"; // Added constant for session key
        private const string AwaitingNameState = "awaiting_name"; // Added constant for state
        private  string RequestFullNameText = "מה השם המלא שלך?"; // Added constant for text message
        private readonly SessionManager _sessionManager;

        public DriverRegistration(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public async Task StartRegistration(ITelegramBotClient botClient, long chatId, int messageId, CancellationToken cancellationToken)
        {
            InitializeDriverRegistration(chatId);
            SetDriverUserState(chatId, keywords.AwaitingNameState);


            await _sessionManager.SetSessionData(chatId, "DriverUserState", keywords.AwaitingNameState);

            await botClient.SendTextMessageAsync(
                chatId: chatId,
               // messageId: messageId,
                text: "מה השם המלא שלך?",
                cancellationToken: cancellationToken
            );
        }

        private async void InitializeDriverRegistration(long chatId)
        {
            var driverRegistration = new DriverDTO();
            await _sessionManager.SetSessionData(chatId, "DriverRegistration", driverRegistration);
        }

        private async void SetDriverUserState(long chatId, string state)
        {
            await _sessionManager.SetSessionData(chatId, DriverCurrentStateKey, state);
        }
    }
}
