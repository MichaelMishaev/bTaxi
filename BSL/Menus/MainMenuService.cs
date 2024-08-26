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
            string greetMessage = $"ברוכים הבאים תושבי אריאל והסביבה 🙌 \n" +
                                   "אנא בחרו אפשרות:";

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: greetMessage,
                replyMarkup: mainMenuButtons,
                cancellationToken: cancellationToken

            );
        }
    }
}
