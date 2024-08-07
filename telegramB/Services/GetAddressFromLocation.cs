using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace telegramB.Services
{
    public class GetAddressFromLocation
    {
        public  async Task<string> GetAddressFromLocationAsync(double latitude, double longitude)
        {
            using (HttpClient client = new HttpClient())
            {
                string requestUri = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={latitude},{longitude}&key={Types.GeocodingApiKey}";
                HttpResponseMessage response = await client.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Geocoding API Response: {responseContent}"); // Log the response content for debugging

                    JObject jsonResponse = JObject.Parse(responseContent);
                    JArray results = (JArray)jsonResponse["results"];
                    if (results != null && results.Count > 0)
                    {
                        string address = results[0]["formatted_address"]?.ToString();
                        return address ?? "Address not found";
                    }
                    return "Address not found";
                }
                else
                {
                    return "Error retrieving address";
                }
            }
        }
    }
}
