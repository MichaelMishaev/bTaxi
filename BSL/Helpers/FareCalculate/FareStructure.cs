using System;
using System.Collections.Generic;
using System.Text;

namespace BL.Helpers.FareCalculate
{
    public class FareStructure
    {
        public double BookingFee { get; set; }
        public double InitialMeter { get; set; }
        public double FarePerMinuteA { get; set; }
        public double FarePerKmA { get; set; }
        public double FarePerMinuteB { get; set; }
        public double FarePerKmB { get; set; }
        public double FarePerMinuteC { get; set; }
        public double FarePerKmC { get; set; }
    }
}
