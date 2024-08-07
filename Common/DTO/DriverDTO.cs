using System;
using System.Collections.Generic;
using System.Text;

namespace Common.DTO
{
    public class DriverDTO 
    {
        public string DriverId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public int IsPremium { get; set; }
        public int IsBot { get; set; }
        public DateTime UpdateDate { get; set; }
        public DateTime? LastVisitDate { get; set; }
        public int finishedReg { get; set; }
        public int IsWorking { get; set; }
        public string CarDetails { get; set; }
    }
}
