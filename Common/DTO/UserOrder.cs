using Common.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace telegramB.Objects
{
    public class UserOrder
    {
        public int Id { get; set; }
        public AddressDTO FromAddress { get; set; }
        public AddressDTO ToAddress { get; set; }
        public int NumberOfPassengers { get; set; }
        public string PhoneNumber { get; set; }
        public string CurrentStep { get; set; }
        public string Remarks { get; set; }
        public long userId { get; set; }
        public decimal price { get; set; }
        public string PendingValue { get; set; }
        public AddressDTO PendingAddress { get; set; } // Add this property
        public long assignToDriver { get; set; }
        public decimal BidAmount { get; set; }
        public long CustomerChatId { get; set; }
        public long? ParentId { get; set; }
        public long BidId { get; set; }
    }
}
