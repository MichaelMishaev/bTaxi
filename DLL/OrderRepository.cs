using Common.DTO;
using DLL;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using telegramB.Objects;

namespace DAL
{
    public class OrderRepository
    {
        private readonly BaseDbContext _context;

        public OrderRepository()
        {
            _context = new BaseDbContext();
        }

        public async Task<int> PlaceOrderAsync(UserOrder order, long bidId)
        {
            string query = @"
        INSERT INTO `btrip`.`order`
        (`userId`, `from`, `to`, `price`, `phoneNumber`, `remarks`, `date`, `bidId`, `currentStep`)
        VALUES
        (@userId, @from, @to, @price, @phoneNumber, @remarks, CURRENT_TIMESTAMP, @bidId, @currentStep);
        SELECT LAST_INSERT_ID();";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", order.userId);
                        command.Parameters.AddWithValue("@from", order.FromAddress ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@to", order.ToAddress ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@price", order.price);
                        command.Parameters.AddWithValue("@phoneNumber", order.PhoneNumber ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@remarks", order.Remarks ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@bidId", bidId);
                        command.Parameters.AddWithValue("@currentStep", order.CurrentStep ?? (object)DBNull.Value);

                        var result = await command.ExecuteScalarAsync();
                        return Convert.ToInt32(result); // Return the newly inserted orderId
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


        public async Task<bool> UpdateCustomerBidAsync(int orderId, long customerId, decimal bidAmount)
        {
            string query = @"
        INSERT INTO `btrip`.`bids`
        (`orderId`, `customerId`, `customerBid`, `updateDate`)
        VALUES
        (@orderId, @customerId, @customerBid, CURRENT_TIMESTAMP)
        ON DUPLICATE KEY UPDATE
        `customerBid` = VALUES(`customerBid`),
        `updateDate` = CURRENT_TIMESTAMP;";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@orderId", orderId);
                        command.Parameters.AddWithValue("@customerId", customerId);
                        command.Parameters.AddWithValue("@customerBid", bidAmount);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
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


        public async Task<bool> UpdateDriverBidAsync(int orderId, long driverId, decimal bidAmount)
        {
            string query = @"
                INSERT INTO `btrip`.`bids`
                (`orderId`, `driverId`, `driverBid`, `updateDate`)
                VALUES
                (@orderId, @driverId, @driverBid, CURRENT_TIMESTAMP)
                ON DUPLICATE KEY UPDATE
                `driverBid` = VALUES(`driverBid`),
                `updateDate` = CURRENT_TIMESTAMP;";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@orderId", orderId);
                        command.Parameters.AddWithValue("@driverId", driverId);
                        command.Parameters.AddWithValue("@driverBid", bidAmount);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
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

        public async Task UpdateOrderStepAsync(long userId, string step)
        {
            string query = "UPDATE `btrip`.`order` SET `currentStep` = @step WHERE `userId` = @userId";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@step", step);

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



        public async Task<List<UserOrder>> GetAvailableOrdersAsync()
        {
            string query = @"
                    SELECT `id`, `from_city`, `from_street`, `from_number`, `to_city`, `to_street`, `to_number`, `price`, `remarks`
                    FROM `btrip`.`order`
                    WHERE `isClosed` IS NULL";

            List<UserOrder> availableOrders = new List<UserOrder>();

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var fromAddress = new AddressDTO
                                {
                                    City = reader.GetString(1),
                                    Street = reader.GetString(2),
                                    StreetNumber = reader.GetInt32(3)
                                };

                                var toAddress = new AddressDTO
                                {
                                    City = reader.GetString(4),
                                    Street = reader.GetString(5),
                                    StreetNumber = reader.GetInt32(6)
                                };

                                var order = new UserOrder
                                {
                                    Id = reader.GetInt32(0),
                                    FromAddress = fromAddress,
                                    ToAddress = toAddress,
                                    price = reader.GetDecimal(7),
                                    Remarks = reader.GetString(8)
                                };
                                availableOrders.Add(order);
                            }
                        }
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

