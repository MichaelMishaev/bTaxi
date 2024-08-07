using System;
using System.Collections.Generic;
using System.Text;

namespace Common.DTO
{
   public class UserDTO
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public int IsPremium { get; set; }
        public int IsBot { get; set; }
        public DateTime UpdateDate { get; set; }
        public DateTime? LastVisitDate { get; set; }
        public int IsDriver { get; set; }
    }
}
