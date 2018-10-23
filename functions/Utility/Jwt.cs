using System;
using System.Collections.Generic;
using JWT.Builder;

namespace KnowShow.Utility
{
    internal static class Jwt
    {
        internal static string CreateJwt(string accountId)
        {
            var privateKeyPem = Environment.GetEnvironmentVariable("JwtPrivatePem", EnvironmentVariableTarget.Process);

            var token = new JwtBuilder()
                .WithAlgorithm(new JWT.Algorithms.HMACSHA512Algorithm())
                .WithSecret(privateKeyPem)
                .AddClaim("exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds())
                .AddClaim("id", accountId)
                .Build();

            return token;
        }

        internal static (bool isValid, string id) ValidateJwt(string jwt)
        {
            var privateKeyPem = Environment.GetEnvironmentVariable("JwtPrivatePem", EnvironmentVariableTarget.Process);

            var payload = new JwtBuilder()
                .WithSecret(privateKeyPem)
                .MustVerifySignature()
                .Decode<IDictionary<string, object>>(jwt);

            var idKeyExists = payload?.ContainsKey("id") ?? false;
            return (idKeyExists, idKeyExists ? (string)payload["id"] : "");
        }
    }
}