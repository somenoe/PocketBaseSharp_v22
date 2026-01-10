using MudBlazor;
using PocketBaseSharp;
using PocketBaseSharp.Models;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Example.Services;

namespace Example.Pages.Admin.Components
{
    public partial class BackupManagement
    {
        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        private IEnumerable<BackupModel>? Backups;
        private bool IsLoading = false;
        private string? BackupName;
        private FileDownloadInterop? _fileDownloadInterop;

        protected override async Task OnInitializedAsync()
        {
            _fileDownloadInterop = new FileDownloadInterop(JSRuntime);
            await RefreshBackupsAsync();
        }

        private async Task RefreshBackupsAsync()
        {
            IsLoading = true;
            try
            {
                var result = await PocketBase.Backup.GetFullListAsync();
                if (result.IsSuccess)
                {
                    Backups = result.Value;
                    Snackbar.Add("Backups loaded successfully", Severity.Success);
                }
                else
                {
                    Snackbar.Add("Failed to load backups", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error loading backups: {ex.Message}", Severity.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CreateBackupAsync()
        {
            IsLoading = true;
            try
            {
                var result = await PocketBase.Backup.CreateAsync(BackupName);
                if (result.IsSuccess)
                {
                    if (result.Value?.Key != null)
                    {
                        Snackbar.Add($"Backup created: {result.Value.Key}", Severity.Success);
                    }
                    else
                    {
                        Snackbar.Add("Backup created successfully", Severity.Success);
                    }
                    BackupName = null;
                    await RefreshBackupsAsync();
                }
                else
                {
                    Snackbar.Add("Failed to create backup", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error creating backup: {ex.Message}", Severity.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DownloadBackupAsync(string? key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            IsLoading = true;
            try
            {
                var result = await PocketBase.Backup.DownloadAsync(key);
                if (result.IsSuccess)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await result.Value.CopyToAsync(memoryStream);
                        var bytes = memoryStream.ToArray();

                        await _fileDownloadInterop!.DownloadFileAsync(bytes, key);
                        Snackbar.Add($"Backup downloaded: {key} ({bytes.Length} bytes)", Severity.Success);
                    }
                }
                else
                {
                    var errorMessage = result.Errors?.FirstOrDefault()?.Message ?? "Unknown error";
                    Snackbar.Add($"Failed to download backup: {errorMessage}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error downloading backup: {ex.Message}", Severity.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RestoreBackupAsync(string? key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            bool? confirmed = await DialogService.ShowMessageBox(
                "Restore Backup",
                $"Are you sure you want to restore from '{key}'? This will replace all current data!",
                yesText: "Restore",
                cancelText: "Cancel"
            );

            if (confirmed == true)
            {
                IsLoading = true;
                try
                {
                    //Warning: This does not work on pocketbase windows hosted. See: https://github.com/pocketbase/pocketbase/issues/3411
                    //It retrns a 204 but the logs will show restore is not supported on Windows
                    var restoreResult = await PocketBase.Backup.RestoreAsync(key);
                    if (restoreResult.IsSuccess)
                    {
                        Snackbar.Add("Database restored successfully", Severity.Success);
                        await RefreshBackupsAsync();
                    }
                    else
                    {
                        Snackbar.Add("Failed to restore backup", Severity.Error);
                    }
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Error restoring backup: {ex.Message}", Severity.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async Task DeleteBackupAsync(string? key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            bool? confirmed = await DialogService.ShowMessageBox(
                "Delete Backup",
                $"Are you sure you want to delete '{key}'?",
                yesText: "Delete",
                cancelText: "Cancel"
            );

            if (confirmed == true)
            {
                IsLoading = true;
                try
                {
                    var deleteResult = await PocketBase.Backup.DeleteAsync(key);
                    if (deleteResult.IsSuccess)
                    {
                        Snackbar.Add("Backup deleted successfully", Severity.Success);
                        await RefreshBackupsAsync();
                    }
                    else
                    {
                        Snackbar.Add("Failed to delete backup", Severity.Error);
                    }
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Error deleting backup: {ex.Message}", Severity.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
