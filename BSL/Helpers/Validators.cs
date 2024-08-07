using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BL.Helpers
{
    public static class Validators
    {
        private static readonly Regex CellularRegex = new Regex(@"^05[0-9]{8}$", RegexOptions.Compiled);
        private static readonly Regex StationaryRegex = new Regex(@"^0[2-4,8-9][0-9]{7}$", RegexOptions.Compiled);
        public static async Task<bool> PhoneValidator(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return false;
            }

            // Check if the phone number matches either the cellular or stationary pattern
            return CellularRegex.IsMatch(phoneNumber) || StationaryRegex.IsMatch(phoneNumber);
        }
    }
    
}
