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
using WorldCompanyDataViewer.Services;
using WorldCompanyDataViewer.Utils;

namespace WorldCompanyDataViewer.ViewModels
{
    internal class PostcodeAnalysis
    {
        public List<PostcodeGeodata> PostcodeLocations { get; private set; } = new();
        public readonly IPostcodeLocationService postcodeLocationService;

        public PostcodeAnalysis(IPostcodeLocationService postcodeLocationService)
        { 
            this.postcodeLocationService = postcodeLocationService;
        }

        //TODO consider caching PostcodeLocations
        public async Task AnalyzePostcodes(DataEntryContext context)
        {
            Dictionary<string, int> postcodesCounts = await GetAllPostCodesWithCount(context);

            List<string> postcodes = postcodesCounts.Keys.ToList();
            
            PostcodeLocations = await postcodeLocationService.RequestPostcodeLocationsAsync(postcodes);
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


        //TODO investigate if inaccuracy of Haversine Distance matters (assumption the earth is a spehere and not an ellipoid)
        public static double DistanceBetweenGeodata(PostcodeGeodata d1, PostcodeGeodata d2)
        {
            return HaversineDistance.DistanceBetweenPlaces(decimal.ToDouble(d1.Longitude), decimal.ToDouble(d1.Latitude), decimal.ToDouble(d2.Longitude), decimal.ToDouble(d2.Latitude));
        }
    }
}
