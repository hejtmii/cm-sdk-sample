using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;

namespace Import
{
    public sealed class Client
    {
        private readonly string _projectId = ConfigurationManager.AppSettings["ProjectId"];
        private readonly HttpClient _httpClient;

        public Client()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://manage.kenticocloud.com")
            };
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ConfigurationManager.AppSettings["ContentManagementApiKey"]);
        }

        public JToken Post(string requestUri, object data)
        {
            return Send(HttpMethod.Post, requestUri, data);
        }

        public JToken PostFile(string requestUri, string filePath)
        {
            return SendFile(HttpMethod.Post, requestUri, filePath);
        }

        public JToken Put(string requestUri, object data)
        {
            return Send(HttpMethod.Put, requestUri, data);
        }

        public void Delete(string requestUri)
        {
            Send(HttpMethod.Delete, requestUri);
        }

        public JToken Get(string requestUri)
        {
            return Send(HttpMethod.Get, requestUri);
        }

        private JToken Send(HttpMethod method, string requestUri, object data = null)
        {
            var request = new HttpRequestMessage(method, $"projects/{_projectId}/{requestUri}");

            if (data != null)
            {
                var requestContent = JToken.FromObject(data).ToString(Formatting.Indented);
                request.Content = new StringContent(requestContent, Encoding.UTF8, "application/json");
            }

            var response = _httpClient.SendAsync(request).Result;
            while ((int)response.StatusCode == 429)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
                response = _httpClient.SendAsync(request).Result;
            }
            var responseContent = response.Content.ReadAsStringAsync().Result;

            response.EnsureSuccessStatusCode();

            if (string.IsNullOrEmpty(responseContent))
            {
                return null;
            }

            return JToken.Parse(responseContent);
        }

        private JToken SendFile(HttpMethod method, string requestUri, string filePath)
        {
            var request = new HttpRequestMessage(method, $"projects/{_projectId}/{requestUri}");

            var content = new ByteArrayContent(File.ReadAllBytes(filePath));
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
            request.Content = content;

            var response = _httpClient.SendAsync(request).Result;
            while ((int)response.StatusCode == 429)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
                response = _httpClient.SendAsync(request).Result;
            }
            var responseContent = response.Content.ReadAsStringAsync().Result;

            response.EnsureSuccessStatusCode();

            if (string.IsNullOrEmpty(responseContent))
            {
                return null;
            }

            return JToken.Parse(responseContent);
        }
    }
}
