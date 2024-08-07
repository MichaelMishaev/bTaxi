using System;
using System.Collections.Generic;
using System.Text;

namespace Common.DTO
{
    public class BidDetailsDTO
    {
        public long CustomerId { get; set; }
        public long DriverId { get; set; }
        public string CustomerPhoneNumber { get; set; }
        public decimal DriverBid { get; set; }
    }
}
