using Common.DTO;
using DLL;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL
{
    public class DriverRepository
    {
        private readonly BaseDbContext _context;

        public DriverRepository()
        {
            _context = new BaseDbContext();
        }

        public async Task<long> GetDriverChatIdByOrderIdAsync(int orderId)
        {
            var query = "SELECT assignToDriver FROM Order WHERE Id = @OrderId";
            using (var connection = await _context.GetOpenConnectionAsync())
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@OrderId", orderId);

                    await connection.OpenAsync();
                    var result = await command.ExecuteScalarAsync();
                    if (result != null)
                    {
                        return Convert.ToInt64(result);
                    }
                    else
                    {
                        throw new Exception("Driver not found for the specified order.");
                    }
                }
            }
        }
            public async Task<bool> checkIfDriverExists(long driver)
        {
            string query = "SELECT COUNT(*) FROM `btrip`.`driver` WHERE `driverId` = @driverId AND status = 1";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@driverId", driver);

                        var result = await command.ExecuteScalarAsync();
                        return Convert.ToInt32(result) > 0;
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
        public async Task InsertDriverAsync(DriverDTO driver)
        {
           
            string query = @"
                            INSERT INTO `btrip`.`driver`
                            (`driverId`, `userName`, `fullName`, `phoneNumber`,  `finishedReg`,`CarDetails`)
                            VALUES
                            (@driverId, @userName, @fullName, @phoneNumber,  @finishedReg, @CarDetails)
                            ON DUPLICATE KEY UPDATE
                            `lastVisitDate` = CURRENT_TIMESTAMP;

                            INSERT INTO `btrip`.`driver_history`
                            (`userId`, `operation`)
                            VALUES
                            (@driverId, 1);";

            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@driverId", driver.DriverId);
                        command.Parameters.AddWithValue("@userName", driver.UserName ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@fullName", driver.FullName ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@phoneNumber", string.IsNullOrWhiteSpace(driver.PhoneNumber) ? (object)DBNull.Value : driver.PhoneNumber);
                        command.Parameters.AddWithValue("@finishedReg", driver.finishedReg);
                        command.Parameters.AddWithValue("@CarDetails", driver.CarDetails ?? (object)DBNull.Value);



                        await command.ExecuteNonQueryAsync();
                        Console.WriteLine("New driver inserted");
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

        public async Task<bool> SetDriverRecieveJobs(long driverId)
        {
            string qry = @$"SET SQL_SAFE_UPDATES = 0;
                            update `btrip`.`driver` SET isWorking = 1 
                            where driverId = {driverId};
                            SET SQL_SAFE_UPDATES = 1;";

                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(qry, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }
            
            return true;
        }

        public async Task<bool> SetDriverDeclineJobs(long driverId)
        {
            string qry = @$"SET SQL_SAFE_UPDATES = 0;
                            update `btrip`.`driver` SET isWorking = 0
                            where driverId = {driverId};
                            SET SQL_SAFE_UPDATES = 1;";

            using (var connection = await _context.GetOpenConnectionAsync())
            {
                using (var command = new MySqlCommand(qry, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }

            return true;
        }


        public async Task<bool> isApprovedDriver(long driverId)
        {
            bool isApproved = false;
            string qry = @$"SELECT isApproved FROM `btrip`.`driver` WHERE driverId = {driverId} AND status = 1";
            using (var connection = await _context.GetOpenConnectionAsync())
            {
                using (var command = new MySqlCommand(qry, connection))
                {
                    var result = await command.ExecuteScalarAsync();
                    if (result!=null)
                    {
                        isApproved = Convert.ToBoolean(result);
                    }
                }
            }
           
            return isApproved;
        }

        public async Task<bool> UpdateDriverStatusToInactive(long driverId)
        {
            bool isUpdated = false;
            string qry = @$"UPDATE `btrip`.`driver` SET status = 0 WHERE driverId = @driverId";

            using (var connection = await _context.GetOpenConnectionAsync())
            {
                using (var command = new MySqlCommand(qry, connection))
                {
                    // Add the driverId parameter to avoid SQL injection
                    command.Parameters.AddWithValue("@driverId", driverId);

                    try
                    {
                        // Execute the query and check if any rows were affected
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        isUpdated = rowsAffected > 0; // If rows were updated, return true
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating driver status: {ex.Message}");
                        throw;
                    }
                }
            }

            return isUpdated;
        }


        public async Task<bool> CheckUserStatusAsync(long userId)
        {

            bool isApproved = false;
            string qry = @"SELECT isApproved FROM `btrip`.`driver` WHERE driverId = @driverId";
            using (var connection = await _context.GetOpenConnectionAsync())
            {
                using (var command = new MySqlCommand(qry, connection))
                {
                    command.Parameters.AddWithValue("@driverId", userId);

                    var result = await command.ExecuteScalarAsync();
                    if (result != null)
                    {
                        isApproved = Convert.ToBoolean(result);
                    }
                }
            }
            return isApproved;
        }

        public async Task<List<DriverDTO>> GetWorkingDriversAsync()
        {
            var workingDrivers = new List<DriverDTO>();
            string query = "SELECT * FROM `btrip`.`driver` WHERE `isWorking` = 1 AND status=1";

            using (var connection = await _context.GetOpenConnectionAsync())
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var driver = new DriverDTO
                            {
                                DriverId = reader["driverId"].ToString(),
                                UserName = reader["userName"].ToString(),
                                FullName = reader["fullName"].ToString(),
                                PhoneNumber = reader["phoneNumber"].ToString(),
                                CarDetails = reader["carDetails"].ToString(),
                            };
                            workingDrivers.Add(driver);
                        }
                    }
                }
            }
            return workingDrivers;
        }

        public async Task<DriverDTO> GetDriverDetailsById(long driverId)
        {
            string query = "SELECT * FROM `btrip`.`driver` WHERE driverId = @driverId";
            try
            {
                using (var connection = await _context.GetOpenConnectionAsync())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@driverId", driverId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new DriverDTO
                                {
                                    DriverId = reader["driverId"].ToString(),
                                    UserName = reader["userName"].ToString(),
                                    FullName = reader["fullName"].ToString(),
                                    PhoneNumber = reader["phoneNumber"].ToString(),
                                    CarDetails = reader["CarDetails"].ToString(),
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




    }
}
