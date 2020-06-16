using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Vigilantes.Dapr.Extensions
{
    public static class BindingExtensions
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
        public const string BindingPath = "/v1.0/bindings/";
        public static readonly string DefaultHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";

        public static Task<HttpResponseMessage> DaprHttpWriteToBindingAsync<T>(this T value, string bindingName, HttpClient httpClient, Dictionary<string, string> metadata = null,
                                                                               string operation = "create", CancellationToken cancellationToken = default) where T : class
        {
            var daprData = new
            {
                data = value,
                metadata = metadata,
                operation = operation
            };

            var json = JsonConvert.SerializeObject(daprData, Formatting.None, JsonSerializerSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return httpClient.PostAsync($"http://127.0.0.1:{DefaultHttpPort}{BindingPath}{bindingName}", content, cancellationToken);
        }
    }
}