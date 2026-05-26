using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace ApiWindowsService
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private string _bearerToken;
        private readonly string _loginUrl;
        private readonly string _pushUrl;
        private readonly string _basicAuth;

        public ApiClient()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            _loginUrl = ConfigurationManager.AppSettings["LoginApiUrl"];
            _pushUrl = ConfigurationManager.AppSettings["PushApiUrl"];
            _basicAuth = ConfigurationManager.AppSettings["BasicAuthValue"];
        }

        public async Task LoginAsync()
        {
            Console.WriteLine($"Logging in to {_loginUrl}...");

            var request = new HttpRequestMessage(HttpMethod.Post, _loginUrl);
            request.Headers.Add("Authorization", _basicAuth);

            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credential")
            });

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var loginResponse = DeserializeJson<LoginResponse>(responseBody);

            if (loginResponse?.Token == null)
                throw new Exception("Login response did not contain a token.");

            _bearerToken = loginResponse.Token;
            Console.WriteLine("Login successful, token acquired.");
        }

        public async Task PushDataAsync()
        {
            if (string.IsNullOrEmpty(_bearerToken))
            {
                Console.WriteLine("No bearer token available, skipping push.");
                return;
            }

            Console.WriteLine($"Pushing data to {_pushUrl}...");

            var pushPayload = new PushRequest
            {
                Timestamp = DateTime.UtcNow,
                Message = "Data from API Client"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, _pushUrl)
            {
                Content = new StringContent(
                    SerializeJson(pushPayload),
                    Encoding.UTF8,
                    "application/json"
                )
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Push successful. Response: {responseBody}");
        }

        private string SerializeJson<T>(T obj)
        {
            using var ms = new MemoryStream();
            var serializer = new DataContractJsonSerializer(typeof(T));
            serializer.WriteObject(ms, obj);
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private T DeserializeJson<T>(string json)
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var serializer = new DataContractJsonSerializer(typeof(T));
            return (T)serializer.ReadObject(ms);
        }
    }

    [DataContract]
    public class LoginResponse
    {
        [DataMember(Name = "token")]
        public string Token { get; set; }
    }

    [DataContract]
    public class PushRequest
    {
        [DataMember(Name = "timestamp")]
        public DateTime Timestamp { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }
    }
}
