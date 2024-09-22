using BL.Helpers.logger;
using Common.DTO;
using Common.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using telegramB.Menus;
using telegramB.Objects;

namespace BL.Helpers
{
    public class LocationService
    {
        private const string NOMINATIM_API = "https://nominatim.openstreetmap.org/search";
        private readonly HttpClient _httpClient;
        private readonly SessionManager _sessionManager;
        public LocationService(SessionManager sessionManager)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            // Initialize HttpClient with the custom handler
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "IsraelDistanceCalculator/1.0");

            _sessionManager = sessionManager;
        }

        public async Task<double> CalculateDistance(AddressDTO location1, AddressDTO location2, ITelegramBotClient botClient, CancellationToken cancellationToken, long chatId)
        {
            var coord1 = await GetCoordinates($"{location1.Street} {location1.StreetNumber} , {location1.City}");
            var coord2 = await GetCoordinates($"{location2.Street} {location2.StreetNumber} , {location2.City}");

            if (coord1 == null)
            {
                //await NotifyInvalidAddress(chatId, botClient, location1, "from", cancellationToken);
                ConsolePrintService.addressErrorPrint($"From address: {location1.Street}{location1.StreetNumber} , {location1.City}");
            }
            if (coord2 == null)
            {
                //await NotifyInvalidAddress(chatId, botClient, location2, "to", cancellationToken);
                ConsolePrintService.addressErrorPrint($"To address: {location2.Street}{location2.StreetNumber} , {location2.City}");
            }
            if (coord1 == null || coord2 == null)
            {
                string wrongAddresses = $"הכתובת {location1.Street}{location1.StreetNumber} או {location2.Street}{location2.StreetNumber} , {location2.City} לא נמצאה בשירות המפות, אין אפשרות לחשב מחיר ממוצע של מונית רגילה.";
                await NotifyInvalidAddress(chatId, botClient, wrongAddresses, "to", cancellationToken);
                return 0;
            }
            return await CalculateHaversineDistanceAsync(coord1.Value, coord2.Value);
        }


        private async Task NotifyInvalidAddress(long chatId, ITelegramBotClient botClient, string location, string addressType, CancellationToken cancellationToken)
        {
            //string address = $"{location.Street} {location.StreetNumber}, {location.City}";
            string formattedLocation = location.Replace(",", ",\n");
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: @$"{formattedLocation}",
           //     replyMarkup: MenuMethods.mainMenuButtons(),
                cancellationToken: cancellationToken
            );

            // Reset the session data and guide the user to start a new order
            await _sessionManager.SetSessionData<UserOrder>(chatId, "UserOrder", null); //************** Reset UserOrder session data
            await _sessionManager.SetSessionData<string>(chatId, "UserState", null); //************** Reset UserState session data
        }
        private async Task<(double Lat, double Lon)?> GetCoordinates(string location)
        {
         if (location.Contains("נמל תעופה בן גוריון")) { location = "נמל תעופה בן גוריון"; }
           var variations = new[]
            {
        $"{location}, Israel",
        location,
        $"{location}, West Bank",
        $"{location}, Palestine"
    };

            foreach (var variation in variations)
            {
                var encodedLocation = Uri.EscapeDataString(variation);
                var url = $"{NOMINATIM_API}?q={encodedLocation}&format=json&limit=1&countrycodes=IL,PS";

                try
                {
                    var response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync();
                    var results = JArray.Parse(content);

                    if (results.Count > 0)
                    {
                        var lat = results[0]["lat"].Value<double>();
                        var lon = results[0]["lon"].Value<double>();

                        //Console.WriteLine($@"Coordinates for {location}: Lat {lat}, Lon {lon}");
                        return (lat, lon);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error geocoding {variation}: {ex.Message}");
                }
            }

            // Try removing the first word from the location and retrying
            var words = location.Split(' ');
            for (int i = 1; i < words.Length; i++)
            {
                var newVariation = string.Join(' ', words.Skip(i));
                if (!string.IsNullOrEmpty(newVariation))
                {
                    var encodedNewVariation = Uri.EscapeDataString(newVariation);
                    var newUrl = $"{NOMINATIM_API}?q={encodedNewVariation}&format=json&limit=1&countrycodes=IL,PS";

                    try
                    {
                        var newResponse = await _httpClient.GetAsync(newUrl);
                        newResponse.EnsureSuccessStatusCode();

                        var newContent = await newResponse.Content.ReadAsStringAsync();
                        var newResults = JArray.Parse(newContent);

                        if (newResults.Count > 0)
                        {
                            //if address fucked up
                            if(validateNewAddress(newVariation) == null) return null;

                            var newLat = newResults[0]["lat"].Value<double>();
                            var newLon = newResults[0]["lon"].Value<double>();

                            Console.WriteLine($"Coordinates for {newVariation}: Lat {newLat}, Lon {newLon}");
                            return (newLat, newLon);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error geocoding {newVariation}: {ex.Message}");
                    }
                }
            }

            Console.WriteLine($"No results found for location: {location}");
            return null;
        }

        private static string  validateNewAddress(string location)
        {
            string result = location;
            if (string.IsNullOrEmpty(location) || location.Split(',').Length < 2)
            {
                Console.WriteLine($"No valid address found for location: {location}");
                // Return a specific indicator for invalid address
                return null;
            }
            return location;
        }

        private async Task<double> CalculateHaversineDistanceAsync((double lat, double lon) point1, (double lat, double lon) point2)
        {
            string apiKey = "5b3ce3597851110001cf6248005490c0100a412cb44798beba497609";
            string apiUrl = $"https://api.openrouteservice.org/v2/directions/driving-car?api_key={apiKey}&start={point1.lon},{point1.lat}&end={point2.lon},{point2.lat}";

            using (HttpClient client = new HttpClient())
            {
                // Send GET request
                HttpResponseMessage response = await client.GetAsync(apiUrl);

                // Ensure success status code
                response.EnsureSuccessStatusCode();

                // Read response content
                string responseBody = await response.Content.ReadAsStringAsync();

                // Parse the JSON response
                var data = JObject.Parse(responseBody);
                var routes = data["features"];

                // Check if routes are present
                if (routes == null || routes.Type == JTokenType.Null || !routes.HasValues)
                {
                    Console.WriteLine("No route found.");
                    Console.WriteLine("Response: " + responseBody);
                    return 0;
                }

                var distance = routes[0]["properties"]["segments"][0]["distance"].Value<double>();

                // Convert distance from meters to kilometers
                double distanceInKm = distance / 1000.0;

                Console.WriteLine($"Distance between points: {distanceInKm} km");

                return distanceInKm;
            }

        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}


