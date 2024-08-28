using BL.Helpers.logger;
using DAL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Telegram.Bot;
using telegramB.Services;

namespace BL.Helpers
{
    public class ApprovalNotifier
    {
        private readonly ITelegramBotClient _botClient;
        private Dictionary<string, long> _userChatIds;
        private readonly Timer _timer;
        private DriverRepository _driverRepository = new DriverRepository();
        private CancellationToken _cancellationToken;
        private const string UserChatIdsFilePath = "userChatIds.json";

        public ApprovalNotifier(ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            _botClient = botClient;

            _timer = new Timer(CheckApprovalStatus, null, TimeSpan.Zero, TimeSpan.FromMinutes(2));
            _cancellationToken = cancellationToken;
        }

        public async void CheckApprovalStatus(object state)
        {
            _userChatIds = LoadUserChatIds(UserChatIdsFilePath);
            foreach (var userId in _userChatIds.Values)
            {
                bool isApproved = await _driverRepository.CheckUserStatusAsync(userId);
                if (isApproved)
                {
                    ConsolePrintService.CheckPointMessage($"Driver {userId} been approved, message sent");

                    await _botClient.SendTextMessageAsync(userId, "ההרשמה עברה בהצלחה, הינך יכול להתחיל לקבל הזמנות");
                    await BotDriversResponseService.SendStartOrdersMenuAsync(_botClient, userId, _cancellationToken);
                    DeleteUserChatId(userId, UserChatIdsFilePath);//delete from file
                    
                }
            }
        }

        public static void SaveUserChatIds(Dictionary<string, long> userChatIds, string filePath)
        {
            var json = JsonConvert.SerializeObject(userChatIds);
            File.WriteAllText(filePath, json);
        }

        private Dictionary<string, long> LoadUserChatIds(string filePath)
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<Dictionary<string, long>>(json);
            }
            return new Dictionary<string, long>();
        }

        public void DeleteUserChatId(long userId, string filePath)
        {
            var userChatIds = LoadUserChatIds(filePath);

            var keyToRemove = userChatIds.FirstOrDefault(x => x.Value == userId).Value.ToString();
            if (!string.IsNullOrEmpty(keyToRemove))
            {
                userChatIds.Remove(keyToRemove);  // Remove the entry from the dictionary
                SaveUserChatIds(userChatIds, filePath);  // Save the updated dictionary back to the file
                Console.WriteLine("Removed pending user from approval list");
            }
            else
            {
                Console.WriteLine($"User ID '{userId}' not found.");
            }
        }
    }

}
