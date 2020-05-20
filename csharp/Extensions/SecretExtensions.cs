using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Vigilantes.Dapr.Extensions
{
    public static class SecretExtensions
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
        private static readonly Dictionary<string, string> SecretCache = new Dictionary<string, string>();
        public const string SecretsPath = "/v1.0/secrets/";
        public static readonly string DefaultHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";

        public static async Task<string> DaprHttpGetSecretAsync(string secretStoreName, string secretName, HttpClient httpClient, string keyName = null,
                                                          Dictionary<string, string> metadata = null, CancellationToken cancellationToken = default)
        {
            var queryString = string.Empty;
            if (metadata != null)
            {
                queryString = new FormUrlEncodedContent(metadata).ReadAsStringAsync().Result;
            }

            var requestUrl = $"http://127.0.0.1:{DefaultHttpPort}{SecretsPath}{secretStoreName}/{secretName}?{queryString}";
            var secret = SecretCache.GetValueOrDefault(requestUrl, null);
            if (secret == null)
            {
                var response = await httpClient.GetAsync(requestUrl);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Unable to read secret. Response: {json}");
                }

                var jToken = JToken.Parse(json);

                secret = jToken.Value<string>(keyName ?? secretName);
                SecretCache[requestUrl] = secret;
            }

            return secret;
        }
    }
}