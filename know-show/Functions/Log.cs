using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KnowShow.Utility;
using KnowShow.Repository;
using KnowShow.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

// setup:
// - CORS
// - App settings:
//   - ConnectionString
//   - SmtpServer
//   - SmtpUsername
//   - SmtpPassword
//   - JwtPublicPem / JwtPrivatePem generated by openssl
// - IsLocal=true in local.settings.json
// - dotnet add package mailkit
//
//
// POST / could create if id not found, error if found and type doesn't match existing? Some params would become optional if ID already found (minperiod). Others could be allowed to change (min/max).
//     ?id=xyz&type=heartbeat&minperiod=[auto/60m/24h]     / nightly backup, only send heartbeat if rsync contains "success", else use &type=message&level=error
//     ?id=xyz&type=range&min=0&max=100&current=88         / disk space monitor
//     ?id=xyz&type=progress&target=24&current=6           / photo cloud upload progress
//     ?id=xyz&type=flag&expected=true&current=false
//     ?id=xyz&type=message&level=[info/warning/error]&current=xyz

namespace KnowShow.Functions
{
    public class Log
    {
        IConfiguration _config;

        public Log(IConfiguration config)
        {
            _config = config;
        }

        [FunctionName("log")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, /*"get",*/ "post", Route = null)] HttpRequest request, ILogger logger)
        {
            string connectionString = _config.GetConnectionString("Storage");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception($"{nameof(connectionString)} is empty.");
            var repository = new LogRepository(connectionString);

            if (request.Method == "GET")
                return await HandleGet(request, repository, logger);
            else
                return await HandlePost(request, repository, logger);
        }

        private async Task<IActionResult> HandleGet(HttpRequest request, LogRepository repository, ILogger logger)
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

        private static async Task<IActionResult> HandlePost(HttpRequest request, LogRepository repository, ILogger logger)
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