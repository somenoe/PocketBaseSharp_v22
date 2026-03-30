using FluentAssertions;
using FluentResults;
using PocketBaseSharp.Event;
using PocketBaseSharp.Extensions;
using PocketBaseSharp.Models;
using PocketBaseSharp.Models.Auth;
using PocketBaseSharp.Models.Collection;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace PocketBaseSharp.Tests
{
    [TestClass]
    public class LivePocketBaseIntegrationTests
    {
        private const string BaseUrl = "http://127.0.0.1:8090";
        private const string AdminEmail = "admin@admin.com";
        private const string AdminPassword = "demo123456";
        private const string DefaultUserPassword = "Password123!";

        private static long uniqueSuffix = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            using var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
            var health = httpClient.GetFromJsonAsync<ApiHealthModel>("/api/health").GetAwaiter().GetResult();

            health.Should().NotBeNull();
            health!.Code.Should().Be((int)HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task HealthService_and_request_events_succeed_against_live_server()
        {
            var client = new PocketBase(BaseUrl);
            Uri? requestUrl = null;
            HttpStatusCode? responseStatus = null;

            client.BeforeSend += (_, args) =>
            {
                requestUrl = args.Url;
                return args.HttpRequest;
            };

            client.AfterSend += (_, args) =>
            {
                responseStatus = args.HttpResponse?.StatusCode;
            };

            var asyncHealth = await client.Health.CheckHealthAsync();

            asyncHealth.IsSuccess.Should().BeTrue();
            asyncHealth.Value.Code.Should().Be((int)HttpStatusCode.OK);
            requestUrl.Should().Be(new Uri($"{BaseUrl}/api/health"));
            responseStatus.Should().Be(HttpStatusCode.OK);

            var syncHealth = client.Send<ApiHealthModel>("/api/health", HttpMethod.Get);

            syncHealth.IsSuccess.Should().BeTrue();
            syncHealth.Value.Code.Should().Be((int)HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task Collection_and_settings_services_succeed_against_live_server()
        {
            var client = await CreateAdminClientAsync();

            var collections = await client.Collections.ListAsync(perPage: 50);
            var fullCollections = await client.Collections.GetFullListAsync(batch: 50);
            var entryCollection = await client.Collections.GetByNameAsync("entry");
            var settings = await client.Settings.GetAllAsync();

            collections.IsSuccess.Should().BeTrue();
            collections.Value.Items.Should().NotBeNull();
            collections.Value.Items!.Select(item => item.Name).Should().Contain(new[] { "users", "entry", "todos" });
            fullCollections.Select(item => item.Name).Should().Contain(new[] { "users", "entry", "todos" });

            entryCollection.IsSuccess.Should().BeTrue();
            entryCollection.Value.Name.Should().Be("entry");
            entryCollection.Value.Schema.Should().NotBeNull();

            settings.IsSuccess.Should().BeTrue();
            settings.Value.Should().NotBeEmpty();
        }

        [TestMethod]
        public async Task Settings_update_and_diagnostics_are_exercised_against_live_server()
        {
            var client = await CreateAdminClientAsync();

            var updated = await client.Settings.UpdateAsync(new Dictionary<string, object>());
            updated.IsSuccess.Should().BeTrue();
            updated.Value.Should().NotBeEmpty();

            var s3Check = await client.Settings.TestS3Async(new Dictionary<string, object>());
            s3Check.IsFailed.Should().BeTrue();
            AssertHasStatusCode(s3Check, (int)HttpStatusCode.BadRequest);

            var emailCheck = await client.Settings.TestEmailAsync("test@example.com", "verification", new Dictionary<string, object>());
            emailCheck.IsFailed.Should().BeTrue();
            AssertHasStatusCode(emailCheck, (int)HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task Collection_import_and_delete_are_exercised_against_live_server()
        {
            var client = await CreateAdminClientAsync();
            var collectionName = NextCollectionName();

            try
            {
                var import = await client.Collections.ImportAsync(Array.Empty<CollectionModel>());
                import.IsFailed.Should().BeTrue();
                AssertHasStatusCode(import, (int)HttpStatusCode.BadRequest);

                await CreateTemporaryTextCollectionAsync(client, collectionName);

                var fetched = await client.Collections.GetByNameAsync(collectionName);
                fetched.IsSuccess.Should().BeTrue();
                fetched.Value.Name.Should().Be(collectionName);

                var deleted = await client.Collections.DeleteAsync(collectionName);
                deleted.IsSuccess.Should().BeTrue();

                var missing = await client.Collections.GetByNameAsync(collectionName);
                missing.IsFailed.Should().BeTrue();
                AssertHasStatusCode(missing, (int)HttpStatusCode.NotFound);
            }
            finally
            {
                await DeleteCollectionIfExistsAsync(client, collectionName);
            }
        }

        [TestMethod]
        public async Task Record_service_crud_round_trip_succeeds_against_live_server()
        {
            var client = await CreateAdminClientAsync();
            var entryService = client.Collection("entry");
            string? recordId = null;

            try
            {
                client.Collection("entry").Should().BeSameAs(entryService);

                var created = await entryService.CreateAsync(new LiveEntryRecord
                {
                    name = NextUniqueValue("entry"),
                    is_done = false,
                });

                created.IsSuccess.Should().BeTrue();
                recordId = created.Value.Id;
                recordId.Should().NotBeNullOrWhiteSpace();

                var fetched = await entryService.GetOneAsync<LiveEntryRecord>(recordId!);
                fetched.IsSuccess.Should().BeTrue();
                fetched.Value.name.Should().Be(created.Value.name);
                fetched.Value.is_done.Should().BeFalse();

                fetched.Value.is_done = true;
                var updated = await entryService.UpdateAsync(fetched.Value);
                updated.IsSuccess.Should().BeTrue();
                updated.Value.is_done.Should().BeTrue();

                var list = await entryService.ListAsync<LiveEntryRecord>(perPage: 100);
                list.IsSuccess.Should().BeTrue();
                list.Value.Items.Should().Contain(item => item.Id == recordId);

                var fullList = await entryService.GetFullListAsync<LiveEntryRecord>(batch: 100);
                fullList.IsSuccess.Should().BeTrue();
                fullList.Value.Should().Contain(item => item.Id == recordId);
            }
            finally
            {
                await DeleteRecordAsync(client, "entry", recordId);
            }
        }

        [TestMethod]
        public async Task User_service_create_get_update_and_list_succeed_against_live_server()
        {
            var adminClient = await CreateAdminClientAsync();
            UserModel? user = null;

            try
            {
                var created = await adminClient.User.CreateAsync(NextEmailAddress("created"), DefaultUserPassword, DefaultUserPassword);
                created.IsSuccess.Should().BeTrue();

                user = created.Value;
                user.Id.Should().NotBeNullOrWhiteSpace();

                var fetched = await adminClient.User.GetOneAsync(user.Id!);
                fetched.IsSuccess.Should().BeTrue();
                fetched.Value.Email.Should().Be(user.Email);

                var updatedUserName = NextUniqueValue("user");
                var updated = await adminClient.User.UpdateAsync(user.Id!, username: updatedUserName, verified: true);
                updated.IsSuccess.Should().BeTrue();
                updated.Value.UserName.Should().Be(updatedUserName);
                updated.Value.Verified.Should().BeTrue();

                var listed = adminClient.User.List(perPage: 100);
                listed.IsSuccess.Should().BeTrue();
                listed.Value.Items.Should().Contain(item => item.Id == user.Id);

                var fullUsers = await adminClient.User.GetFullListAsync(batch: 100);
                fullUsers.Should().Contain(item => item.Id == user.Id);
            }
            finally
            {
                await DeleteRecordAsync(adminClient, "users", user?.Id);
            }
        }

        [TestMethod]
        public async Task Auth_collection_and_user_auth_flows_succeed_against_live_server()
        {
            var adminClient = await CreateAdminClientAsync();
            var userEmail = NextEmailAddress("auth");
            UserModel? createdUser = null;

            try
            {
                var created = await adminClient.User.CreateAsync(userEmail, DefaultUserPassword, DefaultUserPassword);
                created.IsSuccess.Should().BeTrue();
                createdUser = created.Value;

                var userClient = new PocketBase(BaseUrl);
                var authCollection = userClient.AuthCollection<UserModel>("users");

                var authMethods = await authCollection.GetAuthenticationMethodsAsync();
                authMethods.IsSuccess.Should().BeTrue();
                authMethods.Value.EmailPassword.Should().BeTrue();

                var auth = await authCollection.AuthenticateWithPasswordAsync(userEmail, DefaultUserPassword);
                auth.IsSuccess.Should().BeTrue();
                auth.Value.Record.Should().NotBeNull();
                userClient.AuthStore.IsValid.Should().BeTrue();

                var refresh = await authCollection.RefreshAsync();
                refresh.IsSuccess.Should().BeTrue();

                var currentUser = await userClient.GetCurrentUserAsync();
                currentUser.IsSuccess.Should().BeTrue();
                currentUser.Value.Id.Should().Be(createdUser.Id);

                var verification = await authCollection.RequestVerificationAsync(userEmail);
                verification.IsSuccess.Should().BeTrue();

                var passwordReset = await authCollection.RequestPasswordResetAsync(userEmail);
                passwordReset.IsSuccess.Should().BeTrue();

                var externalAuths = await authCollection.GetExternalAuthenticationMethodsAsync(createdUser.Id!);
                externalAuths.IsSuccess.Should().BeTrue();
                externalAuths.Value.Should().BeEmpty();

                var invalidVerification = await authCollection.ConfirmVerificationAsync("invalid-token");
                invalidVerification.IsFailed.Should().BeTrue();
                AssertHasStatusCode(invalidVerification, (int)HttpStatusCode.BadRequest);

                var invalidPasswordReset = await authCollection.ConfirmPasswordResetAsync("invalid-token", DefaultUserPassword, DefaultUserPassword);
                invalidPasswordReset.IsFailed.Should().BeTrue();
                AssertHasStatusCode(invalidPasswordReset, (int)HttpStatusCode.BadRequest);

                var invalidEmailChange = await authCollection.ConfirmEmailChangeAsync("invalid-token", DefaultUserPassword);
                invalidEmailChange.IsFailed.Should().BeTrue();
                AssertHasStatusCode(invalidEmailChange, (int)HttpStatusCode.BadRequest);
            }
            finally
            {
                await DeleteRecordAsync(adminClient, "users", createdUser?.Id);
            }
        }

        [TestMethod]
        public async Task User_service_wrapper_auth_features_are_exercised_against_live_server()
        {
            var adminClient = await CreateAdminClientAsync();
            var userEmail = NextEmailAddress("wrapper");
            UserModel? createdUser = null;

            try
            {
                var created = await adminClient.User.CreateAsync(userEmail, DefaultUserPassword, DefaultUserPassword);
                created.IsSuccess.Should().BeTrue();
                createdUser = created.Value;

                var userClient = new PocketBase(BaseUrl);

                var authMethods = userClient.User.GetAuthenticationMethods();
                authMethods.IsSuccess.Should().BeTrue();
                authMethods.Value.EmailPassword.Should().BeTrue();

                var auth = await userClient.User.AuthenticateWithPasswordAsync(userEmail, DefaultUserPassword);
                auth.IsSuccess.Should().BeTrue();
                auth.Value.Record.Should().NotBeNull();

                var requestEmailChange = await userClient.User.RequestEmailChangeAsync(NextEmailAddress("changed"));
                requestEmailChange.IsFailed.Should().BeTrue();
                AssertHasStatusCode(requestEmailChange, (int)HttpStatusCode.BadRequest);

                var externalAuths = await userClient.User.GetExternalAuthenticationMethodsAsync(createdUser.Id!);
                externalAuths.IsSuccess.Should().BeTrue();
                externalAuths.Value.Should().BeEmpty();

                var unlink = await userClient.User.UnlinkExternalAuthenticationAsync(createdUser.Id!, "github");
                unlink.IsFailed.Should().BeTrue();
                AssertHasStatusCode(unlink, (int)HttpStatusCode.NotFound);

                var oauth = await userClient.User.AuthenticateViaOAuth2Async("github", "bad", "bad", BaseUrl);
                oauth.IsFailed.Should().BeTrue();
                AssertHasStatusCode(oauth, (int)HttpStatusCode.NotFound);
            }
            finally
            {
                await DeleteRecordAsync(adminClient, "users", createdUser?.Id);
            }
        }

        [TestMethod]
        public async Task Backup_and_file_token_services_succeed_against_live_server()
        {
            var client = await CreateAdminClientAsync();
            string? createdBackupKey = null;

            try
            {
                var before = await client.Backup.GetFullListAsync();
                before.IsSuccess.Should().BeTrue();
                var existingKeys = before.Value.Where(item => item.Key is not null).Select(item => item.Key!).ToHashSet();

                var created = await client.Backup.CreateAsync(NextUniqueValue("sdk-backup") + ".zip");
                created.IsSuccess.Should().BeTrue();

                var after = await client.Backup.GetFullListAsync();
                after.IsSuccess.Should().BeTrue();
                createdBackupKey = after.Value.Select(item => item.Key).OfType<string>().Single(key => !existingKeys.Contains(key));

                var fileToken = await client.GetFileTokenAsync();
                fileToken.IsSuccess.Should().BeTrue();
                fileToken.Value.Should().NotBeNullOrWhiteSpace();

                var download = await client.Backup.DownloadAsync(createdBackupKey);
                download.IsSuccess.Should().BeTrue();

                using var stream = download.Value;
                var buffer = new byte[128];
                var bytesRead = await stream.ReadAsync(buffer);
                bytesRead.Should().BeGreaterThan(0);

                var restore = await client.Backup.RestoreAsync("nonexistent.zip");
                restore.IsFailed.Should().BeTrue();
                AssertHasStatusCode(restore, (int)HttpStatusCode.BadRequest);
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(createdBackupKey))
                {
                    var delete = await client.Backup.DeleteAsync(createdBackupKey);
                    delete.IsSuccess.Should().BeTrue();
                }
            }
        }

        [TestMethod]
        public async Task Realtime_service_receives_create_events_against_live_server()
        {
            var client = await CreateAdminClientAsync();
            var entryService = client.Collection("entry");
            string? recordId = null;
            var completionSource = new TaskCompletionSource<SseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

            Func<SseMessage, Task> listener = message =>
            {
                if (message.Event == "entry" && !string.IsNullOrWhiteSpace(message.Data))
                {
                    completionSource.TrySetResult(message);
                }

                return Task.CompletedTask;
            };

            try
            {
                await client.RealTime.SubscribeAsync("entry", listener);

                var created = await entryService.CreateAsync(new LiveEntryRecord
                {
                    name = NextUniqueValue("realtime"),
                    is_done = false,
                });
                created.IsSuccess.Should().BeTrue();
                recordId = created.Value.Id;

                var completedTask = await Task.WhenAny(completionSource.Task, Task.Delay(TimeSpan.FromSeconds(10)));
                completedTask.Should().Be(completionSource.Task, "the realtime subscription should receive the create event");

                var payload = JsonDocument.Parse(completionSource.Task.Result.Data!);
                payload.RootElement.GetProperty("action").GetString().Should().Be("create");
                payload.RootElement.GetProperty("record").GetProperty("id").GetString().Should().Be(recordId);
            }
            finally
            {
                await entryService.UnsubscribeByTopicAndListenerAsync("entry", listener);
                await entryService.UnsubscribeByPrefixAsync("entry");
                await entryService.UnsubscribeAsync();
                await DeleteRecordAsync(client, "entry", recordId);
            }
        }

        [TestMethod]
        public async Task Record_file_methods_are_exercised_against_live_server()
        {
            var client = await CreateAdminClientAsync();
            var entryService = client.Collection("entry");
            HttpStatusCode? uploadStatus = null;
            string? uploadedRecordId = null;

            var beforeUpload = await entryService.GetFullListAsync<LiveEntryRecord>(batch: 200);
            beforeUpload.IsSuccess.Should().BeTrue();
            var beforeIds = beforeUpload.Value.Select(item => item.Id).OfType<string>().ToHashSet();

            client.AfterSend += (_, args) =>
            {
                if (args.Url.AbsolutePath == "/api/collections/entry/records")
                {
                    uploadStatus = args.HttpResponse?.StatusCode;
                }
            };

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("file payload"));
            await entryService.UploadFileAsync("missingField", "file.txt", stream);

            uploadStatus.Should().Be(HttpStatusCode.OK);

            var afterUpload = await entryService.GetFullListAsync<LiveEntryRecord>(batch: 200);
            afterUpload.IsSuccess.Should().BeTrue();
            uploadedRecordId = afterUpload.Value.Select(item => item.Id).OfType<string>().Single(id => !beforeIds.Contains(id));

            try
            {
                var download = await entryService.DownloadFileAsync("missing-record", "missing-file.txt");
                download.IsFailed.Should().BeTrue();
                AssertHasStatusCode(download, (int)HttpStatusCode.NotFound);
            }
            finally
            {
                await DeleteRecordAsync(client, "entry", uploadedRecordId);
            }
        }

        [TestMethod]
        public async Task Admin_related_v023_plus_endpoints_fail_with_expected_404_on_v022_server()
        {
            var client = new PocketBase(BaseUrl);

            var adminAuth = await client.Admin.AuthenticateWithPasswordAsync(AdminEmail, AdminPassword);
            adminAuth.IsFailed.Should().BeTrue();
            AssertHasStatusCode(adminAuth, (int)HttpStatusCode.NotFound);

            var legacyAdminClient = await CreateAdminClientAsync();
            var currentAdmin = await legacyAdminClient.GetCurrentAdminAsync();

            currentAdmin.IsFailed.Should().BeTrue();
            AssertHasStatusCode(currentAdmin, (int)HttpStatusCode.NotFound);

            var refresh = await legacyAdminClient.Admin.RefreshAsync();
            refresh.IsFailed.Should().BeTrue();
            AssertHasStatusCode(refresh, (int)HttpStatusCode.NotFound);

            var passwordReset = await legacyAdminClient.Admin.RequestPasswordResetAsync(AdminEmail);
            passwordReset.IsFailed.Should().BeTrue();
            AssertHasStatusCode(passwordReset, (int)HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task Batch_service_fails_with_expected_404_on_v022_server()
        {
            var client = await CreateAdminClientAsync();
            var batch = client.CreateBatch()
                .Create("entry", new { name = NextUniqueValue("batch-create"), is_done = false })
                .Update("entry", "missing-id", new { name = NextUniqueValue("batch-update"), is_done = true })
                .Upsert("entry", new { id = "missing-id", name = NextUniqueValue("batch-upsert"), is_done = false })
                .Delete("entry", "missing-id");

            var result = await batch.SendAsync();

            result.IsFailed.Should().BeTrue();
            AssertHasStatusCode(result, (int)HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task Log_service_fails_with_expected_404_on_v022_server()
        {
            var client = await CreateAdminClientAsync();

            var result = await client.Log.GetRequestsAsync();
            var request = await client.Log.GetRequestAsync("missing-request-id");
            var stats = await client.Log.GetRequestsStatisticsAsync();

            result.IsFailed.Should().BeTrue();
            AssertHasStatusCode(result, (int)HttpStatusCode.NotFound);

            request.IsFailed.Should().BeTrue();
            AssertHasStatusCode(request, (int)HttpStatusCode.NotFound);

            stats.IsFailed.Should().BeTrue();
            AssertHasStatusCode(stats, (int)HttpStatusCode.NotFound);
        }

        private static async Task<PocketBase> CreateAdminClientAsync()
        {
            var client = new PocketBase(BaseUrl);
            var adminAuth = await client.SendAsync<AdminAuthModel>(
                "/api/admins/auth-with-password",
                HttpMethod.Post,
                body: new Dictionary<string, object>
                {
                    ["identity"] = AdminEmail,
                    ["password"] = AdminPassword,
                });

            adminAuth.IsSuccess.Should().BeTrue();
            adminAuth.Value.Token.Should().NotBeNullOrWhiteSpace();
            adminAuth.Value.Admin.Should().NotBeNull();

            client.AuthStore.Save(adminAuth.Value.Token, adminAuth.Value.Admin);
            return client;
        }

        private static async Task DeleteRecordAsync(PocketBase client, string collectionName, string? recordId)
        {
            if (string.IsNullOrWhiteSpace(recordId))
            {
                return;
            }

            var delete = await client.SendAsync($"/api/collections/{collectionName}/records/{recordId}", HttpMethod.Delete);
            delete.IsSuccess.Should().BeTrue();
        }

        private static async Task CreateTemporaryTextCollectionAsync(PocketBase client, string collectionName)
        {
            var create = await client.SendAsync(
                "/api/collections",
                HttpMethod.Post,
                body: new Dictionary<string, object>
                {
                    ["name"] = collectionName,
                    ["type"] = "base",
                    ["system"] = false,
                    ["schema"] = new List<Dictionary<string, object?>>
                    {
                        new()
                        {
                            ["system"] = false,
                            ["id"] = NextSchemaId("text"),
                            ["name"] = "name",
                            ["type"] = "text",
                            ["required"] = false,
                            ["presentable"] = false,
                            ["unique"] = false,
                            ["options"] = new Dictionary<string, object?>
                            {
                                ["min"] = null,
                                ["max"] = null,
                                ["pattern"] = "",
                            },
                        },
                    },
                    ["indexes"] = Array.Empty<object>(),
                    ["listRule"] = "",
                    ["viewRule"] = "",
                    ["createRule"] = "",
                    ["updateRule"] = "",
                    ["deleteRule"] = "",
                    ["options"] = new Dictionary<string, object>(),
                });

            create.IsSuccess.Should().BeTrue();
        }

        private static async Task DeleteCollectionIfExistsAsync(PocketBase client, string collectionName)
        {
            var delete = await client.Collections.DeleteAsync(collectionName);
            if (delete.IsSuccess)
            {
                return;
            }

            delete.Errors.Any(error =>
                error.Metadata.TryGetValue("Statuscode", out var actualStatusCode)
                && actualStatusCode is int actualValue
                && actualValue == (int)HttpStatusCode.NotFound).Should().BeTrue();
        }

        private static string NextUniqueValue(string prefix)
        {
            return $"{prefix}-{Interlocked.Increment(ref uniqueSuffix)}";
        }

        private static string NextCollectionName()
        {
            return "sdktemp" + Interlocked.Increment(ref uniqueSuffix).ToString();
        }

        private static string NextSchemaId(string prefix)
        {
            return prefix + Interlocked.Increment(ref uniqueSuffix).ToString();
        }

        private static string NextEmailAddress(string prefix)
        {
            return $"{NextUniqueValue(prefix)}@example.com";
        }

        private static void AssertHasStatusCode(IResultBase result, int statusCode)
        {
            result.Errors.Any(error =>
                error.Metadata.TryGetValue("Statuscode", out var actualStatusCode)
                && actualStatusCode is int actualValue
                && actualValue == statusCode).Should().BeTrue();
        }

        private sealed class LiveEntryRecord : BaseModel
        {
            public string? name { get; set; }

            public bool? is_done { get; set; }
        }
    }
}
