using Application.Interfaces.Data;
using Data.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Settings
{
    public class FileDataBaseSettings : IDatabaseSettingsProvider
    {
        private readonly IConfiguration _configuration;

        public FileDataBaseSettings(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetConnectionString()
        {
            return _configuration.GetConnectionString("ConnectionString");
        }

        public string GetClient()
        {
            return _configuration["Client"];
        }

        public string GetSecret()
        {
            return _configuration["Secret"];
        }

        public string GetConnectionBundlePath()
        {
            return _configuration["BundlePath"];
        }
    }

}
