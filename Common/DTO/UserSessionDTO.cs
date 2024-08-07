using System;
using System.Collections.Generic;
using System.Text;
using telegramB.Objects;

namespace Common.DTO
{
    public class UserSessionDTO
    {
            public string CurrentStep { get; set; }
            public decimal? BidAmount { get; set; }
            public long? BidId { get; set; }
            public UserOrder Order { get; set; }
    }
}
