using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace telegramB.ErrorHandle
{
    public class HandleError
    {
        private static readonly string logFilePath = "bot_errors.log";
        private const int MaxRetryAttempts = 5;
        private const int DelayBetweenRetriesInSeconds = 5;

        public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            try
            {
                var errorMessage = exception switch
                {
                    ApiRequestException apiRequestException
                        => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                    _ => exception.ToString()
                };

                Console.WriteLine(errorMessage);

                // Log the error to a file or monitoring system if needed
                LogError(errorMessage);

                // Attempt to handle specific exceptions or general exceptions here
                if (exception is ApiRequestException apiEx && apiEx.ErrorCode == 429) // Too Many Requests
                {
                    Console.WriteLine("Too many requests. Waiting before retrying...");
                    await Task.Delay(DelayBetweenRetriesInSeconds * 1000, cancellationToken);
                }
                else if (exception.InnerException != null &&
                         exception.InnerException.Message.Contains("Unable to read data from the transport connection"))
                {
                    Console.WriteLine("Connection lost. Attempting to reconnect...");
                    await ReconnectAsync(botClient, cancellationToken);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("An unexpected error occurred. Continuing operation...");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while handling exception: {ex}");
                LogError($"Error while handling exception: {ex}");
            }
        }

        private void LogError(string errorMessage)
        {
            try
            {
                File.AppendAllText(logFilePath, $"{DateTime.Now}: {Environment.NewLine}{errorMessage}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                // Ignore logging errors to prevent infinite loops
                Console.WriteLine($"Failed to log error: {ex}");
            }
        }

        private async Task ReconnectAsync(ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                try
                {
                    await Task.Delay(DelayBetweenRetriesInSeconds * 1000, cancellationToken);
                    var me = await botClient.GetMeAsync(cancellationToken);
                    Console.WriteLine($"Reconnected successfully as {me.Username}.");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Reconnect attempt {attempt} failed: {ex.Message}");
                    LogError($"Reconnect attempt {attempt} failed: {ex.Message}");

                    if (attempt == MaxRetryAttempts)
                    {
                        Console.WriteLine("Max reconnect attempts reached. Giving up.");
                        LogError("Max reconnect attempts reached. Giving up.");
                    }
                }
            }
        }
    }
}
