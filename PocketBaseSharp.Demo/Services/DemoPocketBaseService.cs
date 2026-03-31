using System.Net;
using System.Text.Json;
using FluentResults;
using PocketBaseSharp.Event;
using PocketBaseSharp.Models;
using PocketBaseSharp.Models.Auth;

namespace PocketBaseSharp.FlowbiteDemo.Services;

public sealed class DemoPocketBaseService : IDisposable
{
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public const string DefaultBaseUrl = "http://127.0.0.1:8090/";
    public const string DefaultAdminEmail = "admin@admin.com";
    public const string DefaultAdminPassword = "demo123456";

    public PocketBase Client { get; }

    public event Action? Changed;

    public string BaseUrl => DefaultBaseUrl;
    public bool IsAuthenticated => Client.AuthStore.IsValid;
    public bool IsAdminSession => Client.AuthStore.Model is AdminModel;
    public bool IsUserSession => Client.AuthStore.Model is UserModel;
    public string SessionKind => Client.AuthStore.Model switch
    {
        AdminModel => "Admin",
        UserModel => "User",
        _ => "Anonymous",
    };
    public string? CurrentId => Client.AuthStore.Model?.Id;
    public string? CurrentEmail => (Client.AuthStore.Model as BaseAuthModel)?.Email;
    public string? TokenPreview => string.IsNullOrWhiteSpace(Client.AuthStore.Token)
        ? null
        : $"{Client.AuthStore.Token[..Math.Min(18, Client.AuthStore.Token.Length)]}...";
    public Uri? LastRequestUrl { get; private set; }
    public string? LastRequestMethod { get; private set; }
    public HttpStatusCode? LastStatusCode { get; private set; }
    public DateTimeOffset? LastRequestAt { get; private set; }
    public string? LastRequestPath => LastRequestUrl?.PathAndQuery;

    public DemoPocketBaseService()
    {
        Client = new PocketBase(BaseUrl);
        Client.BeforeSend += HandleBeforeSend;
        Client.AfterSend += HandleAfterSend;
        Client.AuthStore.OnChange += HandleAuthStoreChanged;
    }

    public async Task<Result<AdminAuthModel>> SignInAsAdminAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var result = await Client.SendAsync<AdminAuthModel>(
            "/api/admins/auth-with-password",
            HttpMethod.Post,
            body: new Dictionary<string, object>
            {
                ["identity"] = email,
                ["password"] = password,
            },
            cancellationToken: cancellationToken);

        if (result.IsSuccess)
        {
            Client.AuthStore.Save(result.Value.Token, result.Value.Admin);
            NotifyChanged();
        }

        return result;
    }

    public void Logout()
    {
        Client.AuthStore.Clear();
        NotifyChanged();
    }

    public string ToJson(object? value)
    {
        return value is null ? string.Empty : JsonSerializer.Serialize(value, jsonOptions);
    }

    public string Describe(ResultBase result)
    {
        return string.Join(
            Environment.NewLine,
            result.Errors.Select(error =>
            {
                if (error.Metadata.TryGetValue("Statuscode", out var statusCode))
                {
                    return $"{error.Message} (HTTP {statusCode})";
                }

                return error.Message;
            }));
    }

    private HttpRequestMessage HandleBeforeSend(object sender, RequestEventArgs args)
    {
        LastRequestUrl = args.Url;
        LastRequestMethod = args.HttpRequest.Method.Method;
        LastRequestAt = DateTimeOffset.Now;
        NotifyChanged();
        return args.HttpRequest;
    }

    private void HandleAfterSend(object sender, ResponseEventArgs args)
    {
        LastStatusCode = args.HttpResponse?.StatusCode;
        NotifyChanged();
    }

    private void HandleAuthStoreChanged(object? sender, AuthStoreEvent e)
    {
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }

    public void Dispose()
    {
        Client.BeforeSend -= HandleBeforeSend;
        Client.AfterSend -= HandleAfterSend;
        Client.AuthStore.OnChange -= HandleAuthStoreChanged;
    }
}
