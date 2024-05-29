using System;
using System.Linq;
using System.Threading.Tasks;
using KnowShow.Utility;
using KnowShow.Repository;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace KnowShow.Functions
{
    public class Monitor
    {
        private readonly ILogger<Monitor> _logger;
        private readonly IConfiguration _config;

        public Monitor(ILogger<Monitor> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        [Function("monitor")]
        public async Task Run([TimerTrigger("0 0 8 * * *", RunOnStartup = false)] TimerInfo myTimer)
        {
            _logger.LogInformation($"{nameof(Monitor)} {nameof(Run)} function entered at: {DateTime.Now}");

            var logResults = await BuildLogResults();
            var emailMessage = BuildEmail(logResults.ToList());

            using (var smtpClient = new SmtpClient())
            {
                var smtpServer = _config.GetValue<string>("SmtpServer");
                var smtpUsername = _config.GetValue<string>("SmtpUsername");
                var smtpPassword = _config.GetValue<string>("SmtpPassword");
                smtpClient.Connect(smtpServer, 465);
                smtpClient.Authenticate(smtpUsername, smtpPassword);

                smtpClient.Send(emailMessage);
                smtpClient.Disconnect(true);
            }

            _logger.LogInformation("Monitor function completed successfully.");
        }

        private async Task<IEnumerable<LogResult>> BuildLogResults()
        {
            string connectionString = _config.GetConnectionString("Storage");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception($"{nameof(connectionString)} is empty.");
            var repository = new LogRepository(connectionString);

            var statusMessageTasks = (await repository.GetLogStoreNames()).Select(async logStoreName =>
            {
                var logStore = await repository.GetLogStore(logStoreName);

                DateTime? onlyLogsSince = DateTime.UtcNow.Subtract(new TimeSpan(logStore.PeriodHours, 0, 0));

                var mostRecentLog = logStore.Logs
                    .Where(log => onlyLogsSince == null || log.Timestamp >= onlyLogsSince)
                    .OrderByDescending(log => log.Timestamp)
                    .FirstOrDefault()
                    ?.SuccessByContains(logStore.SuccessPattern);

                var statusMessage = $"{logStoreName} status at {DateTime.UtcNow.ToString("s")}Z:\n\n";
                if (mostRecentLog == null)
                    return new LogResult { Title = logStore.DisplayName, Timestamp = DateTime.MaxValue, DisplayOrder = logStore.DisplayOrder, Success = SuccessState.Missing, Message = $"No logs in last {logStore.PeriodHours} hours" };
                else if (mostRecentLog.Successful)
                    return new LogResult { Title = logStore.DisplayName, Timestamp = mostRecentLog.Timestamp, DisplayOrder = logStore.DisplayOrder, Success = SuccessState.Success, Message = $"Log ok at {mostRecentLog.Timestamp.ToString("s")}Z" };
                else
                    return new LogResult { Title = logStore.DisplayName, Timestamp = mostRecentLog.Timestamp, DisplayOrder = logStore.DisplayOrder, Success = SuccessState.Failed, Message = mostRecentLog.Result };
            });

            var statusMessages = await Task.WhenAll(statusMessageTasks);
            return statusMessages.OrderByDescending(_ => _.Timestamp).ThenBy(_ => _.DisplayOrder);
        }

        private MimeMessage BuildEmail(IEnumerable<LogResult> logResults)
        {
            var emailGeneratedAtUtc = DateTime.UtcNow;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Know-Show", _config.GetValue<string>("EmailFromAddress")));
            message.To.Add(new MailboxAddress("Alert", _config.GetValue<string>("Client:EmailAddress")));

            var successLogCount = logResults.Count(_ => _.Success == SuccessState.Success);
            var totalLogCount = logResults.Count();
            message.Subject = $"{(successLogCount == totalLogCount ? "🆗" : "❌")} Know-Show - {successLogCount}/{totalLogCount} @ {emailGeneratedAtUtc.ToString("s")}Z";
            var builder = new BodyBuilder();
            builder.TextBody = BuildTextEmailMessage(logResults, emailGeneratedAtUtc);
            builder.HtmlBody = BuildHtmlEmailMessage(logResults, emailGeneratedAtUtc);

            message.Body = builder.ToMessageBody();

            return message;
        }

        private string BuildHtmlEmailMessage(IEnumerable<LogResult> logResults, DateTime emailGeneratedAtUtc)
        {
            var resultsHtml = new StringBuilder();
            foreach (var logResult in logResults)
            {
                resultsHtml.Append(@$"<div class=""result-container"">
    <span class=""title"">{logResult.Title}</span>
    <div class=""result-status-container"">
        <span>{(logResult.Success == SuccessState.Success ? "OK" : logResult.Success == SuccessState.Missing ? "MISSING" : "ERROR")}</span>
        <img src=""https://knowshowlivestorage.blob.core.windows.net/assets/{(logResult.Success == SuccessState.Success ? "green" : logResult.Success == SuccessState.Missing ? "amber" : "red")}-circle.svg""></img>
    </div>
    <p class=""message"">{logResult.Message}</p>
</div>");
            }

            var htmlBody = @$"<html><head><meta charset=""UTF-8"">
<style>
body {{
    margin: 0;
    color: dimgray;
    background-color: white;
    font-family: 'Courier New', monospace;
}}

footer {{
    font-family: 'Courier New', monospace;
    font-size: 0.8em;
    color: gray;
    margin: 3em 0 0 0;
}}

.result-container {{
    background-color: whitesmoke;
    padding: 0.8em 0.8em 0.1em 0.8em;
    margin: 0 0 0.5em 0;
    border-radius: 0.3em;
}}

.result-container span {{
    vertical-align: middle;
}}

.result-container .title {{
    font-weight: bold;
}}

.result-container .message {{
    font-family: 'Courier New', monospace;
    
}}

.result-status-container {{
    float: right;
}}

.result-status-container span, .result-status-container img {{
    vertical-align: middle;
}}
</style>
</head>
<body>
<br>
{resultsHtml}
<body>
<footer>Generated at {emailGeneratedAtUtc.ToString("s")}Z</footer>
<html>";

            return htmlBody;
        }

        private string BuildTextEmailMessage(IEnumerable<LogResult> logResults, DateTime emailGeneratedAtUtc)
        {
            var resultsText = new StringBuilder();
            foreach (var logResult in logResults)
            {
                resultsText.AppendLine($@"{logResult.Title} {(logResult.Success == SuccessState.Success ? "OK" : logResult.Success == SuccessState.Missing ? "MISSING" : "ERROR")}");
                resultsText.AppendLine(logResult.Message);
                resultsText.AppendLine();
            }

            return resultsText.ToString();
        }

        private enum SuccessState
        {
            Missing,
            Failed,
            Success
        }

        private class LogResult
        {
            public string Title { get; set; }
            public int DisplayOrder { get; set; }
            public SuccessState Success { get; set; }
            public string Message { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}