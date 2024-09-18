using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace BL.Helpers
{
    public static class Validators
    {
        private static readonly Regex CellularRegex = new Regex(@"^05[0-9]{8}$", RegexOptions.Compiled);
        private static readonly Regex StationaryRegex = new Regex(@"^0[2-4,8-9][0-9]{7}$", RegexOptions.Compiled);
        public static async Task<bool> PhoneValidator(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return false;
            }

            // Check if the phone number matches either the cellular or stationary pattern
            return CellularRegex.IsMatch(phoneNumber) || StationaryRegex.IsMatch(phoneNumber);
        }


        public static async Task<bool> DeleteMessage(ITelegramBotClient bot, long chatId, int MessageId, CancellationToken cancellationToken)
        {
            bool isDeleted = false;

            try
            {
                await bot.DeleteMessageAsync(chatId, MessageId, cancellationToken);
                isDeleted = true;

            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("message to delete not found") || ex.Message.Contains("chat not found"))
            {
                // Log the exception or handle it as needed
                Console.WriteLine($"Message or chat not found. Unable to delete message {MessageId} in chat {chatId}, date: {DateTime.Now}.");
            }


            return isDeleted;
        }
    }
    
}
