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
                    StatusCode = (int)HttpStatusCode.Unauthorized,
                    Content = "Unauthorized: Missing API key header"
                };
                return;
            }

            var apiKey = headerValue.ToString().Trim();

            if (string.IsNullOrEmpty(apiKey))
            {
                context.Result = new ContentResult
                {
                    StatusCode = (int)HttpStatusCode.Unauthorized,
                    Content = "Unauthorized: Empty API key"
                };
                return;
            }

            // Validate API key
            var inputHash = ComputeHash(apiKey);
            var storedHash = GetStoredApiKeyHash();

            if (!SecureEquals(inputHash, storedHash))
            {
                context.Result = new ContentResult
                {
                    StatusCode = (int)HttpStatusCode.Unauthorized,
                    Content = "Unauthorized: Invalid API key"
                };
                return;
            }
        }

        private static string ComputeHash(string apiKey)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
            return Convert.ToBase64String(hash);
        }

        private static string GetStoredApiKeyHash()
        {
            var realApiKey = "6b7284fb1fba0492cd2c769c52ca2fc9";
            return ComputeHash(realApiKey);
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
