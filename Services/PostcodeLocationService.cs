using System;
using System.Collections.Generic;
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
        Task<List<PostcodeGeodata>> RequestPostcodeLocationsAsync(List<string> postcodes);
    }

    public class OnlinePostcodeLocationService : IPostcodeLocationService
    {
        //TODO reorganize error handling
        public async Task<List<PostcodeGeodata>> RequestPostcodeLocationsAsync(List<string> postcodes)
        {
            const int maxBatchsize = 100;
            if (postcodes.Count == 0)
            {
                throw new ArgumentException("postcodes list is empty");
            }
            HttpClient client = new HttpClient();
            List<PostcodeGeodata> geodatas = new List<PostcodeGeodata>();
            string path = "https://api.postcodes.io/postcodes?filter=postcode,longitude,latitude";
            for (int i = 0; i < postcodes.Count; i += maxBatchsize)
            {
                var batch = postcodes.Skip(i).Take(maxBatchsize).ToList();
                List<PostcodeGeodata> batchData = await PostcodeApiBatchRequestAsync(client, batch, path);
                geodatas.AddRange(batchData);
            }
            client.Dispose();
            return geodatas;
        }

        private static async Task<List<PostcodeGeodata>> PostcodeApiBatchRequestAsync(HttpClient httpClient, List<string> postcodes, string path)
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
                    geodatas.AddRange(apiResult.result.Where(x => x.result != null).Select(x => new PostcodeGeodata(x.result.postcode, x.result.longitude, x.result.latitude)));
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
    }

    public class TestDataPostcodeLocationService : IPostcodeLocationService
    {
        public Task<List<PostcodeGeodata>> RequestPostcodeLocationsAsync(List<string> postcodes)
        {
            var dataList = new List<PostcodeGeodata>()
            {
                new PostcodeGeodata("AB25 3UZ",-2.10764m,57.154592m),//"AB25 3UZ";-2,10764;57,154592
                new PostcodeGeodata("B75 6HJ",-1.799339m,52.574142m),//"B75 6HJ";-1,799339;52,574142
                new PostcodeGeodata("EH49 7LS",-3.560637m,55.982662m),//"EH49 7LS";-3,560637;55,982662
                new PostcodeGeodata("FY8 3TF",-3.008877m,53.753436m),//"FY8 3TF";-3,008877;53,753436
                new PostcodeGeodata("HP21 8PP",-0.828588m,51.80657m),//"HP21 8PP";-0,828588;51,80657
                new PostcodeGeodata("TN22 9EF",0.086443m,50.967863m),//"TN22 9EF";0,086443;50,967863
            };
            return Task.FromResult(dataList);
        }
    }
}
