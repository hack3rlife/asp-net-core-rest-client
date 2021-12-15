using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CSharpRestClient.Constants;
using CSharpRestClient.Extensions;

namespace CSharpRestClient
{
    public abstract class CSharpRestClient
    {
        /// <summary>
        /// The <see cref="HttpClient"/> instance
        /// </summary>
        /// <remarks>https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/</remarks>
        private readonly Lazy<HttpClient> _lazyHttpClient = new Lazy<HttpClient>(() => new HttpClient(HttpClientHandler));

        /// <summary>
        /// To manage  <see cref="HttpClientHandler"/> properties
        /// </summary>
        /// <see cref="https://thomaslevesque.com/2016/12/08/fun-with-the-httpclient-pipeline/"/>
        /// <see cref="https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/http-message-handlers"/>
        /// <see cref="https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/httpclient-message-handlers"/>
        protected static HttpClientHandler HttpClientHandler = new HttpClientHandler();

        /// <summary>
        /// The actual <see cref="HttpClient"/> singleton instance
        /// </summary>
        /// <remarks>https://docs.microsoft.com/en-us/dotnet/framework/performance/lazy-initialization</remarks>
        public HttpClient HttpClientInstance => _lazyHttpClient.Value;

        /// <summary>
        /// Establishes the expected time before the HTTP Request get cancelled.
        /// </summary>
        public TimeSpan? TimeOut { get; protected set; }

        /// <summary>
        /// To manage <see cref="JsonSerializer"/> properties
        /// </summary>
        /// <see cref="https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_JsonSerializer.htm"/>
        /// <see cref="https://www.newtonsoft.com/json/help/html/SerializingJSON.htm"/>
        protected static JsonSerializer JsonSerializer { get; set; } = new JsonSerializer();

        /// <summary>
        /// Json serializer settings to use for request and response serialization. Defaults to
        /// <see cref="JsonSerializerSettings"/>.
        /// </summary>
        protected static JsonSerializerSettings JsonSerializerSettings { get; set; } = new JsonSerializerSettings();

        /// <summary>
        /// Encoding type for the <see cref="StreamWriter"/> when we do serialize json into a stream
        /// </summary>
        protected static Encoding Encoding { get; set; } = Encoding = Encoding.UTF8;

