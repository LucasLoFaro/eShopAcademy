using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Services.Settings;
using Microsoft.AspNetCore.Http;
using Application.Interfaces.Services;

namespace Services
{
    public class BlobStorageClient : IBlobStorageClient
    {
        private readonly BlobServiceClient _serviceClient;
        private readonly BlobContainerClient _containerClient;
        private readonly IOptionsMonitor<BlobStorageSettings> _settings;
        public BlobStorageClient(BlobServiceClient serviceClient, IOptionsMonitor<BlobStorageSettings> settings)
        {
            _serviceClient = serviceClient;
            _containerClient = _serviceClient.GetBlobContainerClient(settings.CurrentValue.BlobContainerName);
            _settings = settings;
        }
        async public Task DownloadBlob(string blobName) 
        {
            BlobClient _blobClient = _containerClient.GetBlobClient(blobName);
            await _blobClient.DownloadToAsync(_settings.CurrentValue.DownloadPath);
        }

        async public Task UploadBlob(string blobName, IFormFile file)
        {
            BlobClient _blobClient = _containerClient.GetBlobClient(file.FileName);
            using (var stream = file.OpenReadStream())
            {
                await _blobClient.UploadAsync(stream);
            }
        }
    }
}
