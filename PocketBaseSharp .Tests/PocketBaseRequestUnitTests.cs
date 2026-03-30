using FluentAssertions;
using PocketBaseSharp.Models;
using PocketBaseSharp.Models.Files;
using PocketBaseSharp.Tests.TestInfrastructure;
using System.Net;
using System.Text;

namespace PocketBaseSharp.Tests
{
    [TestClass]
    public class PocketBaseRequestUnitTests
    {
        [TestMethod]
        public async Task SendAsync_generic_applies_headers_body_query_and_events()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.QueueResponse(HttpStatusCode.OK, new { code = 200 });

            var client = TestPocketBaseFactory.CreateClient(
                handler,
                TestPocketBaseFactory.CreateValidAuthStore(),
                language: "de-DE");

            Uri? requestUrl = null;
            HttpStatusCode? responseStatus = null;

            client.BeforeSend += (_, args) =>
            {
                requestUrl = args.Url;
                args.HttpRequest.Headers.Add("X-Before", "true");
                return args.HttpRequest;
            };

            client.AfterSend += (_, args) =>
            {
                responseStatus = args.HttpResponse?.StatusCode;
            };

            var result = await client.SendAsync<ApiHealthModel>(
                "/api/health",
                HttpMethod.Post,
                query: new Dictionary<string, object?>
                {
                    ["page"] = 1,
                    ["filter"] = new[] { "a", "b" },
                },
                body: new Dictionary<string, object>
                {
                    ["enabled"] = true,
                });

            result.IsSuccess.Should().BeTrue();

            var request = handler.Requests.Should().ContainSingle().Subject;
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri.Should().Be(new Uri("https://example.com/api/health?page=1&filter=a&filter=b"));
            request.Headers.GetValues("Authorization").Single().Should().Be(TestPocketBaseFactory.ValidToken);
            request.Headers.GetValues("Accept-Language").Single().Should().Be("de-DE");
            request.Headers.GetValues("X-Before").Single().Should().Be("true");
            requestUrl.Should().Be(new Uri("https://example.com/api/health?page=1&filter=a&filter=b"));
            responseStatus.Should().Be(HttpStatusCode.OK);

            using var payload = await TestPocketBaseFactory.ReadJsonAsync(request.Content);
            payload.RootElement.GetProperty("enabled").GetBoolean().Should().BeTrue();
        }

        [TestMethod]
        public void Send_respects_explicit_authorization_and_language_headers()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.QueueResponse(HttpStatusCode.OK);

            var client = TestPocketBaseFactory.CreateClient(
                handler,
                TestPocketBaseFactory.CreateValidAuthStore(),
                language: "de-DE");

            var result = client.Send(
                "/api/health",
                HttpMethod.Get,
                headers: new Dictionary<string, string>
                {
                    ["Authorization"] = "custom-token",
                    ["Accept-Language"] = "es-ES",
                });

            result.IsSuccess.Should().BeTrue();

