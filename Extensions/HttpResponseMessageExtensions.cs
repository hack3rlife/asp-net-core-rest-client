using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CSharpRestClient.Extensions
{
    public static class HttpResponseMessageExtensions
    {
        /// <summary>
        /// Deserialize the JSON content of an HTTP response into an object of the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="response"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static T Read<T>(this HttpResponseMessage response, JsonSerializerSettings settings = null)
        {
            return settings == null
                ? JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result)
                : JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result, settings);
        }

        /// <summary>
        /// Deserialize the JSON content of an HTTP response into an object of the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="response"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static async Task<T> ReadAsync<T>(this HttpResponseMessage response, JsonSerializerSettings settings = null)
        {
            return settings == null
                ? JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync())
                : JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync(), settings);
        }

        /// <summary>
        /// Perform assertions on an HTTP response inline.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="assertion"></param>
        /// <returns></returns>
        public static HttpResponseMessage AssertOnResponse(this HttpResponseMessage response, Action<HttpResponseMessage> assertion)
        {
            assertion(response);
            return response;
        }
    }
}
