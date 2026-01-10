using Microsoft.JSInterop;

namespace Example.Services
{
    /// <summary>
    /// JavaScript interop service for triggering file downloads in the browser.
    /// </summary>
    public class FileDownloadInterop
    {
        private readonly IJSRuntime _jsRuntime;

        public FileDownloadInterop(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        /// <summary>
        /// Triggers a file download in the browser.
        /// </summary>
        /// <param name="fileBytes">The file content as a byte array.</param>
        /// <param name="fileName">The name of the file to download.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DownloadFileAsync(byte[] fileBytes, string fileName)
        {
            await _jsRuntime.InvokeVoidAsync("fileDownload.downloadFile", fileBytes, fileName);
        }
    }
}
