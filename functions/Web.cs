using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;

namespace KnowShow
{
    public static class Web
    {
        [FunctionName("Web")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "web/{path1?}/{path2?}")]HttpRequest req, ILogger log, string path1, string path2)
        {
            string connectionString = Environment.GetEnvironmentVariable("ConnectionString", EnvironmentVariableTarget.Process);
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception($"{nameof(connectionString)} is empty.");

            string path = string.IsNullOrWhiteSpace(path1)
                ? "index.html"
                : string.IsNullOrWhiteSpace(path2)
                    ? path1
                    : $"{path1}/{path2}";
            path = path.Replace("..","");

            string contentType = "text/html";
            if (path.ToLower().EndsWith(".css"))
                contentType = "text/css";
            else if (path.ToLower().EndsWith(".js"))
                contentType = "application/javascript";
            else if (path.ToLower().EndsWith(".tag"))
                contentType = "text/xml";

            bool.TryParse(Environment.GetEnvironmentVariable("IsLocal", EnvironmentVariableTarget.Process), out var isLocal);
            
            string blobContent;
            if (isLocal)
            {
                blobContent = File.ReadAllText($"/home/vin/code/know-show/web/{path}");
            }
            else
            {
                var storageAccount = CloudStorageAccount.Parse(connectionString);
                var blobClient = storageAccount.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference("$web");
                var blob = container.GetBlockBlobReference(path);
                if (!await blob.ExistsAsync())
                    return new NotFoundResult();
                blobContent = await blob.DownloadTextAsync();
            }

            var buffLength = System.Text.UnicodeEncoding.UTF8.GetByteCount(blobContent);
            req.HttpContext.Response.Headers.Add("Content-Length", buffLength.ToString());
            req.HttpContext.Response.Headers.Add("Content-Type", contentType);
            var buffer = System.Text.Encoding.UTF8.GetBytes(blobContent);
            await req.HttpContext.Response.Body.WriteAsync(buffer, 0, buffLength);
            return new OkResult();
        }
    }
}
