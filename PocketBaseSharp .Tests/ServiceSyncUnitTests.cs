using FluentAssertions;
using PocketBaseSharp.Enum;
using PocketBaseSharp.Extensions;
using PocketBaseSharp.Models;
using PocketBaseSharp.Models.Collection;
using PocketBaseSharp.Tests.TestInfrastructure;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;

namespace PocketBaseSharp.Tests
{
    [TestClass]
    public class ServiceSyncUnitTests
    {
        [TestMethod]
        public async Task Collection_and_settings_sync_methods_send_expected_requests()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.QueueResponse(HttpStatusCode.OK);
            handler.QueueResponse(HttpStatusCode.OK, new CollectionModel { Name = "posts" });
            handler.QueueResponse(HttpStatusCode.NoContent);
            handler.QueueResponse(HttpStatusCode.OK, new Dictionary<string, object> { ["smtpEnabled"] = true });
            handler.QueueResponse(HttpStatusCode.OK, new Dictionary<string, object> { ["smtpEnabled"] = false });
            handler.QueueResponse(HttpStatusCode.OK);
            handler.QueueResponse(HttpStatusCode.OK);

            var client = TestPocketBaseFactory.CreateClient(handler);

            var importResult = client.Collections.Import(Array.Empty<CollectionModel>(), deleteMissing: true);
            var collectionResult = client.Collections.GetByName("posts");
            var deleteResult = client.Collections.Delete("posts");
            var settingsResult = client.Settings.GetAll();
            var updateSettingsResult = client.Settings.Update(new Dictionary<string, object> { ["flag"] = true });
            var testS3Result = client.Settings.TestS3(new Dictionary<string, object> { ["bucket"] = "demo" });
            var testEmailResult = client.Settings.TestEmail("user@example.com", "verification", new Dictionary<string, object> { ["subject"] = "Test" });

            importResult.IsSuccess.Should().BeTrue();
            collectionResult.IsSuccess.Should().BeTrue();
            collectionResult.Value.Name.Should().Be("posts");
            deleteResult.IsSuccess.Should().BeTrue();
            settingsResult.IsSuccess.Should().BeTrue();
            settingsResult.Value.Should().ContainKey("smtpEnabled");
            updateSettingsResult.IsSuccess.Should().BeTrue();
            testS3Result.IsSuccess.Should().BeTrue();
            testEmailResult.IsSuccess.Should().BeTrue();

            handler.Requests.Should().HaveCount(7);

            using (var importBody = await TestPocketBaseFactory.ReadJsonAsync(handler.Requests[0].Content))
            {
                handler.Requests[0].Method.Should().Be(HttpMethod.Put);
                handler.Requests[0].RequestUri.Should().Be(new Uri("https://example.com/api/collections/import"));
                importBody.RootElement.GetProperty("deleteMissing").GetBoolean().Should().BeTrue();
                importBody.RootElement.GetProperty("collections").GetArrayLength().Should().Be(0);
            }

            handler.Requests[1].Method.Should().Be(HttpMethod.Get);
            handler.Requests[1].RequestUri.Should().Be(new Uri("https://example.com/api/collections/posts"));

            handler.Requests[2].Method.Should().Be(HttpMethod.Delete);
            handler.Requests[2].RequestUri.Should().Be(new Uri("https://example.com/api/collections/posts"));

            handler.Requests[3].Method.Should().Be(HttpMethod.Get);
            handler.Requests[3].RequestUri.Should().Be(new Uri("https://example.com/api/settings"));

            using (var settingsBody = await TestPocketBaseFactory.ReadJsonAsync(handler.Requests[4].Content))
            {
                handler.Requests[4].Method.Should().Be(HttpMethod.Patch);
                settingsBody.RootElement.GetProperty("flag").GetBoolean().Should().BeTrue();
            }

            handler.Requests[5].Method.Should().Be(HttpMethod.Post);
            handler.Requests[5].RequestUri.Should().Be(new Uri("https://example.com/api/settings/test/s3"));

            handler.Requests[6].Method.Should().Be(HttpMethod.Post);
            handler.Requests[6].RequestUri.Should().Be(new Uri("https://example.com/api/settings/test/email?email=user%40example.com&template=verification"));
        }

        [TestMethod]
        public void Collection_get_full_list_sync_requests_multiple_pages()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.QueueResponse(HttpStatusCode.OK, new
            {
                page = 1,
                perPage = 2,
                totalItems = 3,
                items = new[]
                {
                    new { name = "alpha" },
                    new { name = "beta" },
                },
            });
            handler.QueueResponse(HttpStatusCode.OK, new
            {
                page = 2,
                perPage = 2,
                totalItems = 3,
                items = new[]
                {
                    new { name = "gamma" },
                },
            });

