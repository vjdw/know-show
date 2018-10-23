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
using KnowShow.Utility;

namespace KnowShow
{
    public static class Login
    {
        [FunctionName("Login")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string username = req.Form["username"];
            string password = req.Form["password"];

            return username != null && password == "hunter2"
                ? (ActionResult)new OkObjectResult(Jwt.CreateJwt(username))
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }
    }
}
