using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BL.Services
{
   public class PlaceAutoCompleteService
    {
        private readonly string _placesApiKey;

        public PlaceAutoCompleteService(string placesApiKey)
        {
            _placesApiKey = placesApiKey;
        }


        public async Task<List<string>> GetAutocompleteSuggestions(string input)
        {
            using var client = new HttpClient();
            string requestUri = $"https://maps.googleapis.com/maps/api/place/autocomplete/json?input={Uri.EscapeDataString(input)}&key={_placesApiKey}&components=country:il"; // Assuming you want to limit to Israel
           
            HttpResponseMessage response = await client.GetAsync(requestUri);

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                JObject jsonResponse = JObject.Parse(responseContent);
                JArray predictions = (JArray)jsonResponse["predictions"];
                List<string> suggestions = new List<string>();

                foreach (var prediction in predictions)
                {
                    suggestions.Add(prediction["description"].ToString());
                }

                return suggestions;
            }
            else
            {
                return new List<string>();
            }
        }
    }
}
