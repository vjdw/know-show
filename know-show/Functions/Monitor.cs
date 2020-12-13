using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using KnowShow.Utility;
using KnowShow.Repository;
using MailKit;
using MailKit.Net.Smtp;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using MimeKit;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace KnowShow.Functions
{
    public class Monitor
    {
        IConfiguration _config;

        public Monitor(IConfiguration config)
        {
            _config = config;
        }

        [FunctionName("monitor")]
        public async Task Run([TimerTrigger("0 0 8 * * *", RunOnStartup = true)] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"{nameof(Monitor)} {nameof(Run)} function entered at: {DateTime.Now}");

            var statusMessage = await BuildStatusMessage();

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Know-Show", "noreply@know-show.azurewebsites.net"));
            message.To.Add(new MailboxAddress("Alert", "knowshowalert@vw.sent.com")); // xyzzy move email to config
            message.Subject = "xyzzy Need to aggregate errors";
            //message.Subject = $"{logStoreName} Status: {(mostRecentLog == null ? "Log Store Empty" : mostRecentLog.Successful ? "OK" : "Error")}";

            // xyzzy make this a nice looking html message
            message.Body = new TextPart("plain") { Text = statusMessage };

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

        private async Task<string> BuildStatusMessage()
        {
            string connectionString = _config.GetConnectionString("Storage");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception($"{nameof(connectionString)} is empty.");
            var repository = new LogRepository(connectionString);

            var statusMessageTasks = (await repository.GetLogStoreNames()).Select(async logStoreName =>
            {
                // xyzzy make period configurable per log store (some things only expected to run weekly)
                DateTime? onlyLogsSince = DateTime.UtcNow.Subtract(new TimeSpan(25, 0, 0));

                var logStore = await repository.GetLogStore(logStoreName);
                var mostRecentLog = logStore.Logs
                    .Where(log => onlyLogsSince == null || log.Timestamp >= onlyLogsSince)
                    .OrderByDescending(log => log.Timestamp)
                    .FirstOrDefault()
                    ?.SuccessByContains(logStore.SuccessPattern);
                //var mostRecentLog = logStore.Logs.FirstOrDefault()?.SuccessByContains(logStore.SuccessPattern);

                // xyzzy add FailByContains (like SuccessByContains)

                var statusMessage = $"{logStoreName} status at {DateTime.UtcNow.ToString("s")}Z:\n\n";
                if (mostRecentLog == null)
                    statusMessage += "No logs in last 25 hours";
                else if (mostRecentLog.Successful)
                    statusMessage += $"Most recent log entry flagged successful at {mostRecentLog.Timestamp.ToString("s")}Z";
                else
                    // xyzzy include failing log entry in the email
                    statusMessage += $"Most recent log entry flagged unsuccessful at {mostRecentLog.Timestamp.ToString("s")}Z\n\n{mostRecentLog.Result}";

                return statusMessage;
            });

            await Task.WhenAll(statusMessageTasks);

            var allLogStoreStatusMessages = new StringBuilder();
            foreach (var x in statusMessageTasks)
                allLogStoreStatusMessages.AppendLine(await x);

            return allLogStoreStatusMessages.ToString();
        }
    }
}