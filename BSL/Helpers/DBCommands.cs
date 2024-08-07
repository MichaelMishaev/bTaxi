using Common.DTO;
using DAL;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace BL.Helpers
{
    public class DBCommands
    {
        UserRepository userRepository = null;
        public DBCommands()
        {
             userRepository = new UserRepository();
        }

        public async Task<bool>  SaveDriverAsUserIfNotExists(CallbackQuery? callbackQuery)
        {
            bool isPRemium = callbackQuery.From.IsPremium ?? false;
            UserDTO userDTO = new UserDTO
            {
                FirstName = string.IsNullOrWhiteSpace(callbackQuery.From.FirstName) ? "unknown" : callbackQuery.From.FirstName,
                UserId = callbackQuery.From.Id.ToString(),
                IsBot = callbackQuery.From.IsBot ? 1 : 0,
                IsPremium = isPRemium ? 1 : 0,
                LastName = string.IsNullOrWhiteSpace(callbackQuery.From.LastName) ? "unknown" : callbackQuery.From.LastName,
                UserName = string.IsNullOrWhiteSpace(callbackQuery.From.Username) ? "unknown" : callbackQuery.From.Username,
                PhoneNumber = "RBD",
                IsDriver = 1
            };
            await userRepository.InsertUserAsync(userDTO);
            return true;
        }

    }
}
