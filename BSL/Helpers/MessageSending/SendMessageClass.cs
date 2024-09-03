using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Telegram.Bot;

namespace BL.Helpers.MessageSending
{
    public class SendMessage
    {
        public async Task SafeSendMessageAsync(ITelegramBotClient botClient, long chatId, string text, CancellationToken cancellationToken)
        {
            int retryCount = 3;
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    await botClient.SendTextMessageAsync(chatId, text, cancellationToken: cancellationToken);
                    break; // If successful, exit the loop
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("bot was blocked by the user"))
                {
                    Console.WriteLine($"Bot was blocked by user {chatId}. No further messages will be sent to this user.");
                    break; // Do not retry if the bot is blocked
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException ex)
                {
                    Console.WriteLine($"Telegram API error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"General error: {ex.Message}");
                }
            }
        }

    }
}
