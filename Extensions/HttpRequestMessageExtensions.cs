using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace CSharpRestClient.Extensions
{
    public static class HttpRequestMessageExtensions
    {
        /// <summary>
        /// Removes and Add a new value for <see cref="HttpRequestMessage"/> Authorization Header
        /// </summary>
        /// <param name="httpRequestMessage">The <see cref="HttpRequestMessage"/> to be updated</param>
        /// <param name="authorizationValue"></param>
        public static void AddAuthorizationHeader(this HttpRequestMessage httpRequestMessage, string authorizationValue)
        {
            httpRequestMessage.Headers.Remove("Authorization");
            httpRequestMessage.Headers.Authorization = AuthenticationHeaderValue.Parse(authorizationValue);
        }

        /// <summary>
        /// Removes and Add a new value for <see cref="HttpRequestMessage"/> User-Agent Header
        /// </summary>
        /// <param name="httpRequestMessage">The <see cref="HttpRequestMessage"/> to be updated</param>
        /// <param name="userAgent">The User-Agent Value</param>
        public static void AddUserAgent(this HttpRequestMessage httpRequestMessage, string userAgent)
        {
            httpRequestMessage.Headers.Remove("User-Agent");
            httpRequestMessage.Headers.Add("User-Agent", userAgent);
        }
    }
}
