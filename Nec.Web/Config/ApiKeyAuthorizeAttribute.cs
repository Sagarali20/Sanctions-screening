using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Nec.Web.Config
{
    public class ApiKeyAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private const string HeaderName = "x-api-key";

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var request = context.HttpContext.Request;

            if (!request.Headers.TryGetValue(HeaderName, out var headerValue))
            {
                context.Result = new ContentResult
                {
                    StatusCode = 401,
                    Content = "Unauthorized: Missing API key header"
                };
                return;
            }

            var apiKey = headerValue.ToString().Trim();

            if (string.IsNullOrEmpty(apiKey))
            {
                context.Result = new ContentResult
                {
                    StatusCode = 401,
                    Content = "Unauthorized: Empty API key"
                };
                return;
            }

            var config = context.HttpContext.RequestServices
                .GetRequiredService<IConfiguration>();

            var realApiKey = config["ApiKeySettings:Key"];

            var inputHash = ComputeHash(apiKey);
            var storedHash = ComputeHash(realApiKey);

            if (!SecureEquals(inputHash, storedHash))
            {
                context.Result = new ContentResult
                {
                    StatusCode = 401,
                    Content = "Unauthorized: Invalid API key"
                };
            }
        }

        private static string ComputeHash(string apiKey)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
            return Convert.ToBase64String(hash);
        }

        private static bool SecureEquals(string a, string b)
        {
            var aBytes = Encoding.UTF8.GetBytes(a);
            var bBytes = Encoding.UTF8.GetBytes(b);

            if (aBytes.Length != bBytes.Length) return false;

            var result = 0;
            for (int i = 0; i < aBytes.Length; i++)
                result |= aBytes[i] ^ bBytes[i];

            return result == 0;
        }
    }
}
