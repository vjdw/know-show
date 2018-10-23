using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Blob;

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
            LogStore logStore = await GetLog(logStoreName);

            try
            {
                var base64EncodedBytes = System.Convert.FromBase64String(logResult);
                logResult = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            }
            catch
            {
                // not base64 encoded
            }

            var isSuccessful = logResult.ToLower().Contains("completed successfully");

            logStore.Logs.Add(new LogStore.LogItem(logTimestamp, isSuccessful, logResult));

            var container = m_blobClient.GetContainerReference("log-store");
            var blob = container.GetBlockBlobReference($"{logStoreName}.json");
            await blob.UploadTextAsync(JsonConvert.SerializeObject(logStore));
        }

        public async Task<LogStore> GetLog(string logStoreName, DateTime? onlyLogsSince = null)
        {
            var container = m_blobClient.GetContainerReference("log-store");
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobReference($"{logStoreName}.json");

            LogStore logStore = await blob.ExistsAsync()
                ? JsonConvert.DeserializeObject<LogStore>(await blob.DownloadTextAsync())
                : new LogStore(logStoreName);

            logStore.Logs = logStore.Logs
                .Where(log => onlyLogsSince == null || log.Timestamp >= onlyLogsSince)
                .OrderByDescending(log => log.Timestamp)
                .ToList();

            return logStore;
        }
    }
}