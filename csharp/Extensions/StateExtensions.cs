using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Vigilantes.Dapr.Extensions
{
    public static class StateExtensions
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
        public const string StatePath = "/v1.0/state/";
        public static readonly string DefaultHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";
        public enum Concurrency
        {
            FIRST_WRITE,
            LAST_WRITE
        }

        public enum Consistency
        {
            EVENTUAL,
            STRONG
        }

        public enum RetryPattern
        {
            EXPONENTIAL,
            LINEAR
        }

        public static Task<HttpResponseMessage> DaprHttpSaveStateAsync<T>(this T value, string storeName, string key, HttpClient httpClient, string eTag = null,
                                         Concurrency? concurrency = null, Consistency? consistency = null, int? retryInterval = null,
                                         int? retryThreshold = null, RetryPattern? retryPattern = null) where T : class
        {
            var daprState = new[]
            {
                new
                {
                    key = key,
                    value = value,
                    etag = eTag,
                    options = new
                    {
                        concurrency = concurrency == null || !concurrency.HasValue ? null : concurrency == Concurrency.FIRST_WRITE ? "first-write" : "last-write",
                        consistency = consistency == null || !consistency.HasValue ? null : consistency == Consistency.EVENTUAL ? "eventual" : "strong",
                        retryPolicy = new
                        {
                            interval = retryInterval == null || !retryInterval.HasValue ? (int?)null : retryInterval.Value,
                            threshold = retryThreshold == null || !retryThreshold.HasValue ? (int?)null : retryThreshold.Value,
                            pattern = retryPattern == null || !retryPattern.HasValue ? null : retryPattern == RetryPattern.EXPONENTIAL ? "exponential" : "linear"
                        }
                    }
                }
            };

            var json = JsonConvert.SerializeObject(daprState, Formatting.None, JsonSerializerSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return httpClient.PostAsync($"http://127.0.0.1:{DefaultHttpPort}{StatePath}{storeName}", content);
        }

        public static async Task<string> DaprHttpLoadStateAsync<T>(this T value, string storeName, string key, HttpClient httpClient, Consistency? consistency = null) where T : class
        {
            var consistencyParam = consistency == null ? string.Empty : consistency == Consistency.EVENTUAL ? "eventual" : "strong";
            var response = await httpClient.GetAsync($"http://127.0.0.1:{DefaultHttpPort}{StatePath}{storeName}/{key}?{consistencyParam}");
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

            // This check is required because Dapr doesn't currently return etags correctly. Per the spec they should be quoted, but dapr isn't doing that.
            // Because of this, they don't show up in the standard headers but rather a custom header.
            return response.Headers.ETag?.Tag.Replace(@"""", string.Empty) ?? response.Headers.FirstOrDefault(h => h.Key.Equals("ETag", StringComparison.CurrentCultureIgnoreCase)).Value?.FirstOrDefault();
        }

        public static Task<HttpResponseMessage> DaprHttpDeleteStateAsync(string storeName, string key, HttpClient httpClient, string eTag = null,
                                                                       Concurrency? concurrency = null, Consistency? consistency = null, int? retryInterval = null,
                                                                       int? retryThreshold = null, RetryPattern? retryPattern = null)
        {
            var queryParams = new Dictionary<string, string>();

            if(concurrency != null)
            {
                 queryParams.Add("concurrency", concurrency == Concurrency.FIRST_WRITE ? "first-write" : "last-write");
            }
            if(consistency != null)
            {
                queryParams.Add("consistency", consistency == Consistency.EVENTUAL ? "eventual" : "strong");
            }
            if(retryInterval != null && retryInterval.HasValue)
            {
                queryParams.Add("retryInterval", retryInterval.Value.ToString());
            }
            if(retryThreshold != null && retryThreshold.HasValue)
            {
                queryParams.Add("retryThreshold", retryThreshold.Value.ToString());
            }
            if(retryPattern != null)
            {
                queryParams.Add("retryPattern", retryPattern == RetryPattern.EXPONENTIAL ? "exponential" : "linear");
            }

            var queryString = new FormUrlEncodedContent(queryParams).ReadAsStringAsync().Result;
            
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, $"http://127.0.0.1:{DefaultHttpPort}{StatePath}{storeName}/{key}?{queryString}");

            // if(!string.IsNullOrEmpty(eTag))
            // {
            //     request.Headers.IfMatch.Add(new EntityTagHeaderValue($@"""{eTag}"""));
            // }

            return httpClient.SendAsync(request);
        }
    }
}