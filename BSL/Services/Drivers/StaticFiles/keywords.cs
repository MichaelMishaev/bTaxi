using System;
using System.Collections.Generic;
using System.Text;

namespace BL.Services.Drivers.StaticFiles
{
    public static class keywords
    {
        public const string AwaitingNameState = "awaiting_name";
        public const string AwaitingCarDetailsState = "awaiting_car_details";
        public const string AwaitingPhoneNumberState = "awaiting_phone_number";
        public const string AwaitingConfirmationState = "awaiting_confirmation";
        public const string AcceptBid = "accept_bid";
        public static List<string> knownLocations = new List<string>
                                                        {
                                                            "נתב\"ג",           // Common abbreviation with double quotes
                                                            "נתבג",            // Without double quotes
                                                            "נמל תעופה",       // Generic term for "airport"
                                                            "נמל תעופה בן גוריון",  // Full name
                                                            "בן גוריון",       // Commonly used name
                                                            "שדה תעופה",       // Another term for "airport"
                                                            "ש\"ת בן גוריון",   // Abbreviation for שדה תעופה
                                                            "שדה התעופה בן גוריון",  // Full name with "שדה התעופה"
                                                            "נתבג'",           // Common typo with a single quote
                                                            "שדה תעופה נתב\"ג", // Combination of both
                                                            "שדה נתב\"ג",       // Shortened form
                                                            "שדה בן גוריון",    // Shortened form
                                                            "שדה תעופה בן גוריון",  // Full form with "שדה"
                                                            "שדה תעופה נ\"ת בן גוריון", // Abbreviation for נמל תעופה
                                                            "נ\"ת בן גוריון",   // Short for נמל תעופה בן גוריון
                                                            "נמל ת\"א בן גוריון", // Abbreviation that includes ת"א
                                                            "נתב\"ג תל אביב",     // Full name with Tel Aviv
                                                            "נת\"ב",             // Another abbreviation
                                                            "נ\"ת בן גוריון",   // Abbreviation including נ\"ת
                                                            "נמל בן גוריון",     // Shortened term focusing on "נמל"
                                                            "שדה\"ת נתב\"ג",      // Common abbreviation for שדה תעופה נתב"ג
                                                            "בן גוריון נתב\"ג",   // Combination of both
                                                            "שדה\"ת בן גוריון",   // Abbreviation for שדה תעופה בן גוריון
                                                            "ת\"א נתב\"ג",        // Tel Aviv and נתב"ג combination
                                                            "נמל תעופה בן גוריון תל אביב", // Full name with Tel Aviv
                                                            "נמל תעופה הבינלאומי בן גוריון", // Full name including "הבינלאומי"
                                                            "שדה תעופה בינלאומי בן גוריון", // Including "בינלאומי"
                                                            "נמל התעופה הבינלאומי בן גוריון", // Full official name
                                                            "שדה תעופה תל אביב", // Short form focusing on Tel Aviv
                                                            "נמל תעופה ת\"א",   // Abbreviation with Tel Aviv
                                                            "נמל תעופה לוד",   // Historic reference when it was called Lod Airport
                                                            "נת\"ב\"ג",          // Creative variation with double quotes
                                                            "נמל תעופה נת\"ב",  // Shortened version with abbreviation
                                                            "שדה\"ת לוד",       // Historic reference combining Lod and airport
                                                            "תל אביב נתב\"ג",    // Another combination focusing on Tel Aviv
                                                            "נתב\"ג בן גוריון",  // Reversed order
                                                            "שדה בן גוריון ת\"א", // Combination of both
                                                            "נמל תעופה בן גוריון נ\"ת", // Abbreviation for נמל תעופה
                                                            "נמל תעופה תל אביב בן גוריון", // Full combination of all names
                                                            "נמל תעופה הבינלאומי בן-גוריון" // Official name with hyphen in בן-גוריון
                                                        };


    }
}
