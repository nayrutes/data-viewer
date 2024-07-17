namespace WorldCompanyDataViewer.Models
{
    internal class PostcodeApiResultEntry
    {
        public string Query { get; set; } = "";
        public PostcodeApiResultEntry? Result { get; set; }
        public string Postcode { get; set; } = "";
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
    }

    internal class PostcodeApiResult
    {
        public int Status { get; set; }
        public required List<PostcodeApiResultEntry> Result { get; set; }
    }

    internal class PostcodeRequest
    {
        public List<string> Postcodes { get; set; }

        public PostcodeRequest(List<string> postcodes)
        {
            this.Postcodes = postcodes;
        }
    }
}
