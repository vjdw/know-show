using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit;
using MimeKit;
using Newtonsoft.Json;
using KnowShow.Repository;

namespace KnowShow
{
    public static class Monitor
    {
        [FunctionName("Monitor")]
        public async static void Run([TimerTrigger("0 0 9 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var logStoreName = "Backup";

            var hostname = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME", EnvironmentVariableTarget.Process);
            bool.TryParse(Environment.GetEnvironmentVariable("IsLocal", EnvironmentVariableTarget.Process), out var isLocal);
            if (isLocal)
                hostname = "localhost:7071";
            
            var logUrl = $"http{(isLocal ? "" : "s")}://{hostname}/api/log?code=DuEIMldhwbmHrpbMVu9fCxXnntVhlTjrQ5oM3odqPvI473o5RALXaQ==&name={logStoreName}&hours=25";
            
            using(var client = new HttpClient())
            {
                var result = await client.GetAsync(new Uri(logUrl));

                if (!result.IsSuccessStatusCode)
                {
                    log.LogError($"{logUrl} returned {result.StatusCode}");
                }

                string resultContent = await result.Content.ReadAsStringAsync();
                var logStore = JsonConvert.DeserializeObject<LogStore>(resultContent);

                string statusMessage = $"{logStoreName} status at {DateTime.UtcNow.ToString("s")}Z:\n\n";
                var mostRecentLog = logStore.Logs.FirstOrDefault();
                if (mostRecentLog == null)
                    statusMessage += "No logs in last 25 hours";
                else if (!mostRecentLog.Successful)
                    statusMessage += $"Most recent log entry flagged unsuccessful at {mostRecentLog.Timestamp.ToString("s")}Z\n\n{mostRecentLog.Result}";
                else
                    statusMessage += $"Most recent log entry flagged successful at {mostRecentLog.Timestamp.ToString("s")}Z";

                var message = new MimeMessage();

                message.From.Add(new MailboxAddress("Know-Show", "noreply@know-show.azurewebsites.net"));
                message.To.Add(new MailboxAddress("Alert", "knowshowalert@vw.sent.com"));
                message.Subject = $"{logStoreName} Status";
                message.Body = new TextPart("plain"){ Text = statusMessage };

                using (var smtpClient = new SmtpClient())
                {
                    var smtpServer = Environment.GetEnvironmentVariable("SmtpServer", EnvironmentVariableTarget.Process);
                    var smtpUsername = Environment.GetEnvironmentVariable("SmtpUsername", EnvironmentVariableTarget.Process);
                    var smtpPassword = Environment.GetEnvironmentVariable("SmtpPassword", EnvironmentVariableTarget.Process);
                    smtpClient.Connect(smtpServer, 465);
                    smtpClient.Authenticate(smtpUsername, smtpPassword);

                    smtpClient.Send(message);
                    smtpClient.Disconnect(true);
                }

                log.LogInformation("Monitor function completed successfully.");
            }
        }
    }
}
