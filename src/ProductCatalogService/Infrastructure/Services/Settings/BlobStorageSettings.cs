using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Settings
{
    public class BlobStorageSettings
    {
        public string BlobContainerName { get; set; }
        public string DownloadPath { get; set; }
    }
}
