using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KnowShow.Utility;
using KnowShow.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;

// setup:
// - since azure rbac changes keyvault is difficult to get working in Azure Functions, so:
// - add "Storage" connection string under Function App / Environment Variables / Connection Strings
// - add other config and secrets under Function App / Environment Variables / Values (smtp, to address, from address)
// - see BlobStorage-Asset/readme.txt for putting SVGs in blob storage

namespace KnowShow.Functions
{
    public class Log
    {
        private readonly ILogger<Log> _logger;
        private readonly IConfiguration _config;

        public Log(ILogger<Log> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        [Function("log")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest request)
        {
            string connectionString = _config.GetConnectionString("Storage");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception($"{nameof(connectionString)} is empty.");
            var repository = new LogRepository(connectionString);

            if (request.Method == "GET")
                return await HandleGet(request, repository);
            else
                return await HandlePost(request, repository);
        }

        private async Task<IActionResult> HandleGet(HttpRequest request, LogRepository repository)
        {
            string name = request.Query["name"];
            if (name == null)
                return new BadRequestObjectResult("Pass a name on the query string");

            string type = request.Query["type"];
            if (type == null)
                type = "log";

            if (type == "log")
            {
                DateTime? onlyLogsSince = null;
                if (int.TryParse(request.Query["hours"], out int hours))
                    onlyLogsSince = DateTime.UtcNow.Subtract(new TimeSpan(hours, 0, 0));

                var logStore = await repository.GetLogStore(name);
                var processedLogs = logStore.Logs
                    .Where(log => onlyLogsSince == null || log.Timestamp >= onlyLogsSince)
                    .OrderByDescending(log => log.Timestamp)
                    .SuccessByContains(logStore.SuccessPattern);
                var logResult = new Models.GetLogsDto(logStore.Name, processedLogs);

                return (ActionResult) new OkObjectResult(logResult);
            }
            else
            {
                // TODO: could add support for "info" type, e.g. server disk usage could be posted to be include in status email
                return new NotFoundObjectResult($"Unknown type '{type}'.");
            }
        }

        private static async Task<IActionResult> HandlePost(HttpRequest request, LogRepository repository)
        {
            string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string name = data.name;
            if (name == null)
                return new BadRequestObjectResult("Please pass a name on the query string or in the request body");

            string[] messages =
                data.message != null
                ? new string[] { data.message }
                : data.messages.ToObject<string[]>();

            await repository.InsertLogs(name, DateTime.UtcNow, messages);

            return new OkResult();
        }
    }
}