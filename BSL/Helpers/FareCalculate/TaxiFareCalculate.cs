using BL.Helpers.FareCalculate;
using System;
using System.Collections.Generic;
using System.Text;

namespace BL.Helpers
{
    public enum FareType
    {
        A,
        B,
        C
    }

    public class TaxiFareCalculate
    {
        private readonly FareStructure _fareStructure;

        public TaxiFareCalculate(FareStructure fareStructure)
        {
            _fareStructure = fareStructure;
        }

        public FareType DetermineFareType(DateTime rideTime)
        {
            var dayOfWeek = rideTime.DayOfWeek;
            var timeOfDay = rideTime.TimeOfDay;

            if (dayOfWeek >= DayOfWeek.Sunday && dayOfWeek <= DayOfWeek.Wednesday)
            {
                if (timeOfDay >= new TimeSpan(6, 0, 0) && timeOfDay <= new TimeSpan(21, 0, 0))
                {
                    return FareType.A;
                }
                else
                {
                    return FareType.B;
                }
            }
            else if (dayOfWeek == DayOfWeek.Thursday)
            {
                if (timeOfDay >= new TimeSpan(6, 0, 0) && timeOfDay <= new TimeSpan(21, 0, 0))
                {
                    return FareType.A;
                }
                else if (timeOfDay >= new TimeSpan(21, 1, 0) && timeOfDay <= new TimeSpan(23, 0, 0))
                {
                    return FareType.B;
                }
                else
                {
                    return FareType.C;
                }
            }
            else if (dayOfWeek == DayOfWeek.Friday || IsEveOfHoliday(rideTime))
            {
                if (timeOfDay >= new TimeSpan(6, 0, 0) && timeOfDay <= new TimeSpan(16, 0, 0))
                {
                    return FareType.A;
                }
                else if (timeOfDay >= new TimeSpan(16, 1, 0) && timeOfDay <= new TimeSpan(21, 0, 0))
                {
                    return FareType.B;
                }
                else
                {
                    return FareType.C;
                }
            }
            else if (dayOfWeek == DayOfWeek.Saturday || IsHoliday(rideTime))
            {
                if (timeOfDay >= new TimeSpan(6, 0, 0) && timeOfDay <= new TimeSpan(19, 0, 0))
                {
                    return FareType.B;
                }
                else
                {
                    return FareType.C;
                }
            }

            throw new ArgumentException("Invalid date or time for fare determination");
        }

        public double CalculateFare(FareType fareType, double distanceKm, double rideDurationMinutes)
        {
            double farePerMinute = 0, farePerKm = 0;
            rideDurationMinutes = distanceKm;
            switch (fareType)
            {
                case FareType.A:
                    farePerMinute = _fareStructure.FarePerMinuteA;
                    farePerKm = _fareStructure.FarePerKmA;
                    break;
                case FareType.B:
                    farePerMinute = _fareStructure.FarePerMinuteB;
                    farePerKm = _fareStructure.FarePerKmB;
                    break;
                case FareType.C:
                    farePerMinute = _fareStructure.FarePerMinuteC;
                    farePerKm = _fareStructure.FarePerKmC;
                    break;
            }

            double totalFare = _fareStructure.BookingFee + _fareStructure.InitialMeter +
                               (farePerMinute * rideDurationMinutes) +
                               (farePerKm * distanceKm);

            return totalFare;
        }

        private bool IsEveOfHoliday(DateTime date)
        {
            // Implement logic to determine if the given date is the eve of a holiday
            return false;
        }

        private bool IsHoliday(DateTime date)
        {
            // Implement logic to determine if the given date is a holiday
            return false;
        }
    }
}
