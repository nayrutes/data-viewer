using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldCompanyDataViewer.Models
{
    internal class PostcodeGeodata
    {
        public PostcodeGeodata(string postcode, decimal longitude, decimal latitude)
        {
            Postcode = postcode;
            Longitude = longitude;
            Latitude = latitude;
        }

        public string Postcode {  get; set; }
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
    }
}
