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
            using (HttpClient client = new HttpClient())
            {
                string requestUri = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={latitude},{longitude}&key={TypesManual.GeocodingApiKey}";
                HttpResponseMessage response = await client.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    JObject jsonResponse = JObject.Parse(responseContent);
                    JArray results = (JArray)jsonResponse["results"];
                    if (results != null && results.Count > 0)
                    {
                        var addressComponents = results[0]["address_components"] as JArray;
                        if (addressComponents != null)
                        {
                            string city = addressComponents
                                .FirstOrDefault(c => ((JArray)c["types"]).Any(t => t.ToString() == "locality" || t.ToString() == "administrative_area_level_1" || t.ToString() == "administrative_area_level_2"))?["long_name"]?.ToString() ?? "Unknown City";

                            string street = addressComponents
                                .FirstOrDefault(c => ((JArray)c["types"]).Any(t => t.ToString() == "route"))?["long_name"]?.ToString() ?? "Unknown Street";

                            int streetNumber = int.TryParse(addressComponents
                                .FirstOrDefault(c => ((JArray)c["types"]).Any(t => t.ToString() == "street_number"))?["long_name"]?.ToString(), out int number) ? number : 0;

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
                else
                {
                    return null;
                }
            }
        }

    }
}
