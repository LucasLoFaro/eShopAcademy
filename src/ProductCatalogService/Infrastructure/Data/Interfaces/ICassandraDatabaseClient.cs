using Cassandra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Interfaces
{
    public interface ICassandraDatabaseClient : IDisposable
    {
        public ISession Session { get; }
    }
}
