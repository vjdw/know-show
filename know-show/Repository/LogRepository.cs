using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using KnowShow.Repository.Entities;
using Newtonsoft.Json;

namespace KnowShow.Repository
{
    public class LogRepository
    {
        private BlobServiceClient m_blobClient;

        public LogRepository(string connectionString)
        {
            m_blobClient = new BlobServiceClient(connectionString);
            var container = m_blobClient.GetBlobContainerClient("log-store");
            container.CreateIfNotExists();
        }

        public async Task InsertLog(string logStoreName, DateTime logTimestamp, string logResult)
        {
            await InsertLogs(logStoreName, logTimestamp, new string[] { logResult });
        }

        public async Task InsertLogs(string logStoreName, DateTime logTimestamp, IEnumerable<string> logResults)
        {
            var container = m_blobClient.GetBlobContainerClient("log-store");
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlobClient($"{logStoreName}.json");
            if (!await blob.ExistsAsync())
            {
                var emptyLogStore = new LogStore(logStoreName, logStoreName);
                await blob.UploadAsync(JsonConvert.SerializeObject(emptyLogStore));
            }
            var blobLeaseId = await GetBlobLeaseId(blob);

            try
            {
                var blobDownloadResult = await blob.DownloadContentAsync();
                var logStore = JsonConvert.DeserializeObject<LogStore>(blobDownloadResult.Value.Content.ToString());

                foreach (var logResult in logResults)
                {
                    string logResultDecoded = null;
                    if (CouldBeBase64String(logResult))
                    {
                        try
                        {
                            var base64EncodedBytes = Convert.FromBase64String(logResult);
                            logResultDecoded = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
                        }
                        catch
                        {
                            // not base64 encoded
                        }
                    }

                    if (logResultDecoded == null)
                        logResultDecoded = logResult;

                    logStore.Logs.Add(new LogStore.LogStoreItem(logTimestamp, logResultDecoded));
                }

                // Keep only last 30 logs
                logStore.Logs = logStore.Logs.OrderByDescending(_ => _.Timestamp).Take(30).ToList();
                
                await blob.UploadAsync(
                    BinaryData.FromString(JsonConvert.SerializeObject(logStore)),
                    new BlobUploadOptions() { Conditions = new BlobRequestConditions { LeaseId = blobLeaseId } }
                );
            }
            finally
            {
                await blob.GetBlobLeaseClient(blobLeaseId).ReleaseAsync();
            }
        }

        public async Task<IEnumerable<string>> GetLogStoreNames()
        {
            var container = m_blobClient.GetBlobContainerClient("log-store");
            await container.CreateIfNotExistsAsync();

            var logStoreNames = new List<string>();

            await foreach (BlobItem item in container.GetBlobsAsync())
            {
                logStoreNames.Add(item.Name.Replace(".json", ""));
            }

            return logStoreNames;
        }


        public async Task<LogStore> GetLogStore(string logStoreName)
        {
            var container = m_blobClient.GetBlobContainerClient("log-store");
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlobClient($"{logStoreName}.json");

            LogStore logStore = await blob.ExistsAsync()
                ? JsonConvert.DeserializeObject<LogStore>((await blob.DownloadContentAsync()).Value.Content.ToString())
                : new LogStore(logStoreName, logStoreName);

            return logStore;
        }

        private async Task<string> GetBlobLeaseId(BlobClient blobClient)
        {
            while (true)
            {
                BlobLeaseClient leaseClient = blobClient.GetBlobLeaseClient();

                Response<BlobLease> response = await leaseClient.AcquireAsync(duration: TimeSpan.FromSeconds(20));
                    
                if (string.IsNullOrEmpty(response.Value?.LeaseId))
                    await Task.Delay(TimeSpan.FromMilliseconds(new Random(Guid.NewGuid().GetHashCode()).Next(250, 1000)));
                else
                    return response.Value.LeaseId;
            }
        }

        public bool CouldBeBase64String(string s)
        {
            s = s.Trim();
            return (s.Length % 4 == 0) && Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);

        }
    }
}