            var request = handler.Requests.Should().ContainSingle().Subject;
            request.Headers.GetValues("Authorization").Single().Should().Be("custom-token");
            request.Headers.GetValues("Accept-Language").Single().Should().Be("es-ES");
        }

        [TestMethod]
        public async Task Send_methods_return_failures_for_status_codes_and_http_request_exceptions()
        {
            var badStatusHandler = new RecordingHttpMessageHandler();
            badStatusHandler.QueueResponse(HttpStatusCode.BadRequest);

            var badStatusClient = TestPocketBaseFactory.CreateClient(badStatusHandler);
            var badStatusResult = badStatusClient.Send("/api/health", HttpMethod.Get);

            badStatusResult.IsFailed.Should().BeTrue();
            TestResultAssertions.ShouldHaveStatusCode(badStatusResult, HttpStatusCode.BadRequest);

            var exceptionHandler = new RecordingHttpMessageHandler();
            exceptionHandler.QueueException(new HttpRequestException("boom", null, HttpStatusCode.BadGateway));

            var exceptionClient = TestPocketBaseFactory.CreateClient(exceptionHandler);
            var exceptionResult = await exceptionClient.SendAsync<ApiHealthModel>("/api/health", HttpMethod.Get);

            exceptionResult.IsFailed.Should().BeTrue();
            TestResultAssertions.ShouldHaveStatusCode(exceptionResult, HttpStatusCode.BadGateway);
        }

        [TestMethod]
        public async Task Generic_send_methods_return_default_value_for_no_content_responses()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.QueueResponse(HttpStatusCode.NoContent);
            handler.QueueResponse(HttpStatusCode.NoContent);

            var client = TestPocketBaseFactory.CreateClient(handler);

            var syncResult = client.Send<BackupModel>("/api/backups", HttpMethod.Post);
            var asyncResult = await client.SendAsync<BackupModel>("/api/backups", HttpMethod.Post);

            syncResult.IsSuccess.Should().BeTrue();
            syncResult.Value.Should().BeNull();

            asyncResult.IsSuccess.Should().BeTrue();
            asyncResult.Value.Should().BeNull();
        }

        [TestMethod]
        public async Task Send_builds_multipart_content_for_files_and_collection_body_values()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.QueueResponse(HttpStatusCode.OK);

            var client = TestPocketBaseFactory.CreateClient(handler);

            var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
            await File.WriteAllTextAsync(filePath, "payload");

            try
            {
                var result = client.Send(
                    "/api/upload",
                    HttpMethod.Post,
                    body: new Dictionary<string, object>
                    {
                        ["caption"] = "hello",
                        ["tags"] = new List<string?> { "alpha", null, "omega" },
                        ["skip"] = "",
                    },
                    files: new IFile[]
                    {
                        new FilepathFile(filePath)
                        {
                            FieldName = "document",
                            FileName = "note.txt",
                        },
                        new FilepathFile("missing-file")
                        {
                            FieldName = "ignored",
                            FileName = "ignored.txt",
                        },
                    });

                result.IsSuccess.Should().BeTrue();

                var request = handler.Requests.Should().ContainSingle().Subject;
                var form = request.Content.Should().BeOfType<MultipartFormDataContent>().Subject;
                var parts = form.ToList();

                parts.Should().HaveCount(4);

                var filePart = parts.Single(part => part.Headers.ContentDisposition?.Name?.Trim('"') == "document");
                filePart.Headers.ContentType?.MediaType.Should().Be("text/plain");

                var captionPart = parts.Single(part => part.Headers.ContentDisposition?.Name?.Trim('"') == "caption");
                var tagZeroPart = parts.Single(part => part.Headers.ContentDisposition?.Name?.Trim('"') == "tags0");
                var tagTwoPart = parts.Single(part => part.Headers.ContentDisposition?.Name?.Trim('"') == "tags2");

                (await captionPart.ReadAsStringAsync()).Should().Be("hello");
                (await tagZeroPart.ReadAsStringAsync()).Should().Be("alpha");
                (await tagTwoPart.ReadAsStringAsync()).Should().Be("omega");
            }
            finally
            {
                foreach (var request in handler.Requests)
                {
                    request.Dispose();
                }

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        [TestMethod]
        public async Task GetStreamAsync_and_GetFileTokenAsync_cover_success_and_failure_paths()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.QueueResponse(HttpStatusCode.OK, new StringContent("stream payload", Encoding.UTF8, "text/plain"));
            handler.QueueResponse(HttpStatusCode.OK, new { token = "file-token" });
            handler.QueueResponse(HttpStatusCode.OK, new { token = "" });

            var client = TestPocketBaseFactory.CreateClient(handler);

            var streamResult = await client.GetStreamAsync("/api/files/demo.txt", new Dictionary<string, object?>
            {
                ["download"] = 1,
            });

            streamResult.IsSuccess.Should().BeTrue();
            using (var reader = new StreamReader(streamResult.Value))
            {
                (await reader.ReadToEndAsync()).Should().Be("stream payload");
            }

            var streamRequest = handler.Requests[0];
            streamRequest.RequestUri.Should().Be(new Uri("https://example.com/api/files/demo.txt?download=1"));

            var tokenResult = await client.GetFileTokenAsync();
            tokenResult.IsSuccess.Should().BeTrue();
            tokenResult.Value.Should().Be("file-token");

            var failedTokenResult = await client.GetFileTokenAsync();
            failedTokenResult.IsFailed.Should().BeTrue();
            failedTokenResult.Errors.Should().ContainSingle(error => error.Message == "Failed to obtain file token");
        }
    }
}
