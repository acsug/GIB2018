using Newtonsoft.Json;

namespace GIB2018API.Model.Config
{
    public class CosmosDbConfig
    {
        /// <summary>
        /// Gets or sets the endpoint to the DocumentDB.
        /// </summary>
        /// <value>The DocumentDB endpoint.</value>
        [JsonProperty(PropertyName = "endpoint")]
        [JsonRequired]
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the authorization key.
        /// </summary>
        /// <value>The authorization key.</value>
        [JsonProperty(PropertyName = "authKey")]
        [JsonRequired]
        public string AuthKey { get; set; }

        /// <summary>
        /// Gets or sets the database name.
        /// </summary>
        /// <value>The database name.</value>
        [JsonProperty(PropertyName = "databaseName")]
        [JsonRequired]
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the collection name.
        /// </summary>
        [JsonProperty(PropertyName = "collectionName")]
        [JsonRequired]
        public string CollectionName { get; set; }
    }
}
