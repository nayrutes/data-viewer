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
                        FirstName = entry[0].Trim('"'),
                        LastName = entry[1].Trim('"'),
                        CompanyName = entry[2].Trim('"'),
                        Address = entry[3].Trim('"'),
                        City = entry[4].Trim('"'),
                        County = entry[5].Trim('"'),
                        Postal = entry[6].Trim('"'),
                        Phone1 = entry[7].Trim('"'),
                        Phone2 = entry[8].Trim('"'),
                        Email = entry[9].Trim('"'),
                        Website = entry[10].Trim('"'),
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

        public async Task LoadDataIntoDatabase(string filePath, DatabaseContext databaseContext, Action<string, int> setProgressTextAndPercentage)
        {
            setProgressTextAndPercentage($"Reading File", 30);
            IEnumerable<DataEntry> records = await ReadCsvAsync(filePath);
            setProgressTextAndPercentage($"Chunking Data", 60);
            IEnumerable<List<DataEntry>> chunks = ChunkBy(records, batchsize);

            databaseContext.ChangeTracker.AutoDetectChangesEnabled = false;
            int allCount = chunks.Count();
            int counter = 0;
            foreach (var chunk in chunks)
            {
                counter++;
                setProgressTextAndPercentage($"Loading chunk {counter}/{allCount}", (counter *100)/allCount);
                await databaseContext.AddRangeAsync(chunk);
                await databaseContext.SaveChangesAsync();
                databaseContext.ChangeTracker.Clear();
            }

            databaseContext.ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }
}
