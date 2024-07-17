using System.IO;
using System.Text.RegularExpressions;
using WorldCompanyDataViewer.Models;

namespace WorldCompanyDataViewer.Utils
{
    //TODO consider making the class generic to allow reuse
    public class CsvLoader
    {
        readonly int batchsize;
        public CsvLoader(int batchsize)
        {
            this.batchsize = batchsize;
        }

        public async Task<IEnumerable<DataEntry>> ReadCsvAsync(string filePath)
        {
            var dataEntries = new List<DataEntry>();

            using (var reader = new StreamReader(filePath))
            {
                string? line;
                // Skip header line
                if ((line = await reader.ReadLineAsync()) == null)
                {
                    throw new InvalidOperationException("CSV file is empty");
                }

                List<string> lines = new List<string>();
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lines.Add(line);

                }

                //TODO testing for unplanned data (empty entry, qutotes in data, ...). Consider using an external csv parsing package
                Regex CSVParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");//Alternative Regex: "[,]{1}(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))"

                Parallel.ForEach(lines, line =>
                {
                    string[] entry = CSVParser.Split(line);
                    DataEntry dataEntry = new DataEntry
                    {
                        FirstName = entry[0],
                        LastName = entry[1],
                        CompanyName = entry[2],
                        Address = entry[3],
                        City = entry[4],
                        County = entry[5],
                        Postal = entry[6],
                        Phone1 = entry[7],
                        Phone2 = entry[8],
                        Email = entry[9],
                        Website = entry[10],
                    };
                    lock (dataEntries)
                    {
                        dataEntries.Add(dataEntry);
                    }

                });
            }

            return dataEntries;
        }

        public IEnumerable<List<T>> ChunkBy<T>(IEnumerable<T> source, int size)
        {
            List<T> chunk = new(size);
            foreach (var element in source)
            {
                chunk.Add(element);
                if (chunk.Count == size)
                {
                    yield return chunk;
                    chunk = new List<T>(size);
                }
            }
            if (chunk.Any())
                yield return chunk;
        }

        public async Task LoadDataIntoDatabase(string filePath, DatabaseContext databaseContext)
        {
            IEnumerable<DataEntry> records = await ReadCsvAsync(filePath);
            IEnumerable<List<DataEntry>> chunks = ChunkBy(records, batchsize);

            databaseContext.ChangeTracker.AutoDetectChangesEnabled = false;

            foreach (var chunk in chunks)
            {
                await databaseContext.AddRangeAsync(chunk);
                await databaseContext.SaveChangesAsync();
                databaseContext.ChangeTracker.Clear();
            }

            databaseContext.ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }
}
