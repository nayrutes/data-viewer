using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorldCompanyDataViewer.Models;
using WorldCompanyDataViewer.Utils;

namespace WorldCompanyDataViewer.ViewModels
{
    internal class PostcodeAnalysis
    {
        public List<PostcodeGeodata> PostcodeLocations { get; private set; } = new();


        //TODO consider caching PostcodeLocations
        public async Task AnalyzePostcodes(DataEntryContext context)
        {
            HttpClient client = new HttpClient();
            Dictionary<string, int> postcodesCounts = await GetAllPostCodesWithCount(context);

            List<string> postcodes = postcodesCounts.Keys.ToList();
            //await RequestPostcodeLocations(client, new List<string>() { "OX49 5NU", "M32 0JG", "NE30 1DP" });
            PostcodeLocations = await RequestPostcodeLocations(client, postcodes);
            client.Dispose();
        }

        private async Task<Dictionary<string, int>> GetAllPostCodesWithCount(DataEntryContext context)
        {
            if(context == null)
            {
                return null;
            }
            Dictionary<string, int> result = await context.DataEntries.GroupBy(t => t.Postal)
                .Select(g => new
                {
                    Value = g.Key,
                    Count = g.Count()
                }).ToDictionaryAsync(x => x.Value!, x => x.Count);
            return result;
        }

        //TODO reorganize error handling
        private async Task<List<PostcodeGeodata>> RequestPostcodeLocations(HttpClient httpClient, List<string> postcodes)
        {
            List<PostcodeGeodata> geodatas = new List<PostcodeGeodata>();
            string path = "https://api.postcodes.io/postcodes?filter=postcode,longitude,latitude";
            const int maxBatchsize = 100;
            if (postcodes.Count == 0)
            {
                throw new ArgumentException("postcodes list is empty");
            }
            for (int i = 0; i < postcodes.Count; i += maxBatchsize)
            {
                var batch = postcodes.Skip(i).Take(maxBatchsize).ToList();
                List<PostcodeGeodata> batchData = await PostcodeApiBatchRequest(httpClient, batch, path);
                geodatas.AddRange(batchData);
            }

            return geodatas;
        }

        private static async Task<List<PostcodeGeodata>> PostcodeApiBatchRequest(HttpClient httpClient, List<string> postcodes, string path)
        {
            List<PostcodeGeodata> geodatas = new();
            string json = JsonSerializer.Serialize(new PostcodeRequest(postcodes));
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(path, content);

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    PostcodeApiResult apiResult = await response.Content.ReadFromJsonAsync<PostcodeApiResult>();
                    //TODO properly handle terminated codes and codes with incomplete information (ex: "BN27 1AJ")
                    geodatas.AddRange(apiResult.result.Where(x=>x.result != null).Select(x => new PostcodeGeodata(x.result.postcode, x.result.longitude, x.result.latitude)));
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

            return geodatas;
        }

        //TODO investigate if inaccuracy of Haversine Distance matters (assumption the earth is a spehere and not an ellipoid)
        public static double DistanceBetweenGeodata(PostcodeGeodata d1, PostcodeGeodata d2)
        {
            return HaversineDistance.DistanceBetweenPlaces(decimal.ToDouble(d1.Longitude), decimal.ToDouble(d1.Latitude), decimal.ToDouble(d2.Longitude), decimal.ToDouble(d2.Latitude));
        }
    }
}
