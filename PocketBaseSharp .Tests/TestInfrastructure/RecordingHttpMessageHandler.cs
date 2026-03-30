using FluentAssertions;
using FluentResults;
using PocketBaseSharp.Models;
using System.Net;
using System.Text;
using System.Text.Json;

namespace PocketBaseSharp.Tests.TestInfrastructure
{
    internal sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, CancellationToken, HttpResponseMessage>> responders = new();

        public List<HttpRequestMessage> Requests { get; } = new();

        public void QueueResponse(HttpStatusCode statusCode, object? body = null, string mediaType = "application/json")
        {
            QueueResponse((_, _) => TestHttpResponse.Create(statusCode, body, mediaType));
        }

        public void QueueResponse(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            QueueResponse((request, _) => responder(request));
        }

        public void QueueResponse(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder)
        {
            responders.Enqueue(responder);
        }

        public void QueueException(Exception exception)
        {
            QueueResponse((_, _) => throw exception);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(SendCore(request, cancellationToken));
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return SendCore(request, cancellationToken);
        }

        private HttpResponseMessage SendCore(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);

            if (responders.Count == 0)
            {
                throw new InvalidOperationException("No response has been queued for the current test.");
            }

            return responders.Dequeue()(request, cancellationToken);
        }
    }

    internal static class TestHttpResponse
    {
        public static HttpResponseMessage Create(HttpStatusCode statusCode, object? body = null, string mediaType = "application/json")
        {
            var response = new HttpResponseMessage(statusCode);

            if (body is null)
            {
                return response;
            }

            if (body is HttpContent content)
            {
                response.Content = content;
                return response;
            }

            var payload = body is string text && mediaType != "application/json"
                ? text
                : JsonSerializer.Serialize(body);

            response.Content = new StringContent(payload, Encoding.UTF8, mediaType);
            return response;
        }
    }

    internal static class TestPocketBaseFactory
    {
        internal const string ValidToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE4OTM0NTI0NjF9.yVr-4JxMz6qUf1MIlGx8iW2ktUrQaFecjY_TMm7Bo4o";

        public static PocketBase CreateClient(RecordingHttpMessageHandler handler, AuthStore? authStore = null, string language = "en-US")
        {
            return new PocketBase("https://example.com", authStore, language, new HttpClient(handler));
        }

        public static AuthStore CreateValidAuthStore(IBaseModel? model = null)
        {
            var store = new AuthStore();
            store.Save(ValidToken, model);
            return store;
        }

        public static async Task<JsonDocument> ReadJsonAsync(HttpContent? content)
        {
            var json = content is null ? "{}" : await content.ReadAsStringAsync();
            return JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        }
    }

    internal static class TestResultAssertions
    {
        public static void ShouldHaveStatusCode(IResultBase result, HttpStatusCode statusCode)
        {
            result.Errors.Any(error =>
                error.Metadata.TryGetValue("Statuscode", out var actualStatusCode)
                && actualStatusCode is int actualValue
                && actualValue == (int)statusCode).Should().BeTrue();
        }
    }
}
