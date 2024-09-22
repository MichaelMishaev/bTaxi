using Common.DTO;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace telegramB.Services
{
    public class GetAddressFromLocationService
    {
        public async Task<AddressDTO> GetAddressFromLocationAsync(double latitude, double longitude)
        {
            string url = $"https://nominatim.openstreetmap.org/reverse?lat={latitude}&lon={longitude}&format=json&addressdetails=1";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");  // Required by Nominatim

                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(jsonResponse);

                    // Nominatim uses a different structure than Google Maps. Let's extract the components
                    var address = json["address"] as JObject;
                    if (address != null)
                    {
                        // Extracting city
                        string city = address["city"]?.ToString() ??
                                      address["town"]?.ToString() ??
                                      address["village"]?.ToString() ??
                                      address["county"]?.ToString() ??
                                      "Unknown City";

                        // Extracting street
                        string street = address["road"]?.ToString() ?? "Unknown Street";

                        // Extracting street number
                        int streetNumber = int.TryParse(address["house_number"]?.ToString(), out int number) ? number : 0;

                        // Returning the formatted AddressDTO object
                        return new AddressDTO
                        {
                            City = city,
                            Street = street,
                            StreetNumber = streetNumber
                        };
                    }
                }
                return null;
            }
        }

    }
}
