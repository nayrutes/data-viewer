using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WorldCompanyDataViewer.Models;

namespace WorldCompanyDataViewer.Services
{
    public interface IPostcodeLocationService
    {
        string GetUrl();
        Task RequestPostcodeLocationsAsync(List<PostcodeGeodataEntry> postcodes);
    }

    public class OnlinePostcodeLocationService : IPostcodeLocationService
    {
        const string path = "https://api.postcodes.io/postcodes?filter=postcode,longitude,latitude";
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
            //string path = "https://api.postcodes.io/postcodes?filter=postcode,longitude,latitude";
            for (int i = 0; i < postcodes.Count; i += maxBatchsize)
            {
                var batch = postcodes.Skip(i).Take(maxBatchsize).ToList();
                await PostcodeApiBatchRequestAsync(client, batch, path);
                //geodatas.AddRange(batchData);
            }
            client.Dispose();
            //return geodatas;
        }

        private static async Task PostcodeApiBatchRequestAsync(HttpClient httpClient, List<PostcodeGeodataEntry> postcodes, string path)
        {
            //List<PostcodeGeodataEntry> geodatas = new();
            string json = JsonSerializer.Serialize(new PostcodeRequest(postcodes.Select(x => x.Postcode).ToList()));
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(path, content);

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    //string responseContent = await response.Content.ReadAsStringAsync();
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
                    //geodatas.AddRange(apiResult.result.Where(x => x.result != null).Select(x => new PostcodeGeodataEntry(x.result.postcode, x.result.longitude, x.result.latitude)));
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

            //return geodatas;
        }

    }

    public class TestDataPostcodeLocationService : IPostcodeLocationService
    {
        public string GetUrl()
        {
            return "<<Local Test Data Set with 6 entries>>";
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

            //postcodes.AddRange(new List<PostcodeGeodataEntry> {
            //    new PostcodeGeodataEntry("AB25 3UZ", -2.10764m, 57.154592m),//"AB25 3UZ";-2,10764;57,154592
            //    new PostcodeGeodataEntry("B75 6HJ", -1.799339m, 52.574142m),//"B75 6HJ";-1,799339;52,574142
            //    new PostcodeGeodataEntry("EH49 7LS", -3.560637m, 55.982662m),//"EH49 7LS";-3,560637;55,982662
            //    new PostcodeGeodataEntry("FY8 3TF", -3.008877m, 53.753436m),//"FY8 3TF";-3,008877;53,753436
            //    new PostcodeGeodataEntry("HP21 8PP", -0.828588m, 51.80657m),//"HP21 8PP";-0,828588;51,80657
            //    new PostcodeGeodataEntry("TN22 9EF", 0.086443m, 50.967863m),//"TN22 9EF";0,086443;50,967863
            //});
            return Task.CompletedTask;
        }
    }
}
