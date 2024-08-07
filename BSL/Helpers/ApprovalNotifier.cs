using DAL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Telegram.Bot;
using telegramB.Services;

namespace BL.Helpers
{
    public class ApprovalNotifier
    {
        private readonly ITelegramBotClient _botClient;
        private readonly Dictionary<string, long> _userChatIds;
        private readonly Timer _timer;
        private DriverRepository _driverRepository = new DriverRepository();
        private CancellationToken _cancellationToken;


        public ApprovalNotifier(ITelegramBotClient botClient, Dictionary<string, long> userChatIds, CancellationToken cancellationToken)
        {
            _botClient = botClient;
            _userChatIds = userChatIds;
            _timer = new Timer(CheckApprovalStatus, null, TimeSpan.Zero, TimeSpan.FromMinutes(2));
            _cancellationToken = cancellationToken;
        }

        private async void CheckApprovalStatus(object state)
        {
            foreach (var userId in _userChatIds.Keys)
            {
                bool isApproved = await _driverRepository.CheckUserStatusAsync(userId);
                if (isApproved)
                {
                    long chatId = _userChatIds[userId];
                    await _botClient.SendTextMessageAsync(chatId, "ההרשמה עברה בהצלחה, הינך יכול להתחיל לקבל הזמנות");
                    await BotDriversResponseService.SendStartOrdersMenuAsync(_botClient, chatId, _cancellationToken);
                    _userChatIds.Remove(userId); // Remove from list to avoid repeated notifications
                }
            }
        }

        public static void SaveUserChatIds(Dictionary<string, long> userChatIds, string filePath)
        {
            var json = JsonConvert.SerializeObject(userChatIds);
            File.WriteAllText(filePath, json);
        }

    }

}
