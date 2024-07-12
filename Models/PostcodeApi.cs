using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldCompanyDataViewer.Models
{
    internal class PostcodeApiResultEntry
    {
        public string query { get; set; }
        public PostcodeApiResultEntry result { get; set; }
        public string postcode { get; set; }
        public decimal longitude { get; set; }
        public decimal latitude { get; set; }
    }

    internal class PostcodeApiResult
    {
        public int status { get; set; }
        public List<PostcodeApiResultEntry> result { get; set; }
    }

    internal class PostcodeRequest
    {
        public List<string> postcodes { get; set; }

        public PostcodeRequest(List<string> postcodes)
        {
            this.postcodes = postcodes;
        }
    }
}
