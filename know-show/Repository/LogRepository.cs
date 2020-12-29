using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using KnowShow.Repository.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace KnowShow.Repository
{
    public class LogRepository
    {
        private CloudBlobClient m_blobClient;

        public LogRepository(string connectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            m_blobClient = storageAccount.CreateCloudBlobClient();
        }

        public async Task InsertLog(string logStoreName, DateTime logTimestamp, string logResult)
        {
            await InsertLogs(logStoreName, logTimestamp, new string[] { logResult });
        }

        public async Task InsertLogs(string logStoreName, DateTime logTimestamp, IEnumerable<string> logResults)
        {
            var container = m_blobClient.GetContainerReference("log-store");
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobReference($"{logStoreName}.json");
            if (!await blob.ExistsAsync())
            {
                var emptyLogStore = new LogStore(logStoreName, logStoreName);
                await blob.UploadTextAsync(JsonConvert.SerializeObject(emptyLogStore));
            }
            var blobLeaseId = await GetBlobLeaseId(blob);

            try
            {
                var logStore = JsonConvert.DeserializeObject<LogStore>(await blob.DownloadTextAsync());

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

                await blob.UploadTextAsync(
                    JsonConvert.SerializeObject(logStore),
                    Encoding.UTF8,
                    new AccessCondition() { LeaseId = blobLeaseId },
                    new BlobRequestOptions(),
                    new OperationContext()
                );
            }
            finally
            {
                await blob.ReleaseLeaseAsync(AccessCondition.GenerateLeaseCondition(blobLeaseId));
            }
        }

        public async Task<IEnumerable<string>> GetLogStoreNames()
        {
            var container = m_blobClient.GetContainerReference("log-store");
            await container.CreateIfNotExistsAsync();

            var logStoreNames = new List<string>();
            BlobContinuationToken continuationToken = null;
            do
            {
                var blobs = await container.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = blobs.ContinuationToken;
                logStoreNames.AddRange(blobs.Results.Select(_ => _.Uri.Segments.Last().Replace(".json", "")));

            } while (continuationToken != null);

            return logStoreNames;
        }


        public async Task<LogStore> GetLogStore(string logStoreName)
        {
            var container = m_blobClient.GetContainerReference("log-store");
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobReference($"{logStoreName}.json");

            LogStore logStore = await blob.ExistsAsync()
                ? JsonConvert.DeserializeObject<LogStore>(await blob.DownloadTextAsync())
                : new LogStore(logStoreName, logStoreName);

            return logStore;
        }

        private async Task<string> GetBlobLeaseId(CloudBlockBlob blob)
        {
            while (true)
            {
                try
                {
                    var blobLeaseId = await blob.AcquireLeaseAsync(TimeSpan.FromSeconds(20));
                    if (string.IsNullOrEmpty(blobLeaseId))
                        await Task.Delay(TimeSpan.FromMilliseconds(new Random(Guid.NewGuid().GetHashCode()).Next(250, 1000)));
                    else
                        return blobLeaseId;
                }
                catch (StorageException ex)
                {
                    if (ex.RequestInformation.HttpStatusCode != 409)
                        throw;
                }
            }
        }

        public bool CouldBeBase64String(string s)
        {
            s = s.Trim();
            return (s.Length % 4 == 0) && Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);

        }
    }
}