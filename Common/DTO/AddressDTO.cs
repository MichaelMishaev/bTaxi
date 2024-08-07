using System;
using System.Collections.Generic;
using System.Text;

namespace Common.DTO
{
    public class AddressDTO
    {
        public string City { get; set; }
        public string Street { get; set; }
        public int StreetNumber { get; set; }

        public string GetFormattedAddress()
        {
            return $"{Street} {StreetNumber}, {City}";
        }
    }
}
