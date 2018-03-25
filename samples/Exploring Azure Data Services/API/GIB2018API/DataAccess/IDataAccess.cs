using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

using Microsoft.Azure.Documents;

using GIB2018API.Model;

namespace GIB2018API.DataAccess
{
    public interface IDataAccess<T> : IDisposable where T : class, IThing
    {
        Task<HttpStatusCode> CreateDatabaseAsync();

        Task<T> SaveAsync(T document);

        Task<T> ReadAsync(string id);

        Task<HttpStatusCode> DeleteAsync(string id);

        Task<IEnumerable<T>> SearchQueryAsync(string query, SqlParameterCollection parameters);
    }
}