            return availableOrders;
        }

        public async Task<long> GetCustomerIdByOrderId(int orderId)
        {
            string query = "SELECT userId FROM `btrip`.`order` WHERE id = @orderId";
            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@orderId", orderId);
                        var result = await command.ExecuteScalarAsync();
                        return Convert.ToInt64(result);
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


        public async Task<UserOrder> GetOrderByIdAsync(long orderId)
        {
            string query = @"
            SELECT `id`, `userId`, `from_city`, `from_street`, `from_number`, 
                   `to_city`, `to_street`, `to_number`, `price`, 
                   `phoneNumber`, `remarks`, `date`, `assignToDriver`, 
                   `assignDatetime`, `isClosed`, `userId` AS `CustomerChatId`
            FROM `btrip`.`order`
            WHERE `id` = @orderId;
            ";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@orderId", orderId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new UserOrder
                                {
                                    Id = reader.GetInt32("id"),
                                    userId = reader.GetInt64("userId"),
                                    FromAddress = new AddressDTO
                                    {
                                        City = reader["from_city"] != DBNull.Value ? reader["from_city"].ToString() : string.Empty,
                                        Street = reader["from_street"] != DBNull.Value ? reader["from_street"].ToString() : string.Empty,
                                        StreetNumber = reader["from_number"] != DBNull.Value ? reader.GetInt32("from_number") : 0
                                    },
                                    ToAddress = new AddressDTO
                                    {
                                        City = reader["to_city"] != DBNull.Value ? reader["to_city"].ToString() : string.Empty,
                                        Street = reader["to_street"] != DBNull.Value ? reader["to_street"].ToString() : string.Empty,
                                        StreetNumber = reader["to_number"] != DBNull.Value ? reader.GetInt32("to_number") : 0
                                    },
                                    price = reader.GetDecimal("price"),
                                    PhoneNumber = reader["phoneNumber"] != DBNull.Value ? reader["phoneNumber"].ToString() : string.Empty,
                                    Remarks = reader["remarks"] != DBNull.Value ? reader["remarks"].ToString() : string.Empty,
                                    assignToDriver = reader["assignToDriver"] != DBNull.Value ? reader.GetInt64("assignToDriver") : 0,
                                    CustomerChatId = reader.GetInt64("CustomerChatId")
                                };
                            }
                        }
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

