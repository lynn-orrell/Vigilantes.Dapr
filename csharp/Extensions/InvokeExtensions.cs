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
    public static class InvokeExtensions
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
        public const string InvokePath = "/v1.0/invoke/";
        public static readonly string DefaultHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";

        public static async Task<HttpResponseMessage> DaprHttpInvokeGetAsync<T>(this T value, string daprAppId, string daprAppMethodName, HttpClient httpClient,
                                                                           Dictionary<string, string> queryParams = null, CancellationToken cancellationToken = default)
        {
            var queryString = string.Empty;
            if (queryParams != null)
            {
                queryString = new FormUrlEncodedContent(queryParams).ReadAsStringAsync().Result;
            }

            var response = await httpClient.GetAsync($"http://127.0.0.1:{DefaultHttpPort}{InvokePath}{daprAppId}/method/{daprAppMethodName}?{queryString}", cancellationToken);
            var json = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(json))
            {
                return null;
            }
            var jToken = JToken.Parse(json);
            switch (jToken.Type)
            {
                case JTokenType.Array:
                case JTokenType.Object:
                    {
                        JsonSerializer.Create(JsonSerializerSettings).Populate(jToken.CreateReader(), value);
                        break;
                    }
                default:
                    {
                        value = jToken.Value<T>();
                        break;
                    }
            }

            return response;
        }

        public static Task<HttpResponseMessage> DaprHttpInvokePostAsync<T>(this T value, string daprAppId, string daprAppMethodName, HttpClient httpClient,
                                                                           CancellationToken cancellationToken = default)
        {
            var json = JsonConvert.SerializeObject(value, Formatting.None, JsonSerializerSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return httpClient.PostAsync($"http://127.0.0.1:{DefaultHttpPort}{InvokePath}{daprAppId}/method/{daprAppMethodName}", content, cancellationToken);
        }

        public static Task<HttpResponseMessage> DaprHttpInvokePutAsync<T>(this T value, string daprAppId, string daprAppMethodName, HttpClient httpClient,
                                                                          CancellationToken cancellationToken = default)
        {
            var json = JsonConvert.SerializeObject(value, Formatting.None, JsonSerializerSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return httpClient.PutAsync($"http://127.0.0.1:{DefaultHttpPort}{InvokePath}{daprAppId}/method/{daprAppMethodName}", content, cancellationToken);
        }

        public static async Task<HttpResponseMessage> DaprHttpInvokeDeleteAsync(string daprAppId, string daprAppMethodName, HttpClient httpClient,
                                                                                Dictionary<string, string> queryParams = null, CancellationToken cancellationToken = default)
        {
            var queryString = string.Empty;
            if (queryParams != null)
            {
                queryString = new FormUrlEncodedContent(queryParams).ReadAsStringAsync().Result;
            }

            var response = await httpClient.DeleteAsync($"http://127.0.0.1:{DefaultHttpPort}{InvokePath}{daprAppId}/method/{daprAppMethodName}", cancellationToken);

            return response;
        }
    }
}