            var client = TestPocketBaseFactory.CreateClient(handler);
            var result = client.Collections.GetFullList(batch: 2);

            result.IsSuccess.Should().BeTrue();
            result.Value.Select(item => item.Name).Should().Equal("alpha", "beta", "gamma");

            handler.Requests.Select(request => request.RequestUri!.ToString()).Should().Equal(
                "https://example.com/api/collections?page=1&perPage=2",
                "https://example.com/api/collections?page=2&perPage=2");
        }

        [TestMethod]
        public async Task Record_sync_crud_upload_and_download_send_expected_requests()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.QueueResponse(HttpStatusCode.OK, new { id = "record-1", name = "Created", is_done = false });
            handler.QueueResponse(HttpStatusCode.OK, new { id = "record-1", name = "Created", is_done = false });
            handler.QueueResponse(HttpStatusCode.OK, new { id = "record-1", name = "Created", is_done = true });
            handler.QueueResponse(HttpStatusCode.OK, new
            {
                page = 2,
                perPage = 1,
                totalItems = 1,
                items = new[] { new { id = "record-1", name = "Created", is_done = true } },
            });
            handler.QueueResponse(HttpStatusCode.OK, new
            {
                page = 1,
                perPage = 1,
                totalItems = 2,
                items = new[] { new { id = "record-1", name = "Created", is_done = true } },
            });
            handler.QueueResponse(HttpStatusCode.OK, new
            {
                page = 2,
                perPage = 1,
                totalItems = 2,
                items = Array.Empty<object>(),
            });
            handler.QueueResponse(HttpStatusCode.OK);
            handler.QueueResponse(HttpStatusCode.OK, new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("downloaded"))));

            var client = TestPocketBaseFactory.CreateClient(handler);
            var records = client.Collection("entry");

            var createResult = records.Create(new TestRecord { Id = "ignored", name = "Created", is_done = false }, expand: "owner");
            var getResult = records.GetOne<TestRecord>("record/1");

            var recordToUpdate = new TestRecord { Id = "record/1", name = "Created", is_done = true };
            var updateResult = records.Update(recordToUpdate);
            var listResult = records.List<TestRecord>(page: 2, perPage: 1, filter: "name~'x'", sort: "-created");
            var fullListResult = records.GetFullList<TestRecord>(batch: 1);

            using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes("payload"));
            records.UploadFile("attachment", "demo.txt", uploadStream);

            var downloadResult = await records.DownloadFileAsync("record/1", "demo.txt", ThumbFormat.ResizeToWidth);

            createResult.IsSuccess.Should().BeTrue();
            getResult.IsSuccess.Should().BeTrue();
            updateResult.IsSuccess.Should().BeTrue();
            listResult.IsSuccess.Should().BeTrue();
            fullListResult.IsSuccess.Should().BeTrue();
            fullListResult.Value.Should().ContainSingle(item => item.Id == "record-1");
            downloadResult.IsSuccess.Should().BeTrue();
            downloadResult.Value.CanRead.Should().BeTrue();

            using (var createBody = await TestPocketBaseFactory.ReadJsonAsync(handler.Requests[0].Content))
            {
                handler.Requests[0].RequestUri.Should().Be(new Uri("https://example.com/api/collections/entry/records?expand=owner"));
                createBody.RootElement.TryGetProperty("id", out _).Should().BeFalse();
                createBody.RootElement.GetProperty("name").GetString().Should().Be("Created");
                createBody.RootElement.GetProperty("is_done").GetBoolean().Should().BeFalse();
            }

            handler.Requests[1].RequestUri.Should().Be(new Uri("https://example.com/api/collections/entry/records/record%2f1"));

            using (var updateBody = await TestPocketBaseFactory.ReadJsonAsync(handler.Requests[2].Content))
            {
                handler.Requests[2].Method.Should().Be(HttpMethod.Patch);
                handler.Requests[2].RequestUri.Should().Be(new Uri("https://example.com/api/collections/entry/records/record%2f1"));
                updateBody.RootElement.TryGetProperty("id", out _).Should().BeFalse();
                updateBody.RootElement.GetProperty("is_done").GetBoolean().Should().BeTrue();
            }

            handler.Requests[3].RequestUri.Should().Be(new Uri("https://example.com/api/collections/entry/records?filter=name~%27x%27&page=2&perPage=1&sort=-created"));
            handler.Requests[4].RequestUri.Should().Be(new Uri("https://example.com/api/collections/entry/records?page=1&perPage=1"));
            handler.Requests[5].RequestUri.Should().Be(new Uri("https://example.com/api/collections/entry/records?page=2&perPage=1"));

            var uploadForm = handler.Requests[6].Content.Should().BeOfType<MultipartFormDataContent>().Subject;
            uploadForm.Single(part => part.Headers.ContentDisposition?.Name?.Trim('"') == "attachment");

            handler.Requests[7].RequestUri.Should().Be(new Uri("https://example.com/api/files/entry/record%2f1/demo.txt?thumb=Wx0"));
        }

        [TestMethod]
        public async Task Record_sync_crud_respects_json_property_names_for_pascal_case_models()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.QueueResponse(HttpStatusCode.OK, new { id = "record-1", display_name = "Created", is_done = true, Todo_Id = new[] { "todo-1" } });
            handler.QueueResponse(HttpStatusCode.OK, new { id = "record-1", display_name = "Updated", is_done = false });

            var client = TestPocketBaseFactory.CreateClient(handler);
            var records = client.Collection("entry");

            var createResult = records.Create(new AttributedRecord
            {
                DisplayName = "Created",
                IsDone = true,
                TodoId = "todo-1",
                Ignored = "ignore-me",
            });
            var updateResult = records.Update(new AttributedRecord
            {
                Id = "record-1",
                DisplayName = "Updated",
                IsDone = false,
            });

            createResult.IsSuccess.Should().BeTrue();
            updateResult.IsSuccess.Should().BeTrue();
            createResult.Value.DisplayName.Should().Be("Created");
            createResult.Value.IsDone.Should().BeTrue();
            createResult.Value.TodoId.Should().Be("todo-1");

            using (var createBody = await TestPocketBaseFactory.ReadJsonAsync(handler.Requests[0].Content))
            {
                createBody.RootElement.GetProperty("display_name").GetString().Should().Be("Created");
                createBody.RootElement.GetProperty("is_done").GetBoolean().Should().BeTrue();
                createBody.RootElement.GetProperty("Todo_Id")[0].GetString().Should().Be("todo-1");
                createBody.RootElement.TryGetProperty("displayName", out _).Should().BeFalse();
                createBody.RootElement.TryGetProperty("todoId", out _).Should().BeFalse();
                createBody.RootElement.TryGetProperty("ignored", out _).Should().BeFalse();
            }

            using (var updateBody = await TestPocketBaseFactory.ReadJsonAsync(handler.Requests[1].Content))
            {
                updateBody.RootElement.GetProperty("display_name").GetString().Should().Be("Updated");
                updateBody.RootElement.GetProperty("is_done").GetBoolean().Should().BeFalse();
                updateBody.RootElement.TryGetProperty("Todo_Id", out _).Should().BeFalse();
            }
        }

        [TestMethod]
        public async Task Backup_sync_methods_and_download_token_failure_send_expected_requests()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.QueueResponse(HttpStatusCode.NoContent);
            handler.QueueResponse(HttpStatusCode.NoContent);
            handler.QueueResponse(HttpStatusCode.NoContent);

            var client = TestPocketBaseFactory.CreateClient(handler);

            var createResult = client.Backup.Create("nightly.zip");
            var restoreResult = client.Backup.Restore("nightly.zip");
            var deleteResult = client.Backup.Delete("nightly.zip");

            createResult.IsSuccess.Should().BeTrue();
            createResult.Value.Should().BeNull();
            restoreResult.IsSuccess.Should().BeTrue();
            deleteResult.IsSuccess.Should().BeTrue();

            using (var createBody = await TestPocketBaseFactory.ReadJsonAsync(handler.Requests[0].Content))
            {
                handler.Requests[0].Method.Should().Be(HttpMethod.Post);
                handler.Requests[0].RequestUri.Should().Be(new Uri("https://example.com/api/backups"));
                createBody.RootElement.GetProperty("basename").GetString().Should().Be("nightly.zip");
            }

            handler.Requests[1].Method.Should().Be(HttpMethod.Post);
            handler.Requests[1].RequestUri.Should().Be(new Uri("https://example.com/api/backups/nightly.zip/restore"));

            handler.Requests[2].Method.Should().Be(HttpMethod.Delete);
            handler.Requests[2].RequestUri.Should().Be(new Uri("https://example.com/api/backups/nightly.zip"));

            var tokenFailureHandler = new RecordingHttpMessageHandler();
            tokenFailureHandler.QueueResponse(HttpStatusCode.InternalServerError);

            var tokenFailureClient = TestPocketBaseFactory.CreateClient(tokenFailureHandler);
            var downloadFailure = await tokenFailureClient.Backup.DownloadAsync("nightly.zip");

            downloadFailure.IsFailed.Should().BeTrue();
            downloadFailure.Errors.Should().ContainSingle(error => error.Message == "Failed to obtain file token");
            tokenFailureHandler.Requests.Should().ContainSingle(request => request.RequestUri == new Uri("https://example.com/api/files/token"));
        }

        [TestMethod]
        public void Log_service_sync_methods_send_expected_requests()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.QueueResponse(HttpStatusCode.OK, new { page = 1, perPage = 30, totalItems = 0, totalPages = 0, items = Array.Empty<object>() });
            handler.QueueResponse(HttpStatusCode.OK, new { });
            handler.QueueResponse(HttpStatusCode.OK, Array.Empty<object>());

            var client = TestPocketBaseFactory.CreateClient(handler);

            var listResult = client.Log.GetRequests(filter: "status=200", sort: "-created");
            var requestResult = client.Log.GetRequest("request-1");
            var statsResult = client.Log.GetRequestsStatistics();

            listResult.IsSuccess.Should().BeTrue();
            requestResult.IsSuccess.Should().BeTrue();
            statsResult.IsSuccess.Should().BeTrue();

            handler.Requests[0].RequestUri.Should().Be(new Uri("https://example.com/api/logs/requests?page=1&perPage=30&filter=status%3d200&sort=-created"));
            handler.Requests[1].RequestUri.Should().Be(new Uri("https://example.com/api/logs/requests/request-1"));
            handler.Requests[2].RequestUri.Should().Be(new Uri("https://example.com/api/logs/requests/stats"));
        }

        [TestMethod]
        public async Task Health_and_extension_methods_cover_success_and_failure_paths()
        {
            var missingClient = new PocketBase("https://example.com");

            missingClient.GetCurrentUser().IsFailed.Should().BeTrue();
            (await missingClient.GetCurrentUserAsync()).IsFailed.Should().BeTrue();
            missingClient.GetCurrentAdmin().IsFailed.Should().BeTrue();
            (await missingClient.GetCurrentAdminAsync()).IsFailed.Should().BeTrue();

            var healthHandler = new RecordingHttpMessageHandler();
            healthHandler.QueueResponse(HttpStatusCode.OK, new { code = 200 });

            var healthClient = TestPocketBaseFactory.CreateClient(healthHandler);
            var healthResult = healthClient.Health.CheckHealth();

            healthResult.IsSuccess.Should().BeTrue();
            healthResult.Value.Code.Should().Be(200);

            var userHandler = new RecordingHttpMessageHandler();
            userHandler.QueueResponse(HttpStatusCode.OK, new { id = "user-1", email = "user@example.com" });

            var userClient = TestPocketBaseFactory.CreateClient(
                userHandler,
                TestPocketBaseFactory.CreateValidAuthStore(new UserModel { Id = "user-1" }));

            var currentUser = await userClient.GetCurrentUserAsync();

            currentUser.IsSuccess.Should().BeTrue();
            currentUser.Value.Id.Should().Be("user-1");
            userHandler.Requests.Single().RequestUri.Should().Be(new Uri("https://example.com/api/collections/users/records/user-1"));

            var adminHandler = new RecordingHttpMessageHandler();
            adminHandler.QueueResponse(HttpStatusCode.OK, new { id = "admin-1", email = "admin@example.com" });

            var adminClient = TestPocketBaseFactory.CreateClient(
                adminHandler,
                TestPocketBaseFactory.CreateValidAuthStore(new AdminModel { Id = "admin-1" }));

            var currentAdmin = adminClient.GetCurrentAdmin();

            currentAdmin.IsSuccess.Should().BeTrue();
            currentAdmin.Value.Id.Should().Be("admin-1");
            adminHandler.Requests.Single().RequestUri.Should().Be(new Uri("https://example.com/api/collections/_superusers/records/admin-1"));
        }

        private sealed class TestRecord : BaseModel
        {
            public string? name { get; set; }

            public bool? is_done { get; set; }
        }

        private sealed class AttributedRecord : BaseModel
        {
            [JsonPropertyName("display_name")]
            public string? DisplayName { get; set; }

            [JsonPropertyName("is_done")]
            public bool? IsDone { get; set; }

            [JsonPropertyName("Todo_Id")]
            public List<string>? TodoIds { get; set; }

            [JsonIgnore]
            public string? TodoId
            {
                get => TodoIds is { Count: > 0 } todoIds ? todoIds[0] : null;
                set => TodoIds = string.IsNullOrWhiteSpace(value) ? null : [value];
            }

            [JsonIgnore]
            public string? Ignored { get; set; }
        }
    }
}
