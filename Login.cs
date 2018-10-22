using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using JWT.Builder;

namespace KnowShow
{
    public static class Login
    {
        [FunctionName("Login")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string username = req.Form["username"];
            string password = req.Form["password"];

            return username != null && password == "hunter2"
                ? (ActionResult)new OkObjectResult(CreateJwt())
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

        private static string CreateJwt()
        {
            var privateKeyPem = Environment.GetEnvironmentVariable("JwtPrivatePem", EnvironmentVariableTarget.Process);

            var token = new JwtBuilder()
                .WithAlgorithm(new JWT.Algorithms.HMACSHA512Algorithm())
                .WithSecret(privateKeyPem)
                .AddClaim("exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds())
                .AddClaim("claim2", "claim2-value")
                .Build();

            return token;
        }
    }
}
