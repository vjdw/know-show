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

// setup:
// - CORS
// - ConnectionString
//
// POST / could create if id not found, error if found and type doesn't match existing? Some params would become optional if ID already found (minperiod). Others could be allowed to change (min/max).
//     ?id=xyz&type=heartbeat&minperiod=[auto/60m/24h]     / nightly backup, only send heartbeat if rsync contains "success", else use &type=message&level=error
//     ?id=xyz&type=range&min=0&max=100&current=88         / disk space monitor
//     ?id=xyz&type=progress&target=24&current=6           / photo cloud upload progress
//     ?id=xyz&type=flag&expected=true&current=false
//     ?id=xyz&type=message&level=[info/warning/error]&current=xyzzy

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

            string type = data.type;
            if (string.IsNullOrEmpty(type) || type == "heartbeat")
            {
                string message = data.message ?? "heartbeat logged";
                await repository.InsertLog(name, DateTime.UtcNow, message);
                return new OkResult();
            }

            return new BadRequestObjectResult($"Unrecognised value for {nameof(type)}: '{type}'");
        }
    }
}
