using Common.DTO;
using DLL;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;
using MySqlX.XDevAPI.Common;
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

        public async Task UpdateBidsSentMessageAsync(long orderId, string driverId)
        {
            string disableSafeUpdatesQuery = "SET SQL_SAFE_UPDATES = 0;";
            string updateBidsQuery = @"
                                    UPDATE btrip.bids AS b
                                    JOIN (
                                        SELECT o.bidId
                                        FROM `btrip`.`order` AS o
                                        WHERE o.id = @orderId
                                    ) AS subquery
                                    ON b.parentId = subquery.bidId
                                    SET b.isMessageSent = 1
                                    WHERE b.driverId = @driverId
                                      AND b.isMessageSent = 0;";
            string enableSafeUpdatesQuery = "SET SQL_SAFE_UPDATES = 1;";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        // Disable safe updates
                        using (var disableCommand = new MySqlCommand(disableSafeUpdatesQuery, connection, transaction))
                        {
                            await disableCommand.ExecuteNonQueryAsync();
                        }

                        // Perform the update
                        using (var updateCommand = new MySqlCommand(updateBidsQuery, connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@orderId", orderId);
                            updateCommand.Parameters.AddWithValue("@driverId", driverId);

                            await updateCommand.ExecuteNonQueryAsync();
                        }

                        // Re-enable safe updates
                        using (var enableCommand = new MySqlCommand(enableSafeUpdatesQuery, connection, transaction))
                        {
                            await enableCommand.ExecuteNonQueryAsync();
                        }

                        // Commit the transaction
                        await transaction.CommitAsync();
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


        public async Task<List<(string driverId, int id)>> GetDriverIdsForMessageAsync(long orderId)
        {
            string query = @"
                        SELECT DISTINCT driverId, id
                        FROM bids
                        WHERE parentId = (SELECT bidId
                                          FROM `btrip`.`order`
                                          WHERE id = @orderId)
                          AND driverId IS NOT NULL
                          AND isMessageSent = 0;";

            var driverData = new List<(string driverId, int id)>();

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@orderId", orderId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string driverId = reader.GetInt64("driverId").ToString();
                                int id = reader.GetInt32("id");
                                driverData.Add((driverId, id));
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

            return driverData;
        }




        public async Task UpdateOrderWithNewBidAsync(UserOrder userOrder)
        {
            string userOrderDetails =
                            $"OrderId: {userOrder.OrderId}, " +
                            $"FromAddress: {userOrder.FromAddress}, " +
                            $"ToAddress: {userOrder.ToAddress}, " +
                            $"BidAmount: {userOrder.BidAmount}, " +
                            $"CurrentStep: {userOrder.CurrentStep}, " +
                            $"Remarks: {userOrder.Remarks} " ;
            Console.WriteLine($"run UpdateOrderWithNewBidAsync, param From: {userOrder.FromAddress.GetFormattedAddress()} To {userOrder.ToAddress.GetFormattedAddress()}" +
                $" Order: {userOrder.OrderId}, {DateTime.Now}");
            string updateOrderQuery = @"
                        UPDATE `btrip`.`order`
                        SET  `CurrentStep` = @CurrentStep
                        WHERE `id` = @OrderId;";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(updateOrderQuery, connection))
                    {
                        command.Parameters.AddWithValue("@BidAmount", userOrder.BidAmount);
                        command.Parameters.AddWithValue("@CurrentStep", userOrder.CurrentStep);
                        command.Parameters.AddWithValue("@OrderId", userOrder.OrderId);

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


        public async Task<UserOrder> GetOrderByBidIdAsync(long bidId)
        {
            string query = @"
                            SELECT o.*
                            FROM `btrip`.`order` o
                            INNER JOIN `btrip`.`bids` b ON o.BidId = b.parentId
                            WHERE o.BidId = (
                                SELECT parentId 
                                FROM `btrip`.`bids` 
                                WHERE Id = @bidId
                            )
                            LIMIT 1;";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        // Adding bidId as a parameter to the query
                        command.Parameters.AddWithValue("@bidId", bidId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                // Return the populated Order object
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
                                    CustomerChatId = reader.GetInt64("userId")
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

            // Return null if no order is found
            return null;
        }



        public async Task<int> PlaceOrderAsync(UserOrder order, long bidId)
        {
            Console.WriteLine("run PlaceOrderAsync ");
            string query = @"
                            INSERT INTO `btrip`.`order`
                            (`userId`, `from_city`, `from_street`, `from_number`, `to_city`, `to_street`, `to_number`, `price`, `phoneNumber`, `remarks`, `date`, `bidId`, `currentStep`)
                            VALUES
                            (@userId, @from_city, @from_street, @from_number, @to_city, @to_street, @to_number, @price, @phoneNumber, @remarks, CURRENT_TIMESTAMP, @bidId, @currentStep);
                            SELECT LAST_INSERT_ID();";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", order.userId);
                        command.Parameters.AddWithValue("@from_city", order.FromAddress.City ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@from_street", order.FromAddress.Street ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@from_number", order.FromAddress.StreetNumber);
                        command.Parameters.AddWithValue("@to_city", order.ToAddress.City ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@to_street", order.ToAddress.Street ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@to_number", order.ToAddress.StreetNumber);
                        command.Parameters.AddWithValue("@price", order.price);
                        command.Parameters.AddWithValue("@phoneNumber", order.PhoneNumber ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@remarks", order.Remarks ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@bidId", bidId);
                        command.Parameters.AddWithValue("@currentStep", order.CurrentStep ?? (object)DBNull.Value);

                        var result = await command.ExecuteScalarAsync();

                        Console.WriteLine("----------------------------------");
                        Console.WriteLine($"Order created: {Convert.ToInt32(result)}");
                        Console.WriteLine("-----------------------------------");


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
            Console.WriteLine($"UpdateOrderStepAsync, userId {userId}");
            string query = " SET SQL_SAFE_UPDATES = 0;" +
                " UPDATE `btrip`.`order` SET `currentStep` = @step WHERE `userId` = @userId " +
                " SET SQL_SAFE_UPDATES = 1;";

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
                //Console.WriteLine("-----------------------------------------------------");
                //Console.WriteLine($"query {query}");
                //Console.WriteLine($"MySQL error: {ex.Message}");
                //Console.WriteLine("-----------------------------------------------------");

            }
            catch (Exception ex)
            {
                Console.WriteLine("-----------------------------------------------------");
                Console.WriteLine($"General error: {ex.Message}");
                Console.WriteLine("-----------------------------------------------------");
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
            Console.WriteLine($"run InsertBidAsync customerId {customerId} , {DateTime.Now}");
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
                        command.Parameters.AddWithValue("@driverId", isDriver == true ? driverId : (object)DBNull.Value);
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
            Console.WriteLine($"Rnn UpdateBidParentIdAsync {DateTime.Now}");
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
            Console.WriteLine($"CloseOrderAsync, Order:  {orderId} closed");
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
            Console.WriteLine($"AssignOrderToDriverAsync orderId: {orderId}");
            string updateQuery = "UPDATE `btrip`.`order` SET assignToDriver = @driverId, assignDatetime = @assignDatetime WHERE id = @orderId";
            string closeBidsForOrder = "UPDATE `bids` SET isTaken = 1 where id = (select bidId from `btrip`.`order` where id = @orderId)"; 

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
                       // return true;
                    }

                    using (var command2 = new MySqlCommand(closeBidsForOrder, connection))
                    {
                        command2.Parameters.AddWithValue("@orderId", orderId);

                        await command2.ExecuteNonQueryAsync();
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

        public async Task<(long? driverId, decimal driverBid)> GetDriverIdByBidIdAsync(int  bidId)
        {
            string query = @"
            SELECT driverId , driverBid
            FROM bids 
            WHERE Id = @bidId 
                   AND driverId IS NOT NULL
            LIMIT 1";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@bidId", bidId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                long? driverId = reader.IsDBNull(0) ? (long?)null : reader.GetInt64(0);
                                decimal driverBid = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);

                                return (driverId, driverBid); // Tuple with driverId and driverBid
                            }
                            else
                            {
                                return (null, 0); // Return default values if no data is found
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"MySQL error: {ex.Message}");
                return (null, 0); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}");
                return (null, 0); 


            }
        }


        public async Task<(long? UserId, long BidId)> GetCustomerChatIdAndBidIdByOrderIdAsync(long bidChatId)
        {
            string query = @"
                                        SELECT userId, bidId 
                                        FROM btrip.order 
                                        WHERE id = @orderId 
                                        LIMIT 1";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@orderId", bidChatId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                long? userId = reader.IsDBNull(0) ? (long?)null : reader.GetInt64(0);
                                long bidId = reader.GetInt64(1);

                                return (userId, bidId); // Return a tuple with userId and bidId
                            }
                            else
                            {
                                return (null, 0); // Return null values if no record is found
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
            Console.WriteLine("run InsertDriverBidAsync");
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


        public async Task<long> InsertAndThenUpdateCustomerBidAsync(long chatId, long customerId, decimal bidAmount)
        {
            Console.WriteLine("run InsertAndThenUpdateCustomerBidAsync");
            long newBidId;

            // First step: Perform the INSERT operation
            string insertQuery = @"
                        INSERT INTO `btrip`.`bids` (`chatId`, `customerId`, `customerBid`, `isDriver`)
                        VALUES (@chatId, @customerId, @customerBid, 0);
                        SELECT LAST_INSERT_ID();"; // This comment is outside the SQL query string

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@chatId", chatId);
                        command.Parameters.AddWithValue("@customerId", customerId);
                        command.Parameters.AddWithValue("@customerBid", bidAmount);

                        // Execute the INSERT and get the new ID
                        var result = await command.ExecuteScalarAsync();
                        newBidId = Convert.ToInt64(result);
                    }

                    // Commit the INSERT operation
                    await connection.CloseAsync();
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"MySQL error during INSERT: {ex.Message}");
                throw;
            }

            // Second step: Perform the UPDATE operation
            string updateQuery = @"
                    UPDATE `btrip`.`bids`
                    SET `parentId` = @parentId
                    WHERE `id` = @id;";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@parentId", newBidId);
                        command.Parameters.AddWithValue("@id", newBidId);

                        // Execute the UPDATE operation
                        await command.ExecuteNonQueryAsync();
                    }

                    // Commit the UPDATE operation
                    await connection.CloseAsync();
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"MySQL error during UPDATE: {ex.Message}");
                throw;
            }

            return newBidId;
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
