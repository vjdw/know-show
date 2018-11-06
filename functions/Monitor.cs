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
        public async static void Run([TimerTrigger("0 0 8 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var logStoreName = "Backup";

            string connectionString = Environment.GetEnvironmentVariable("ConnectionString", EnvironmentVariableTarget.Process);
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception($"{nameof(connectionString)} is empty.");
            var repository = new LogRepository(connectionString);

            DateTime? onlyLogsSince = DateTime.UtcNow.Subtract(new TimeSpan(25, 0, 0));

            var logStore = await repository.GetLog(logStoreName, onlyLogsSince);

            string statusMessage = $"{logStoreName} status at {DateTime.UtcNow.ToString("s")}Z:\n\n";
            var mostRecentLog = logStore.Logs.FirstOrDefault();
            if (mostRecentLog == null)
                statusMessage += "No logs in last 25 hours";
            else if (mostRecentLog.Successful)
                statusMessage += $"Most recent log entry flagged successful at {mostRecentLog.Timestamp.ToString("s")}Z";
            else
                statusMessage += $"Most recent log entry flagged unsuccessful at {mostRecentLog.Timestamp.ToString("s")}Z\n\n{mostRecentLog.Result}";

            var message = new MimeMessage();

            message.From.Add(new MailboxAddress("Know-Show", "noreply@know-show.azurewebsites.net"));
            message.To.Add(new MailboxAddress("Alert", "knowshowalert@vw.sent.com"));
            message.Subject = $"{logStoreName} Status: {(mostRecentLog.Successful ? "OK" : "Error")}";
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
