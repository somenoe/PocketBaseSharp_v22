using FluentResults;
using PocketBaseSharp.Models;
using PocketBaseSharp.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PocketBaseSharp.Services
{
    public class BackupService : BaseService
    {
        readonly PocketBase _pocketBase;
        public BackupService(PocketBase pocketBase)
        {
            this._pocketBase = pocketBase;
        }

        protected override string BasePath(string? path = null)
        {
            return path ?? "/api/backups";
        }

        /// <summary>
        /// Retrieves a list of all available backups.
        /// </summary>
        /// <remarks>
        /// Endpoint: GET /api/backups
        /// Auth: Admin/Superuser required (via AuthStore)
        /// </remarks>
        /// <returns>Result containing an enumerable of BackupModel objects.</returns>
        public async Task<Result<IEnumerable<BackupModel>>> GetFullListAsync()
        {
            var body = await _pocketBase.SendAsync<IEnumerable<BackupModel>>(BasePath(), HttpMethod.Get);
            return body;
        }

        /// <summary>
        /// Triggers a new database backup.
        /// </summary>
        /// <remarks>
        /// Endpoint: POST /api/backups
        /// Auth: Admin/Superuser required (via AuthStore)
        /// Note: PocketBase returns 204 No Content on success, so the BackupModel in the result will be null.
        /// Use GetFullListAsync() after creation to retrieve the backup details.
        /// </remarks>
        /// <param name="basename">
        /// Optional custom filename for the backup (e.g., "daily-backup.zip").
        /// If null, PocketBase will auto-generate a timestamp-based name.
        /// </param>
        /// <param name="body">The request body to send to the API. Default is null.</param>
        /// <param name="query">The query parameters to send to the API. Default is null.</param>
        /// <param name="headers">The headers to send to the API. Default is null.</param>
        /// <param name="cancellationToken">Cancellation token. Default is default.</param>
        /// <returns>Result containing null BackupModel (API returns 204 No Content on success).</returns>
        public async Task<Result<BackupModel>> CreateAsync(string? basename = null,IDictionary<string, object>? body = null,IDictionary<string, object?>? query = null,IDictionary<string, string>? headers = null,CancellationToken cancellationToken = default)
        {
            body ??= new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(basename))
            {
                body["basename"] = basename;
            }
            var url = BasePath();
            return await _pocketBase.SendAsync<BackupModel>(url, HttpMethod.Post, headers: headers, query: query, body: body, files: null, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Triggers a new database backup (synchronous version).
        /// </summary>
        /// <remarks>
        /// Endpoint: POST /api/backups
        /// Auth: Admin/Superuser required (via AuthStore)
        /// Note: PocketBase returns 204 No Content on success, so the BackupModel in the result will be null.
        /// Use GetFullListAsync() after creation to retrieve the backup details.
        /// </remarks>
        /// <param name="basename">
        /// Optional custom filename for the backup (e.g., "daily-backup.zip").
        /// If null, PocketBase will auto-generate a timestamp-based name.
        /// </param>
        /// <param name="body">The request body to send to the API. Default is null.</param>
        /// <param name="query">The query parameters to send to the API. Default is null.</param>
        /// <param name="headers">The headers to send to the API. Default is null.</param>
        /// <param name="cancellationToken">Cancellation token. Default is default.</param>
        /// <returns>Result containing null BackupModel (API returns 204 No Content on success).</returns>
        public Result<BackupModel> Create(string? basename = null,IDictionary<string, object>? body = null,IDictionary<string, object?>? query = null,IDictionary<string, string>? headers = null,CancellationToken cancellationToken = default)
        {
            body ??= new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(basename))
            {
                body["basename"] = basename;
            }
            var url = BasePath();
            return _pocketBase.Send<BackupModel>(url, HttpMethod.Post, headers: headers, query: query, body: body, files: null, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Downloads a backup file as a stream.
        /// </summary>
        /// <remarks>
        /// Endpoint: GET /api/backups/{key}
        /// Auth: Admin/Superuser required (via AuthStore)
        /// Requires: A valid file token obtained via GetFileTokenAsync()
        /// </remarks>
        /// <param name="key">The backup key identifier.</param>
        /// <param name="query">The query parameters to send to the API. Default is null.</param>
        /// <param name="cancellationToken">Cancellation token. Default is default.</param>
        /// <returns>Result containing a readable Stream of the backup file.</returns>
        public async Task<Result<Stream>> DownloadAsync(string key,IDictionary<string, object?>? query = null,CancellationToken cancellationToken = default)
        {
            query ??= new Dictionary<string, object?>();
            
            var tokenResult = await _pocketBase.GetFileTokenAsync(cancellationToken);
            if (!tokenResult.IsSuccess)
            {
                return Result.Fail<Stream>(tokenResult.Errors);
            }

            query["token"] = tokenResult.Value;
            
            var url = $"{BasePath()}/{UrlEncode(key)}";
            return await _pocketBase.GetStreamAsync(url, query, cancellationToken);
        }


        /// <summary>
        /// Restores the database from a backup.
        /// WARNING: DESTRUCTIVE OPERATION - Completely replaces current database.
        /// </summary>
        /// <remarks>
        /// Endpoint: POST /api/backups/{key}/restore
        /// Auth: Admin/Superuser required (via AuthStore)
        /// Note: PocketBase will temporarily stop the server, restore, and restart.
        /// </remarks>
        /// <param name="key">The backup key identifier.</param>
        /// <param name="body">The request body to send to the API. Default is null.</param>
        /// <param name="query">The query parameters to send to the API. Default is null.</param>
        /// <param name="headers">The headers to send to the API. Default is null.</param>
        /// <param name="cancellationToken">Cancellation token. Default is default.</param>
        /// <returns>Result indicating success/failure of restore operation.</returns>
        public async Task<Result> RestoreAsync(string key,IDictionary<string, object>? body = null,IDictionary<string, object?>? query = null,IDictionary<string, string>? headers = null,CancellationToken cancellationToken = default)
        {
            body ??= new Dictionary<string, object>();
            var url = $"{BasePath()}/{UrlEncode(key)}/restore";
            return await _pocketBase.SendAsync(url, HttpMethod.Post, headers: headers, query: query, body: body, files: null, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Restores the database from a backup (synchronous version).
        /// WARNING: DESTRUCTIVE OPERATION - Completely replaces current database.
        /// </summary>
        /// <remarks>
        /// Endpoint: POST /api/backups/{key}/restore
        /// Auth: Admin/Superuser required (via AuthStore)
        /// Note: PocketBase will temporarily stop the server, restore, and restart.
        /// </remarks>
        /// <param name="key">The backup key identifier.</param>
        /// <param name="body">The request body to send to the API. Default is null.</param>
        /// <param name="query">The query parameters to send to the API. Default is null.</param>
        /// <param name="headers">The headers to send to the API. Default is null.</param>
        /// <param name="cancellationToken">Cancellation token. Default is default.</param>
        /// <returns>Result indicating success/failure of restore operation.</returns>
        public Result Restore(string key,IDictionary<string, object>? body = null,IDictionary<string, object?>? query = null,IDictionary<string, string>? headers = null,CancellationToken cancellationToken = default)
        {
            body ??= new Dictionary<string, object>();
            var url = $"{BasePath()}/{UrlEncode(key)}/restore";
            return _pocketBase.Send(url, HttpMethod.Post, headers: headers, query: query, body: body, files: null, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Deletes a backup file.
        /// </summary>
        /// <remarks>
        /// Endpoint: DELETE /api/backups/{key}
        /// Auth: Admin/Superuser required (via AuthStore)
        /// </remarks>
        /// <param name="key">The backup key identifier.</param>
        /// <param name="body">The request body to send to the API. Default is null.</param>
        /// <param name="query">The query parameters to send to the API. Default is null.</param>
        /// <param name="headers">The headers to send to the API. Default is null.</param>
        /// <param name="cancellationToken">Cancellation token. Default is default.</param>
        /// <returns>Result indicating success/failure of delete operation.</returns>
        public async Task<Result> DeleteAsync(string key,IDictionary<string, object>? body = null,IDictionary<string, object?>? query = null,IDictionary<string, string>? headers = null,CancellationToken cancellationToken = default)
        {
            var url = $"{BasePath()}/{UrlEncode(key)}";
            return await _pocketBase.SendAsync(url, HttpMethod.Delete, headers: headers, query: query, body: body, files: null, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Deletes a backup file (synchronous version).
        /// </summary>
        /// <remarks>
        /// Endpoint: DELETE /api/backups/{key}
        /// Auth: Admin/Superuser required (via AuthStore)
        /// </remarks>
        /// <param name="key">The backup key identifier.</param>
        /// <param name="body">The request body to send to the API. Default is null.</param>
        /// <param name="query">The query parameters to send to the API. Default is null.</param>
        /// <param name="headers">The headers to send to the API. Default is null.</param>
        /// <param name="cancellationToken">Cancellation token. Default is default.</param>
        /// <returns>Result indicating success/failure of delete operation.</returns>
        public Result Delete(string key,IDictionary<string, object>? body = null,IDictionary<string, object?>? query = null,IDictionary<string, string>? headers = null,CancellationToken cancellationToken = default)
        {
            var url = $"{BasePath()}/{UrlEncode(key)}";
            return _pocketBase.Send(url, HttpMethod.Delete, headers: headers, query: query, body: body, files: null, cancellationToken: cancellationToken);  
        }
    }
}