            return null;
        }


        public async Task<long> InsertBidAsync(long parentId, long chatId, long driverId, long customerId, decimal bidAmount, bool isDriver)
        {
            long bidId = 0;
            string query = string.Empty;
             query = "INSERT INTO `bids` (`parentId`, `chatId`, `driverId`, `customerId`, `driverBid`, `customerBid`, `isDriver`) " +
                           "VALUES (@parentId, @chatId, @driverId, @customerId, @driverBid, @customerBid, @isDriver); SELECT LAST_INSERT_ID();";


            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@parentId", parentId);
                        command.Parameters.AddWithValue("@chatId", chatId);
                        command.Parameters.AddWithValue("@driverId",isDriver==true? driverId: (object)DBNull.Value);
                        command.Parameters.AddWithValue("@customerId", customerId);
                        command.Parameters.AddWithValue("@driverBid", isDriver ? bidAmount : (object)DBNull.Value);
                        command.Parameters.AddWithValue("@customerBid", !isDriver ? bidAmount : (object)DBNull.Value);
                        command.Parameters.AddWithValue("@isDriver", isDriver);

                        bidId = Convert.ToInt64(await command.ExecuteScalarAsync());
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

            return bidId;
        }


        public async Task UpdateBidParentIdAsync(long bidId, long parentId)
        {
            string query = "UPDATE `bids` SET `parentId` = @parentId WHERE `id` = @bidId;";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@parentId", parentId);
                        command.Parameters.AddWithValue("@bidId", bidId);

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



        public async Task<long> InsertCustomerFirstBidAsync(long chatId, long customerId, decimal customerBid)
        {
            string query = @"
        INSERT INTO `bids` (`chatId`, `customerId`, `customerBid`, `isDriver`, `updateDate`)
        VALUES (@chatId, @customerId, @customerBid, 0, CURRENT_TIMESTAMP);
        SELECT LAST_INSERT_ID();
    ";

            using (var connection = await _context.GetOpenConnectionAsync())
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@chatId", chatId);
                    command.Parameters.AddWithValue("@customerId", customerId);
                    command.Parameters.AddWithValue("@customerBid", customerBid);

                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt64(result);
                }
            }
        }



        public async Task CloseOrderAsync(int orderId)
        {
            var query = "UPDATE `order` SET `isClosed` = 1, `closeDate` = @closeDate WHERE `id` = @orderId";
            using (var connection = await _context.GetOpenConnectionAsync())
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@orderId", orderId);
                    command.Parameters.AddWithValue("@closeDate", DateTime.Now);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }


        public async Task<bool> AssignOrderToDriverAsync(int orderId, long driverId)
        {
            string updateQuery = "UPDATE `btrip`.`order` SET assignToDriver = @driverId, assignDatetime = @assignDatetime WHERE id = @orderId";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@orderId", orderId);
                        command.Parameters.AddWithValue("@driverId", driverId);
                        command.Parameters.AddWithValue("@assignDatetime", DateTime.Now);

                        await command.ExecuteNonQueryAsync();
                        return true;
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


        public async Task<bool> InsertDriverRatingAsync(int orderId, int rating)
        {
            var query = "INSERT INTO orders_rating (orderId, driver_rating) VALUES (@OrderId, @Rating) ON DUPLICATE KEY UPDATE driver_rating = @Rating, ratingDate = NOW()";
            using (var connection = await _context.GetOpenConnectionAsync())
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@OrderId", orderId);
                    command.Parameters.AddWithValue("@Rating", rating);

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        public async Task<long?> GetCustomerChatIdByBidChatIdAsync(long bidChatId)
        {
            string query = @"
            SELECT customerId 
            FROM bids 
            WHERE chatId = @chatId 
            LIMIT 1";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@chatId", bidChatId);

                        var result = await command.ExecuteScalarAsync();
                        return result == DBNull.Value ? (long?)null : Convert.ToInt64(result);
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


        public async Task<long?> GetParentBidIdAsync(long chatId, long customerId)
        {
            string query = @"
        SELECT `parentId`
        FROM `bids`
        WHERE `chatId` = @chatId AND `customerId` = @customerId AND `isTaken` = 0
        ORDER BY `updateDate` DESC
        LIMIT 1;
    ";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@chatId", chatId);
                        command.Parameters.AddWithValue("@customerId", customerId);

                        var result = await command.ExecuteScalarAsync();

                        if (result != DBNull.Value && result != null)
                        {
                            return Convert.ToInt64(result);
                        }
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

            return null;
        }



        public async Task UpdateBidStatusAsync(long orderId, long driverId, bool isTaken)
        {
            string query = @"
                        UPDATE `bids`
                        SET `isTaken` = @isTaken, `assignToDriver` = @driverId, `updateDate` = CURRENT_TIMESTAMP
                        WHERE `id` = @orderId;";

            using (var connection = await _context.GetOpenConnectionAsync())
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@orderId", orderId);
                    command.Parameters.AddWithValue("@driverId", driverId);
                    command.Parameters.AddWithValue("@isTaken", isTaken);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }


        public async Task InsertDriverBidAsync(long chatId, long driverId, decimal driverBid)
        {
            string query = @"
                            UPDATE `bids`
                            SET `driverId` = @driverId, `driverBid` = @driverBid, `isTaken` = 0, `updateDate` = CURRENT_TIMESTAMP
                            WHERE `chatId` = @chatId;";

            using (var connection = await _context.GetOpenConnectionAsync())
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@chatId", chatId);
                    command.Parameters.AddWithValue("@driverId", driverId);
                    command.Parameters.AddWithValue("@driverBid", driverBid);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }


        public async Task<long> InsertCustomerBidAsync(long chatId, long customerId, decimal bidAmount)
        {
            string query = @"
        INSERT INTO `btrip`.`bids` (`chatId`, `customerId`, `customerBid`, `isDriver`)
        VALUES (@chatId, @customerId, @customerBid, 0);
        SELECT LAST_INSERT_ID();";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@chatId", chatId);
                        command.Parameters.AddWithValue("@customerId", customerId);
                        command.Parameters.AddWithValue("@customerBid", bidAmount);

                        var result = await command.ExecuteScalarAsync();
                        return Convert.ToInt64(result);
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




        public async Task InsertRatingAsync(int orderId, int rating)
        {
            var query = "INSERT INTO `OrderRatings` (`orderId`, `rating`) VALUES (@orderId, @rating)";
            using (var connection = await _context.GetOpenConnectionAsync())
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@orderId", orderId);
                    command.Parameters.AddWithValue("@rating", rating);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<int?> GetRatingAsync(int orderId)
        {
            var query = "SELECT `rating` FROM `OrderRatings` WHERE `orderId` = @orderId";
            using (var connection = await _context.GetOpenConnectionAsync())
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@orderId", orderId);
                    var result = await command.ExecuteScalarAsync();
                    return result == DBNull.Value ? (int?)null : (int?)result;
                }
            }
        }

        public async Task<bool> SaveDriverRatingAsync(int orderId, int rating)
        {
            // Logic to save the rating in the database
            // Assuming you have a 'Ratings' column in your 'Orders' table
            var query = @"
            INSERT INTO orders_rating (orderId, driver_rating, ratingDate)
            VALUES (@OrderId, @Rating, @RatingDate)
            ON DUPLICATE KEY UPDATE
                driver_rating = @Rating,
                ratingDate = @RatingDate";
            using (var connection = await _context.GetOpenConnectionAsync())
            {
                using (var command = new MySqlCommand(query, connection))
                {

                    command.Parameters.AddWithValue("@OrderId", orderId);
                    command.Parameters.AddWithValue("@Rating", rating);
                    command.Parameters.AddWithValue("@RatingDate", DateTime.Now);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    // Return true if one or more rows were updated, otherwise false
                    return rowsAffected > 0;
                }
            }
        }

        public async Task<bool> CheckOrderAssignedAsync(int orderId)
        {
            string checkQuery = "SELECT assignToDriver FROM `btrip`.`order` WHERE id = @orderId";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(checkQuery, connection))
                    {
                        command.Parameters.AddWithValue("@orderId", orderId);
                        var result = await command.ExecuteScalarAsync();

                        return result != DBNull.Value && result != null;
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


        public async Task MarkBidAsTakenAsync(long bidChatId)
        {
            string query = @"
            UPDATE bids
            SET isTaken = 1
            WHERE chatId = @chatId";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@chatId", bidChatId);
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

        public async Task<BidDetailsDTO> GetBidDetailsAsync(long bidChatId)
        {
            string query = @"
            SELECT customerId, driverId, customerPhoneNumber
            FROM bids
            WHERE chatId = @chatId
            LIMIT 1";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@chatId", bidChatId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new BidDetailsDTO
                                {
                                    CustomerId = reader.GetInt64("customerId"),
                                    DriverId = reader.GetInt64("driverId"),
                                    CustomerPhoneNumber = reader.GetString("customerPhoneNumber")
                                };
                            }
                        }
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
            return null;
        }

        public async Task<int> CreateOrderAsync(BidDetailsDTO bidDetails)
        {
            string query = @"
            INSERT INTO orders (customerId, driverId, price, status)
            VALUES (@customerId, @driverId, @price, 'confirmed');
            SELECT LAST_INSERT_ID();";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@customerId", bidDetails.CustomerId);
                        command.Parameters.AddWithValue("@driverId", bidDetails.DriverId);
                        command.Parameters.AddWithValue("@price", bidDetails.DriverBid);

                        var result = await command.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
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

    }
}
