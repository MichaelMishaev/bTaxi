using Common.DTO;
using Common.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using telegramB.Objects;

namespace BL.Helpers
{
    public class LocationService
    {
        private const string NOMINATIM_API = "https://nominatim.openstreetmap.org/search";
        private readonly HttpClient _httpClient;

        public LocationService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "IsraelDistanceCalculator/1.0");
        }

        public async Task<double> CalculateDistance(AddressDTO location1, AddressDTO location2, ITelegramBotClient botClient, CancellationToken cancellationToken, long chatId)
        {
            var coord1 = await GetCoordinates($"{location1.Street} {location1.StreetNumber} , {location1.City}");
            var coord2 = await GetCoordinates($"{location2.Street} {location2.StreetNumber} , {location2.City}");

            if (coord1 == null)
            {
                await NotifyInvalidAddress(chatId, botClient, location1, "from", cancellationToken);
                return -1;
            }
            if (coord2 == null)
            {
                await NotifyInvalidAddress(chatId, botClient, location2, "to", cancellationToken);
                return -1;
            }

            return await CalculateHaversineDistanceAsync(coord1.Value, coord2.Value);
        }


        private async Task NotifyInvalidAddress(long chatId, ITelegramBotClient botClient, AddressDTO location, string addressType, CancellationToken cancellationToken)
        {
            string address = $"{location.Street} {location.StreetNumber}, {location.City}";
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"הכתובת {address} שגויה. אנא הזן שוב את הכתובת.",
                cancellationToken: cancellationToken
            );

            // Restart the address input process
            var userOrder = SessionManager.GetSessionData<UserOrder>(chatId, "UserOrder");
            userOrder.CurrentStep = addressType == "from" ? "enter_city" : "enter_to_city"; //************** Set the current step based on address type
            SessionManager.SetSessionData(chatId, "UserOrder", userOrder);

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "הכנס עיר:",
                cancellationToken: cancellationToken
            );
        }
        private async Task<(double Lat, double Lon)?> GetCoordinates(string location)
            {
                // Try different variations of the address
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

                            Console.WriteLine($"Coordinates for {location}: Lat {lat}, Lon {lon}");
                            return (lat, lon);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error geocoding {variation}: {ex.Message}");
                    }
                }

                Console.WriteLine($"No results found for location: {location}");
                return null;
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


