using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace BL.Helpers.MessageSending
{
    public class SendMessage
    {
        public async Task<bool> SafeSendMessageAsync(ITelegramBotClient botClient, long chatId, string text, CancellationToken cancellationToken)
        {
            int retryCount = 3;
            bool res = true;
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
                    res = false;
                    break; // Do not retry if the bot is blocked
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException ex)
                {
                    Console.WriteLine($"Telegram API error: {ex.Message}");
                    res = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"General error: {ex.Message}");
                    res = false;
                }
            }
            return res;
        }

        public async Task SafeSendMessageWithReplyMarkupAsync(ITelegramBotClient botClient, long chatId, string text, IReplyMarkup replyMarkup, CancellationToken cancellationToken)
        {
            try
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: text,
                    replyMarkup: replyMarkup,
                    cancellationToken: cancellationToken
                );
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("bot was blocked by the user"))
            {
                Console.WriteLine($"Bot was blocked by user {chatId}. No further messages will be sent.");
                // You can optionally log or flag the user in your system.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
                // Handle any other exceptions that may occur.
            }
        }

    }
}
