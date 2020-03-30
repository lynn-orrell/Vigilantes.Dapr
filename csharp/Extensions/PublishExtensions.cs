using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Vigilantes.Dapr.Extensions
{
    public static class PublishExtensions
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
        public const string PublishPath = "/v1.0/publish/";
        public static readonly string DefaultHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";

        public static Task<HttpResponseMessage> DaprHttpPublishAsync<T>(this T value, 
                                                                        string topicName, 
                                                                        HttpClient httpClient, 
                                                                        CancellationToken cancellationToken = default) where T : class
        {
            var json = JsonConvert.SerializeObject(value, Formatting.None, JsonSerializerSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return httpClient.PostAsync($"http://127.0.0.1:{DefaultHttpPort}{PublishPath}{topicName}", content, cancellationToken);
        }

    }
}