using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using JWT.Builder;
using Microsoft.Extensions.Logging;

namespace KnowShow.Utility
{
    internal static class Authentication
    {
        internal static bool HasAccountToken(HttpRequest request, out string accountId, ILogger log)
        {
            if (request.Cookies.TryGetValue("auth", out var jwt))
            {
                var validationResult = Jwt.ValidateJwt(jwt, log);
                if (validationResult.isValid)
                {
                    accountId = validationResult.id;
                    return true;
                }
            }

            accountId = default(string);
            return false;
        }
    }
}