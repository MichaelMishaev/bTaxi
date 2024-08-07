using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using telegramB.Menus;

namespace BL.Helpers
{
    public static class MainMenuService
    {
        public static async Task  DisplayMainMenu(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            //****************************************************************//
            var mainMenuButtons = MenuMethods.mainMenuButtons();

            //****************************************************************//

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Welcome! Choose an option:",
                replyMarkup: mainMenuButtons,
                cancellationToken: cancellationToken

            );
        }
    }
}
