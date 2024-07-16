using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using WorldCompanyDataViewer.Models;

namespace WorldCompanyDataViewer.Services
{
    public interface IPostcodeLocationService
    {
        string GetUrl();
        Task<string> RequestClosestPlace(string postcode);
        Task RequestPostcodeLocationsAsync(List<PostcodeGeodataEntry> postcodes);
    }

    public class OnlinePostcodeLocationService : IPostcodeLocationService
    {
        const string path = "https://api.postcodes.io/postcodes?filter=postcode,longitude,latitude";
        const string pathLoc = "https://api.postcodes.io/postcodes/";
        public string GetUrl()
        {
            return path;
        }
        //TODO reorganize error handling
        public async Task RequestPostcodeLocationsAsync(List<PostcodeGeodataEntry> postcodes)
        {
            const int maxBatchsize = 100;
            if (postcodes.Count == 0)
            {
                throw new ArgumentException("postcodes list is empty");
            }
            HttpClient client = new HttpClient();
            List<PostcodeGeodataEntry> geodatas = new List<PostcodeGeodataEntry>();
            for (int i = 0; i < postcodes.Count; i += maxBatchsize)
            {
                var batch = postcodes.Skip(i).Take(maxBatchsize).ToList();
                await PostcodeApiBatchRequestAsync(client, batch, path);
            }
            client.Dispose();
        }

        private static async Task PostcodeApiBatchRequestAsync(HttpClient httpClient, List<PostcodeGeodataEntry> postcodes, string path)
        {
            string json = JsonSerializer.Serialize(new PostcodeRequest(postcodes.Select(x => x.Postcode).ToList()));
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(path, content);

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    PostcodeApiResult apiResult = await response.Content.ReadFromJsonAsync<PostcodeApiResult>();
                    //TODO properly handle terminated codes and codes with incomplete information (ex: "BN27 1AJ")
                    for (int i = 0; i < apiResult.result.Count; i++)
                    {
                        var r = apiResult.result[i].result;
                        if (r == null)
                        {
                            postcodes[i].IsNotAvailable = true;
                        }
                        else
                        {
                            postcodes[i].Longitude = r.longitude;
                            postcodes[i].Latitude = r.latitude;
                            postcodes[i].IsNotAvailable = false;
                        }

                    }
                }
                catch (NotSupportedException) // When content type is not valid
                {
                    Console.WriteLine("The content type is not supported.");
                }
                catch (JsonException) // Invalid JSON
                {
                    Console.WriteLine("Invalid JSON.");
                }
            }

        }

        public async Task<string> RequestClosestPlace(string postcode)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync($"{pathLoc}" + postcode);
            string result = "";
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                const string propertyName = "admin_district";

                using (JsonDocument doc = JsonDocument.Parse(responseBody))
                {
                    JsonElement root = doc.RootElement;

                    // Navigate through the JSON structure
                    if (root.TryGetProperty("result", out JsonElement resultElement) &&
                    resultElement.ValueKind == JsonValueKind.Object)
                    {
                        if (resultElement.TryGetProperty(propertyName, out JsonElement prop1Value))
                        {
                            result = prop1Value.ToString();
                        }
                        else
                        {
                            throw new JsonException($"Property {propertyName} not found.");
                        }
                    }
                    else
                    {
                        throw new JsonException("Property 'result' not found or is not an array.");
                    }
                }
            }
            return result;
        }
    }

    public class TestDataPostcodeLocationService : IPostcodeLocationService
    {
        public string GetUrl()
        {
            return "<<Local Test Data Set with 6 entries>>";
        }

        public Task<string> RequestClosestPlace(string postcode)
        {
            return Task.FromResult("TestTown");
        }

        public Task RequestPostcodeLocationsAsync(List<PostcodeGeodataEntry> postcodes)
        {
            foreach (var postcode in postcodes)
            {
                postcode.IsNotAvailable = false;
                postcode.Longitude = 0;
                postcode.Latitude = 0;

                if (postcode.Postcode == "AB25 3UZ")
                {
                    postcode.Longitude = -2.10764m;
                    postcode.Latitude = 57.154592m;
                }
                else if (postcode.Postcode == "B75 6HJ")
                {
                    postcode.Longitude = -1.799339m;
                    postcode.Latitude = 52.574142m;
                }
                else if (postcode.Postcode == "EH49 7LS")
                {
                    postcode.Longitude = -3.560637m;
                    postcode.Latitude = 55.982662m;
                }
                else if (postcode.Postcode == "FY8 3TF")
                {
                    postcode.Longitude = -3.008877m;
                    postcode.Latitude = 53.753436m;
                }
                else if (postcode.Postcode == "HP21 8PP")
                {
                    postcode.Longitude = -0.828588m;
                    postcode.Latitude = 51.80657m;
                }
                else if (postcode.Postcode == "TN22 9EF")
                {
                    postcode.Longitude = 0.086443m;
                    postcode.Latitude = 50.967863m;
                }
                else
                {
                    postcode.IsNotAvailable = true;
                }

            }
            return Task.CompletedTask;
        }
    }
}