        /// <summary>
        /// The service base uriString 
        /// </summary>
        public Uri BaseUrl { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseUrl"></param>
        protected CSharpRestClient(string baseUrl)
        {
            if (string.IsNullOrEmpty(baseUrl))
                throw new ArgumentNullException(nameof(baseUrl));

            BaseUrl = new Uri(baseUrl);
        }


        /// <summary>
        /// Sends an HTTP request as a synchronous operation to the specified <paramref name="uriString"/> and returns an <see cref="HttpRequestMessage"/>.  
        /// </summary>
        /// <param name="uriString">The uriString the request is sent to</param>
        /// <param name="httpMethod">The <see cref="HttpMethod"/> to be requested</param>
        /// <param name="content">The payload for the request (if needed)</param>
        /// <param name="preRequestHandler">Configures any<see cref="HttpRequestMessage"/> property (if needed) before the request is sent</param>
        /// <returns>An <see cref="HttpRequestMessage"/></returns>
        public HttpResponseMessage Send(
            string uriString,
            HttpMethod httpMethod,
            object content = null,
            Action<HttpRequestMessage> preRequestHandler = null)
        {
            return SendAsyncInternal(uriString, httpMethod, content, preRequestHandler).Result;
        }

        /// <summary>
        /// Send an HTTP Request as an asynchronous operation to the specified <paramref name="uriString"/> and deserialize from Json to <see cref="Task"/>
        /// </summary>
        /// <param name="uriString">The uriString the request is sent to.</param>
        /// <param name="httpMethod">The <see cref="HttpMethod"/> to be requested</param>
        /// <param name="preRequestHandler">Configures any<see cref="HttpRequestMessage"/> property (if needed) before the request is sent</param>
        /// <param name="content">The content to be sent to the server (a.k.a. the request body)</param>
        /// <returns>The <see cref="HttpResponseMessage"/> object representing the asynchronous operation.</returns>
        /// <remarks>In case <see cref="TimeOut"/> is not null, the asynchronous operation will be cancelled after <see cref="TimeOut"/> value is expired.</remarks>
        public async Task<HttpResponseMessage> SendAsync(
            string uriString,
            HttpMethod httpMethod,
            object content = null,
            Action<HttpRequestMessage> preRequestHandler = null)
        {
            return await SendAsyncInternal(uriString, httpMethod, content, preRequestHandler);
        }

        /// <summary>
        /// Sends an HTTP request as a synchronous operation to the <paramref name="uriString"/> and deserialize the json response to an object of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">The type of the object to be returned</typeparam>
        /// <param name="uriString">The uriString the request is sent to.</param>
        /// <param name="httpMethod">The <see cref="HttpMethod"/> to be executed</param>
        /// <param name="content"></param>
        /// <param name="preRequestHandler"></param>
        /// <returns>An <typeparamref name="T"/></returns>
        public T Send<T>(
            string uriString,
            HttpMethod httpMethod,
            object content = null,
            Action<HttpRequestMessage> preRequestHandler = null)
        {
            var response = SendAsyncInternal(uriString, httpMethod, content, preRequestHandler).Result;
            return response.Read<T>();
        }

        /// <summary>
        /// Sends an HTTP request as an asynchronous operation to the specified <paramref name="uriString"/> and deserialized from Json to <see cref="Task{TResult}"/>
        /// </summary>
        /// <typeparam name="T">The <see cref="Task{T}"/> object representing the asynchronous operation.</typeparam>
        /// <param name="uriString">The uriString the request is sent to.</param>
        /// <param name="httpMethod">The <see cref="HttpMethod"/> to be requested</param>
        /// <param name="preRequestHandler">Configures any<see cref="HttpRequestMessage"/> property (if needed) before the request is sent</param>
        /// <param name="content">The content to be sent to the server (a.k.a. the request body)</param>
        /// <returns>The <see cref="Task{T}"/> object representing the asynchronous operation.</returns>
        /// <remarks>In case <see cref="TimeOut"/> is not null, the asynchronous operation will be cancelled after <see cref="TimeOut"/> value is expired.</remarks>
        public async Task<T> SendAsync<T>(
            string uriString,
            HttpMethod httpMethod,
            object content = null,
            Action<HttpRequestMessage> preRequestHandler = null)
        {
            var response = await SendAsyncInternal(uriString, httpMethod, content, preRequestHandler);
            var stream = await response.Content.ReadAsStreamAsync();

            return DeserializeJsonFromStream<T>(stream);
        }



        ///<inheritdoc cref="Send"/>
        public HttpResponseMessage Get(
            string uriString,
            Action<HttpRequestMessage> preRequestHandler = null)
        {
            return Send(uriString, HttpMethod.Get, null, preRequestHandler);
        }

        ///<inheritdoc cref="SendAsync"/>
        public async Task<HttpResponseMessage> GetAsync(
            string uriString,
            Action<HttpRequestMessage> preRequestHandler = null)
        {
            return await SendAsync(uriString, HttpMethod.Get, null, preRequestHandler);
        }

        ///<inheritdoc cref="SendAsync{T}"/>
        public async Task<T> GetAsync<T>(
            string uriString,
            Action<HttpRequestMessage> preRequestHandler = null)
        {
            return await SendAsync<T>(uriString, HttpMethod.Get, null, preRequestHandler);
        }


        ///<inheritdoc cref="Send"/>
        public HttpResponseMessage Post(
            string uriString,
            object content = null,
            Action<HttpRequestMessage> preRequestHandler = null)
        {
            return Send(uriString, HttpMethod.Post, content, preRequestHandler);
        }

        ///<inheritdoc cref="SendAsync"/>
        public async Task<HttpResponseMessage> PostAsync(
            string uriString,
            object content = null,
            Action<HttpRequestMessage> preRequestHandler = null)
        {
            return await SendAsync(uriString, HttpMethod.Post, content, preRequestHandler);
        }

        /// <inheritdoc cref="SendAsync{T}"/>
        public async Task<T> PostAsync<T>(
            string uriString,
            object content = null,
            Action<HttpRequestMessage> preRequestHandler = null)
        {
            return await SendAsync<T>(uriString, HttpMethod.Post, content, preRequestHandler);
        }



        ///<inheritdoc cref="Send"/>
        public HttpResponseMessage Put(
            string uriString,
            object content = null,
            Action<HttpRequestMessage> preRequestHandler = null)
        {
            return Send(uriString, HttpMethod.Put, content, preRequestHandler);
        }

        ///<inheritdoc cref="SendAsync"/>
        public async Task<HttpResponseMessage> PutAsync(
            string uriString,
            object content = null,
            Action<HttpRequestMessage> preRequestHandler = null)
        {
            return await SendAsync(uriString, HttpMethod.Put, content, preRequestHandler);
        }

        ///<inheritdoc cref="SendAsync{T}"/>
        public async Task<T> PutAsync<T>(
            string uriString,
            object content = null,
            Action<HttpRequestMessage> preRequestHandler = null)
        {
            return await SendAsync<T>(uriString, HttpMethod.Put, content, preRequestHandler);
        }


        /// <summary>
        /// Send a PUT request
        /// </summary>
        public HttpResponseMessage Patch(
            string uriString,
            object content = null,
            Action<HttpRequestMessage> preRequestHandler = null)
        {
            return Send(uriString, new HttpMethod("PATCH"), preRequestHandler);
        }


        ///<inheritdoc cref="Send"/>
        public HttpResponseMessage Delete(
            string uriString,
            object content = null,
            Action<HttpRequestMessage> preRequestHandler = null)
        {
            return Send(uriString, HttpMethod.Delete, content, preRequestHandler);
        }

        ///<inheritdoc cref="SendAsync"/>
        public async Task<HttpResponseMessage> DeleteAsync(
            string uriString,
            object content = null,
            Action<HttpRequestMessage> preRequestHandler = null)
        {
            return await SendAsync(uriString, HttpMethod.Delete, content, preRequestHandler);
        }

        ///<inheritdoc cref="SendAsync{T}"/>
        public async Task<T> DeleteAsync<T>(
            string uriString,
            object content = null,
            Action<HttpRequestMessage> preRequestHandler = null)
        {
            return await SendAsync<T>(uriString, HttpMethod.Delete, content, preRequestHandler);
        }


        /// <summary>
        /// Sends an HTTP request as an asynchronous operation to the specified <paramref name="uriString"/> and returns an <see cref="HttpResponseMessage"/>
        /// </summary>
        /// <param name="uriString">The uriString the request is sent to.</param>
        /// <param name="httpMethod">The <see cref="HttpMethod"/> to be requested</param>
        /// <param name="content">The content to be sent to the server (a.k.a. the request body)</param>
        /// <param name="preRequestHandler">Configures any<see cref="HttpRequestMessage"/> property (if needed) before the request is sent</param>
        /// <returns>The <see cref="HttpResponseMessage"/> object representing the asynchronous operation.</returns>
        private async Task<HttpResponseMessage> SendAsyncInternal(
            string uriString,
            HttpMethod httpMethod,
            object content = null,
            Action<HttpRequestMessage> preRequestHandler = null)
        {
            var uri = new Uri(BaseUrl, uriString);

            using var request = new HttpRequestMessage(httpMethod, uri);
            if (content != null)
            {
                request.Content = CreateHttpContent(content);
            }

            preRequestHandler?.Invoke(request);

            var cts = GetCancellationTokenSource();
            return await HttpClientInstance.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="stream"></param>
        /// <see cref="https://www.newtonsoft.com/json/help/html/Performance.htm"/>
        private static void SerializeJsonIntoStream(object value, Stream stream)
        {
            const int bufferSize = 1024;
            using var sw = new StreamWriter(stream, Encoding, bufferSize, true);
            using var jtw = new JsonTextWriter(sw) { Formatting = Formatting.None };
            JsonSerializer.Serialize(jtw, value);
            jtw.Flush();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream"></param>
        /// <returns></returns>
        private static T DeserializeJsonFromStream<T>(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using var streamReader = new StreamReader(stream);
            using var jsonTextReader = new JsonTextReader(streamReader);
            var result = JsonSerializer.Deserialize<T>(jsonTextReader);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private async Task<string> StreamToStringAsync(Stream stream)
        {
            if (stream == null)
            {
                return null;
            }

            using var sr = new StreamReader(stream);
            return await sr.ReadToEndAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private static HttpContent CreateHttpContent(object content)
        {
            if (content == null)
                return null;

            var ms = new MemoryStream();
            SerializeJsonIntoStream(content, ms);
            ms.Seek(0, SeekOrigin.Begin);

            var httpContent = new StreamContent(ms);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue(Headers.ContentType);

            return httpContent;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CancellationTokenSource GetCancellationTokenSource()
        {
            return TimeOut == null
                ? new CancellationTokenSource()
                : new CancellationTokenSource(TimeOut.Value);
        }

    }
}
