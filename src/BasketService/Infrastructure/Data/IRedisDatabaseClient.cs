using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRedisStack;
using StackExchange.Redis;

namespace Data
{
    public interface IRedisDatabaseClient : IDisposable
    {
        void OpenConnection();
        IDatabase GetSession();
    }
}