using System;
using System.Configuration;
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

namespace KnowShow
{
    public static class Log
    {
        [FunctionName("Log")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            if (name == null)
                return new BadRequestObjectResult("Please pass a name on the query string or in the request body");

            string connectionString = Environment.GetEnvironmentVariable("ConnectionString", EnvironmentVariableTarget.Process);
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception($"{nameof(connectionString)} is empty.");

            var repository = new Repository(connectionString);

            if (req.Method == "GET")
                return (ActionResult)new OkObjectResult(await repository.GetLog(name));

            await repository.InsertLog(name, $"Backup at {DateTime.UtcNow.ToString()}");
            return new OkResult();
        }
    }
}
