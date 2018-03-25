using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using GIB2018API.Model;
using GIB2018API.Model.Config;
using GIB2018API.Serialization;

namespace GIB2018API.DataAccess
{
    public class CosmosDBDataAccess<T> : IDataAccess<T> where T : class, IThing
    {
        protected CosmosDbConfig _config;

        protected DocumentClient _client;

        public CosmosDBDataAccess(IOptions<CosmosDbConfig> config)
        {
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                Converters = new List<JsonConverter>()
                {
                    new StringEnumConverter(),
                    new CosmosDbJsonConverter()
                },
                NullValueHandling = NullValueHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };

            _config = config.Value;
            _client = new DocumentClient(
                new Uri(_config.Endpoint),
                _config.AuthKey,
                jsonSerializerSettings);
        }

        public void Dispose()
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }

        public async Task<HttpStatusCode> CreateDatabaseAsync()
        {
            var response = HttpStatusCode.OK;

            var databaseResponse = await _client.CreateDatabaseIfNotExistsAsync(new Database { Id = _config.DatabaseName });

            if (databaseResponse.StatusCode == HttpStatusCode.Created) response = HttpStatusCode.Created;

            var documentCollection = new DocumentCollection { Id = _config.CollectionName };

            var collectionResponse = await _client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(_config.DatabaseName), documentCollection);

            if (collectionResponse.StatusCode == HttpStatusCode.Created) response = HttpStatusCode.Created;

            return response;
        }

        public async Task<T> SaveAsync(T document)
        {
            if (string.IsNullOrEmpty(document.Id))
                document.Id = Guid.NewGuid().ToString();

            try
            {
                var documentUri = UriFactory.CreateDocumentUri(_config.DatabaseName, _config.CollectionName, document.Id);

                var existingDoc = await _client.ReadDocumentAsync<T>(documentUri);

                if (existingDoc.StatusCode == HttpStatusCode.OK)
                {
                    if (existingDoc.Document.CreatedAt.HasValue)
                        document.CreatedAt = existingDoc.Document.CreatedAt;
                    else
                        document.CreatedAt = DateTimeOffset.UtcNow;
                    document.UpdatedAt = DateTimeOffset.UtcNow;

                    var response = await _client.ReplaceDocumentAsync(documentUri, document);
                }

                return document;
            }
            catch (DocumentClientException de)
            {
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    var collectionUri = UriFactory.CreateDocumentCollectionUri(_config.DatabaseName, _config.CollectionName);

                    document.CreatedAt = DateTimeOffset.UtcNow;
                    document.UpdatedAt = document.CreatedAt;

                    var response = await _client.CreateDocumentAsync(collectionUri, document);

                    return document;
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task<T> ReadAsync(string id)
        {
            try
            {
                var documentUri = UriFactory.CreateDocumentUri(_config.DatabaseName, _config.CollectionName, id);

                return await _client.ReadDocumentAsync<T>(documentUri);
            }
            catch (DocumentClientException de)
            {
                if (de.StatusCode == HttpStatusCode.NotFound)
                    return default(T);

                throw;
            }
        }

        public async Task<HttpStatusCode> DeleteAsync(string id)
        {
            try
            {
                var documentUri = UriFactory.CreateDocumentUri(_config.DatabaseName, _config.CollectionName, id);

                var response = await _client.DeleteDocumentAsync(documentUri);

                return response.StatusCode;
            }
            catch (DocumentClientException de)
            {
                if (de.StatusCode == HttpStatusCode.NotFound)
                    return HttpStatusCode.NotFound;

                throw;
            }
        }

        /// <summary>
        /// Searches the document db for documents that match the query.
        /// </summary>
        /// <param name="query">NOSQL query string to be executed against DocumentDB</param>
        /// <param name="parameters">Parameters to be inserted into query string</param>
        /// <returns>List of documents that match the input</returns>
        public async Task<IEnumerable<T>> SearchQueryAsync(string query, SqlParameterCollection parameters)
        {
            var collectionUri = UriFactory.CreateDocumentCollectionUri(_config.DatabaseName, _config.CollectionName);

            var documentQuery = _client.CreateDocumentQuery<T>(collectionUri,
                                new SqlQuerySpec()
                                {
                                    QueryText = query,
                                    Parameters = parameters
                                }).AsDocumentQuery();

            var batches = new List<IEnumerable<T>>();

            do
            {
                var batch = await documentQuery.ExecuteNextAsync<T>();
                batches.Add(batch);
            }
            while (documentQuery.HasMoreResults);

            return batches.SelectMany(b => b);
        }
    }
}
