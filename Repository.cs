using System;
using System.IO;
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

namespace KnowShow
{
    public class Repository
    {
        private CloudBlobClient m_blobClient;

        public Repository(string connectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            m_blobClient = storageAccount.CreateCloudBlobClient();
        }

        public async Task InsertLog(string logStoreName, string logEntry)
        {
            LogStore logStore = await GetLog(logStoreName);

            logStore.Logs.Add(logEntry);

            var container = m_blobClient.GetContainerReference("log-store");
            var blob = container.GetBlockBlobReference($"{logStoreName}.json");
            await blob.UploadTextAsync(JsonConvert.SerializeObject(logStore));
        }

        public async Task<LogStore> GetLog(string logStoreName)
        {
            var container = m_blobClient.GetContainerReference("log-store");
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobReference($"{logStoreName}.json");

            LogStore logStore = await blob.ExistsAsync()
                ? JsonConvert.DeserializeObject<LogStore>(await blob.DownloadTextAsync())
                : new LogStore(logStoreName);

            return logStore;
        }
    }
}