using System;
using System.Collections.Generic;
using System.Text;

namespace telegramB.Objects
{
    public class UserOrder
    {
            public string FromAddress { get; set; }
            public string ToAddress { get; set; }
            public int NumberOfPassengers { get; set; }
            public string PhoneNumber { get; set; }
            public string CurrentStep { get; set; }
            public string Remarks { get; set; }
    }
}
