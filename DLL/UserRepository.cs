using Common.DTO;
using DLL;
using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;

namespace DAL
{
    public class UserRepository
    {
        private readonly BaseDbContext _context;

        public UserRepository()
        {
            _context = new BaseDbContext();
        }

        public async Task InsertUserAsync(UserDTO user)
        {
            string query = @"
                             INSERT INTO `btrip`.`user`
                                        (`userName`, `firstName`, `lastName`, `phoneNumber`, `updateDate`, `isPremium`, `isBot`, `userId`, `lastVisitDate`, `isDriver`)
                                        VALUES
                                        (@userName, @firstName, @lastName, @phoneNumber, CURRENT_TIMESTAMP, @isPremium, @isBot, @userId, CURRENT_TIMESTAMP, @isDriver)
                                        ON DUPLICATE KEY UPDATE
                                        `lastVisitDate` = CURRENT_TIMESTAMP,
                                        `isDriver` = @isDriver;

                                        INSERT INTO `btrip`.`user_history`
                                        (`userId`, `operation`)
                                        VALUES
                                        (@userId, 1);";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userName", user.UserName ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@firstName", user.FirstName ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@lastName", string.IsNullOrWhiteSpace(user.LastName) ? (object)DBNull.Value : user.LastName);
                        command.Parameters.AddWithValue("@phoneNumber", string.IsNullOrWhiteSpace(user.PhoneNumber) ? (object)DBNull.Value : user.PhoneNumber);
                        command.Parameters.AddWithValue("@isPremium", user.IsPremium );
                        command.Parameters.AddWithValue("@isBot", user.IsBot);
                        command.Parameters.AddWithValue("@userId", user.UserId);
                        command.Parameters.AddWithValue("@isDriver", user.IsDriver == 0? 0 : user.IsDriver);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"MySQL error: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateUserPhoneNomberAsync(long userChatId, string phoneNumber)
        {
            string query = @$"UPDATE btrip.user SET phoneNumber = @phoneNumber where userId = @userChatId  AND phoneNumber IS NULL";
            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@phoneNumber", phoneNumber);
                        command.Parameters.AddWithValue("@userChatId", userChatId);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"error in: UpdateUserPhoneNomberAsync at:  {DateTime.Now}");
                Console.WriteLine($"query is: UPDATE btrip.user SET phoneNumber = {phoneNumber} where userId = {userChatId}  AND phoneNumber IS NULL" );
                Console.WriteLine($"MySQL error: {ex.Message}");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error in: UpdateUserPhoneNomberAsync");
                Console.WriteLine($"General error: {ex.Message}");
                
            }
        }
    }
}
