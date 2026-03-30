using FluentAssertions;
using PocketBaseSharp.Models;
using PocketBaseSharp.Models.Auth;
using PocketBaseSharp.Models.Log;
using PocketBaseSharp.Tests.TestInfrastructure;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PocketBaseSharp.Tests
{
    [TestClass]
    public class AuthAndBatchUnitTests
    {
        [TestMethod]
        public async Task Batch_builder_send_sync_serializes_requests_and_guards_empty_batch()
        {
            var emptyClient = TestPocketBaseFactory.CreateClient(new RecordingHttpMessageHandler());
            var emptyResult = emptyClient.CreateBatch().Send();

            emptyResult.IsFailed.Should().BeTrue();
            emptyResult.Errors.Should().ContainSingle(error => error.Message == "No requests added to batch");

            var handler = new RecordingHttpMessageHandler();
            handler.QueueResponse(HttpStatusCode.OK, new[]
            {
                new
                {
                    status = 200,
                    body = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                    },
                },
            });

            var client = TestPocketBaseFactory.CreateClient(handler);
            var scheduledAt = new DateTime(2024, 01, 02, 03, 04, 05, 678, DateTimeKind.Utc);

            var result = client.Batch.CreateBatch()
                .Create("tasks", new BatchItem { DisplayName = "Created", ScheduledAt = scheduledAt }, expand: "owner")
                .Update("tasks", "record-1", new BatchItem { DisplayName = "Updated", ScheduledAt = scheduledAt })
                .Upsert("tasks", new BatchItem { Id = "record-2", DisplayName = "Upserted", ScheduledAt = scheduledAt })
                .Delete("tasks", "record-3")
                .Send(headers: new Dictionary<string, string> { ["X-Batch"] = "1" });

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(1);

            var request = handler.Requests.Should().ContainSingle().Subject;
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri.Should().Be(new Uri("https://example.com/api/batch"));
            request.Headers.GetValues("X-Batch").Single().Should().Be("1");

            using var payload = await TestPocketBaseFactory.ReadJsonAsync(request.Content);
            var requests = payload.RootElement.GetProperty("requests");

            requests.GetArrayLength().Should().Be(4);
            requests[0].GetProperty("method").GetString().Should().Be("POST");
            requests[0].GetProperty("url").GetString().Should().Be("/api/collections/tasks/records?expand=owner");
            requests[0].GetProperty("body").GetProperty("display_name").GetString().Should().Be("Created");
            requests[0].GetProperty("body").GetProperty("scheduledAt").GetString().Should().Be("2024-01-02 03:04:05.678Z");

            requests[1].GetProperty("method").GetString().Should().Be("PATCH");
            requests[1].GetProperty("url").GetString().Should().Be("/api/collections/tasks/records/record-1");

            requests[2].GetProperty("method").GetString().Should().Be("PUT");
            requests[2].GetProperty("body").GetProperty("id").GetString().Should().Be("record-2");

            requests[3].GetProperty("method").GetString().Should().Be("DELETE");
            requests[3].GetProperty("body").EnumerateObject().Should().BeEmpty();
        }

        [TestMethod]
        public async Task User_service_sync_crud_wrappers_send_expected_requests()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.QueueResponse(HttpStatusCode.OK, new { id = "user-1", email = "user@example.com", username = "created", verified = false });
            handler.QueueResponse(HttpStatusCode.OK, new { id = "user-1", email = "user@example.com", username = "updated", verified = true });

            var client = TestPocketBaseFactory.CreateClient(handler);

            var createResult = client.User.Create("user@example.com", "Password123!", "Password123!");
            var updateResult = client.User.Update(
                "user-1",
                username: "updated",
                email: "user@example.com",
                emailVisibility: true,
                oldPassword: "OldPassword123!",
                password: "Password123!",
                passwordConfirm: "Password123!",
                verified: true);

            createResult.IsSuccess.Should().BeTrue();
            updateResult.IsSuccess.Should().BeTrue();

            using (var createBody = await TestPocketBaseFactory.ReadJsonAsync(handler.Requests[0].Content))
            {
                handler.Requests[0].RequestUri.Should().Be(new Uri("https://example.com/api/collections/users/records"));
                createBody.RootElement.GetProperty("email").GetString().Should().Be("user@example.com");
                createBody.RootElement.GetProperty("password").GetString().Should().Be("Password123!");
                createBody.RootElement.GetProperty("passwordConfirm").GetString().Should().Be("Password123!");
            }

            using (var updateBody = await TestPocketBaseFactory.ReadJsonAsync(handler.Requests[1].Content))
            {
                handler.Requests[1].Method.Should().Be(HttpMethod.Patch);
                handler.Requests[1].RequestUri.Should().Be(new Uri("https://example.com/api/collections/users/records/user-1"));
                updateBody.RootElement.GetProperty("username").GetString().Should().Be("updated");
                updateBody.RootElement.GetProperty("emailVisibility").GetBoolean().Should().BeTrue();
                updateBody.RootElement.GetProperty("verified").GetBoolean().Should().BeTrue();
            }
        }

        [TestMethod]
        public async Task User_service_sync_auth_wrappers_send_expected_requests_and_update_auth_store()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.QueueResponse(HttpStatusCode.OK, new { usernamePassword = true, emailPassword = true, authProviders = Array.Empty<object>() });
            handler.QueueResponse(HttpStatusCode.OK, CreateUserAuthResponse());
            handler.QueueResponse(HttpStatusCode.OK, CreateUserAuthResponse());
            handler.QueueResponse(HttpStatusCode.OK, CreateUserAuthResponse());
            handler.QueueResponse(HttpStatusCode.NoContent);
            handler.QueueResponse(HttpStatusCode.OK, CreateUserAuthResponse());
            handler.QueueResponse(HttpStatusCode.NoContent);
            handler.QueueResponse(HttpStatusCode.OK, CreateUserAuthResponse());
            handler.QueueResponse(HttpStatusCode.NoContent);
            handler.QueueResponse(HttpStatusCode.OK, CreateUserAuthResponse());
            handler.QueueResponse(HttpStatusCode.OK, Array.Empty<object>());
            handler.QueueResponse(HttpStatusCode.NoContent);

            var client = TestPocketBaseFactory.CreateClient(handler);

            var authMethods = client.User.GetAuthenticationMethods();
            var passwordAuth = client.User.AuthenticateWithPassword("user@example.com", "Password123!");
            var oauthAuth = client.User.AuthenticateViaOAuth2("github", "code", "verifier", "https://example.com/callback");
            var refresh = client.User.Refresh();
            var requestPasswordReset = client.User.RequestPasswordReset("user@example.com");
            var confirmPasswordReset = client.User.ConfirmPasswordReset("reset-token", "Password123!", "Password123!");
            var requestVerification = client.User.RequestVerification("user@example.com");
            var confirmVerification = client.User.ConfirmVerification("verification-token");
            var requestEmailChange = client.User.RequestEmailChange("next@example.com");
            var confirmEmailChange = client.User.ConfirmEmailChange("email-token", "Password123!");
            var externalAuths = client.User.GetExternalAuthenticationMethods("user-1");
            var unlinkExternalAuth = client.User.UnlinkExternalAuthentication("user-1", "github");

            authMethods.IsSuccess.Should().BeTrue();
            authMethods.Value.EmailPassword.Should().BeTrue();
            passwordAuth.IsSuccess.Should().BeTrue();
            oauthAuth.IsSuccess.Should().BeTrue();
            refresh.IsSuccess.Should().BeTrue();
            requestPasswordReset.IsSuccess.Should().BeTrue();
            confirmPasswordReset.IsSuccess.Should().BeTrue();
            requestVerification.IsSuccess.Should().BeTrue();
            confirmVerification.IsSuccess.Should().BeTrue();
            requestEmailChange.IsSuccess.Should().BeTrue();
            confirmEmailChange.IsSuccess.Should().BeTrue();
            externalAuths.IsSuccess.Should().BeTrue();
            unlinkExternalAuth.IsSuccess.Should().BeTrue();

            client.AuthStore.Token.Should().Be(TestPocketBaseFactory.ValidToken);
            client.AuthStore.Model.Should().BeOfType<UserModel>();
            client.AuthStore.Model!.Id.Should().Be("user-1");

            handler.Requests[0].RequestUri.Should().Be(new Uri("https://example.com/api/collections/users/auth-methods"));

            using (var authBody = await TestPocketBaseFactory.ReadJsonAsync(handler.Requests[1].Content))
            {
                handler.Requests[1].RequestUri.Should().Be(new Uri("https://example.com/api/collections/users/auth-with-password"));
                authBody.RootElement.GetProperty("identity").GetString().Should().Be("user@example.com");
                authBody.RootElement.GetProperty("password").GetString().Should().Be("Password123!");
            }

            using (var oauthBody = await TestPocketBaseFactory.ReadJsonAsync(handler.Requests[2].Content))
            {
                handler.Requests[2].RequestUri.Should().Be(new Uri("https://example.com/api/collections/users/auth-via-oauth2"));
                oauthBody.RootElement.GetProperty("provider").GetString().Should().Be("github");
                oauthBody.RootElement.GetProperty("redirectUrl").GetString().Should().Be("https://example.com/callback");
            }

            handler.Requests[3].RequestUri.Should().Be(new Uri("https://example.com/api/collections/users/auth-refresh"));
            handler.Requests[3].Headers.GetValues("Authorization").Single().Should().Be(TestPocketBaseFactory.ValidToken);

            using (var confirmResetBody = await TestPocketBaseFactory.ReadJsonAsync(handler.Requests[5].Content))
            {
                handler.Requests[5].RequestUri.Should().Be(new Uri("https://example.com/api/collections/users/confirm-password-reset"));
                confirmResetBody.RootElement.GetProperty("token").GetString().Should().Be("reset-token");
            }

            using (var confirmEmailChangeBody = await TestPocketBaseFactory.ReadJsonAsync(handler.Requests[9].Content))
            {
                handler.Requests[9].RequestUri.Should().Be(new Uri("https://example.com/api/collections/users/confirm-email-change"));
                confirmEmailChangeBody.RootElement.GetProperty("token").GetString().Should().Be("email-token");
                confirmEmailChangeBody.RootElement.GetProperty("password").GetString().Should().Be("Password123!");
            }

            handler.Requests[10].RequestUri.Should().Be(new Uri("https://example.com/api/collections/users/records/user-1/external-auths"));
            handler.Requests[11].RequestUri.Should().Be(new Uri("https://example.com/api/collections/users/records/user-1/external-auths/github"));
        }

        [TestMethod]
        public async Task User_service_async_auth_wrappers_send_expected_requests()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.QueueResponse(HttpStatusCode.OK, new { usernamePassword = true, emailPassword = true, authProviders = Array.Empty<object>() });
            handler.QueueResponse(HttpStatusCode.OK, CreateUserAuthResponse());
            handler.QueueResponse(HttpStatusCode.OK, CreateUserAuthResponse());
            handler.QueueResponse(HttpStatusCode.NoContent);
            handler.QueueResponse(HttpStatusCode.OK, CreateUserAuthResponse());
            handler.QueueResponse(HttpStatusCode.NoContent);
            handler.QueueResponse(HttpStatusCode.OK, CreateUserAuthResponse());
            handler.QueueResponse(HttpStatusCode.NoContent);
            handler.QueueResponse(HttpStatusCode.OK, CreateUserAuthResponse());

            var client = TestPocketBaseFactory.CreateClient(handler);

            var authMethods = await client.User.GetAuthenticationMethodsAsync();
            var passwordAuth = await client.User.AuthenticateWithPasswordAsync("user@example.com", "Password123!");
            var refresh = await client.User.RefreshAsync();
            var requestPasswordReset = await client.User.RequestPasswordResetAsync("user@example.com");
            var confirmPasswordReset = await client.User.ConfirmPasswordResetAsync("reset-token", "Password123!", "Password123!");
            var requestVerification = await client.User.RequestVerificationAsync("user@example.com");
            var confirmVerification = await client.User.ConfirmVerificationAsync("verification-token");
            var requestEmailChange = await client.User.RequestEmailChangeAsync("next@example.com");
            var confirmEmailChange = await client.User.ConfirmEmailChangeAsync("email-token", "Password123!");

            authMethods.IsSuccess.Should().BeTrue();
            passwordAuth.IsSuccess.Should().BeTrue();
            refresh.IsSuccess.Should().BeTrue();
            requestPasswordReset.IsSuccess.Should().BeTrue();
            confirmPasswordReset.IsSuccess.Should().BeTrue();
            requestVerification.IsSuccess.Should().BeTrue();
            confirmVerification.IsSuccess.Should().BeTrue();
            requestEmailChange.IsSuccess.Should().BeTrue();
            confirmEmailChange.IsSuccess.Should().BeTrue();

            handler.Requests.Select(request => request.RequestUri!.AbsolutePath).Should().Equal(
                "/api/collections/users/auth-methods",
                "/api/collections/users/auth-with-password",
                "/api/collections/users/auth-refresh",
                "/api/collections/users/request-password-reset",
                "/api/collections/users/confirm-password-reset",
                "/api/collections/users/request-verification",
                "/api/collections/users/confirm-verification",
                "/api/collections/users/request-email-change",
                "/api/collections/users/confirm-email-change");
        }

        [TestMethod]
        public async Task Auth_collection_and_admin_sync_methods_use_expected_paths()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.QueueResponse(HttpStatusCode.OK, new { usernamePassword = true, emailPassword = true, authProviders = Array.Empty<object>() });
            handler.QueueResponse(HttpStatusCode.OK, CreateAdminAuthResponse());
            handler.QueueResponse(HttpStatusCode.OK, CreateAdminAuthResponse());
            handler.QueueResponse(HttpStatusCode.NoContent);
            handler.QueueResponse(HttpStatusCode.OK, CreateAdminAuthResponse());

            var client = TestPocketBaseFactory.CreateClient(handler);

            var authCollection = client.AuthCollection<UserModel>("members");
            var methods = authCollection.GetAuthenticationMethods();

            var adminAuth = client.Admin.AuthenticateWithPassword("admin@example.com", "Password123!");
            var adminRefresh = client.Admin.Refresh();
            var adminRequestReset = client.Admin.RequestPasswordReset("admin@example.com");
            var adminConfirmReset = client.Admin.ConfirmPasswordReset("reset-token", "Password123!", "Password123!");

            methods.IsSuccess.Should().BeTrue();
            adminAuth.IsSuccess.Should().BeTrue();
            adminRefresh.IsSuccess.Should().BeTrue();
            adminRequestReset.IsSuccess.Should().BeTrue();
            adminConfirmReset.IsSuccess.Should().BeTrue();

            handler.Requests[0].RequestUri.Should().Be(new Uri("https://example.com/api/collections/members/auth-methods"));

            using (var adminAuthBody = await TestPocketBaseFactory.ReadJsonAsync(handler.Requests[1].Content))
            {
                handler.Requests[1].RequestUri.Should().Be(new Uri("https://example.com/api/collections/_superusers/auth-with-password"));
                adminAuthBody.RootElement.GetProperty("identity").GetString().Should().Be("admin@example.com");
            }

            handler.Requests[2].RequestUri.Should().Be(new Uri("https://example.com/api/collections/_superusers/auth-refresh"));
            handler.Requests[3].RequestUri.Should().Be(new Uri("https://example.com/api/collections/_superusers/request-password-reset"));
            handler.Requests[4].RequestUri.Should().Be(new Uri("https://example.com/api/collections/_superusers/confirm-password-reset"));
        }

        private static object CreateUserAuthResponse()
        {
            return new
            {
                token = TestPocketBaseFactory.ValidToken,
                record = new
                {
                    id = "user-1",
                    email = "user@example.com",
                    username = "created",
                    verified = true,
                },
            };
        }

        private static object CreateAdminAuthResponse()
        {
            return new
            {
                token = TestPocketBaseFactory.ValidToken,
                record = new
                {
                    id = "admin-1",
                    email = "admin@example.com",
                    verified = true,
                },
            };
        }

        private sealed class BatchItem
        {
            public string? Id { get; set; }

            [JsonPropertyName("display_name")]
            public string? DisplayName { get; set; }

            public DateTime ScheduledAt { get; set; }
        }
    }
}
