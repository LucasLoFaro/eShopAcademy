using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;

namespace Application.Interfaces.Services
{
    public interface IBlobStorageClient
    {
        public Task DownloadBlob(string blobName);
        public Task UploadBlob(string blobName, IFormFile file);
    }
}
