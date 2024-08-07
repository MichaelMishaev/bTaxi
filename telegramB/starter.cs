﻿using BL.Helpers;
using BL.Services.Drivers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using telegramB.ErrorHandle;

namespace telegramB
{
    public class starter
    {
        private const string UserChatIdsFilePath = "userChatIds.json"; // Path to save the file
        private Timer _saveTimer;
        HandleUserUpdateService _handleUpdate;
        HandleDriverUpdateService _handleDriverService;
        HandleError _handleError;
        Dictionary<string, long> _userChatIds;

        public starter()
        {
        }

        public starter(HandleUserUpdateService handleUpdate, HandleError handleError)
        {
            _handleUpdate = handleUpdate;
            _handleError = handleError;
            _userChatIds = LoadUserChatIds(UserChatIdsFilePath); // Load userChatIds from file
            _handleDriverService = new HandleDriverUpdateService(_userChatIds);

            // Set up a timer to save userChatIds every 5 minutes
            _saveTimer = new Timer(SaveUserChatIds, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(20));
        }

        public async Task StartAsync()
        {
            TypesManual.botClient = new TelegramBotClient(TypesManual.BotToken);
            TypesManual.botDriver = new TelegramBotClient(TypesManual.DriverBotToken);

            using var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { } // receive all update types
            };

            TypesManual.botClient.StartReceiving(
                _handleUpdate.HandleUpdateAsync,
                _handleError.HandleErrorAsync,
                receiverOptions,
                cancellationToken: cts.Token
            );

            TypesManual.botDriver.StartReceiving(
                _handleDriverService.HandleDriverUpdateAsync,
                _handleError.HandleErrorAsync,
                receiverOptions,
                cancellationToken: cts.Token
            );

            var userBot = await TypesManual.botClient.GetMeAsync();
            var driverBot = await TypesManual.botDriver.GetMeAsync();
            Console.WriteLine($"Bot @{userBot.Username} is running...");
            Console.WriteLine($"Bot @{driverBot.Username} is running...");

            // Initialize the ApprovalNotifier
            var approvalNotifier = new ApprovalNotifier(TypesManual.botDriver, _userChatIds, cts.Token);

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

            SaveUserChatIds(null); // Save userChatIds to file before exiting
            cts.Cancel();
        }

        private void SaveUserChatIds(object state)
        {
            if (_userChatIds.Count > 0)
            {
                var json = JsonConvert.SerializeObject(_userChatIds);
                System.IO.File.WriteAllText(UserChatIdsFilePath, json);
            }
        }

        private Dictionary<string, long> LoadUserChatIds(string filePath)
        {
            if (System.IO.File.Exists(filePath))
            {
                var json = System.IO.File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<Dictionary<string, long>>(json);
            }
            return new Dictionary<string, long>();
        }
    }
}
