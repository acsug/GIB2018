#r "Microsoft.Azure.Documents.Client"
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Microsoft.IdentityModel.Clients.ActiveDirectory;

using Microsoft.Azure.Documents;
using Microsoft.Azure.DataLake.Store;
using Microsoft.Azure.Management.DataLake.Store;
using Microsoft.Azure.Management.DataLake.Store.Models;

using Microsoft.Rest.Azure.Authentication;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static async Task Run(IReadOnlyList<Document> documents, TraceWriter log)
{
    if (documents == null || documents.Count == 0)
    {
        log.Verbose($"Sync function triggered - no data to process.");
        return;
    }

    log.Info($"Sync function triggered.");
    log.Verbose("Document count: " + documents.Count);

    // These keys and ids should come from configuration

    // see: https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-create-service-principal-portal#get-tenant-id
    var tenantId = "**";           // the ID of the Azure AD that the client is in

    // see: https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-create-service-principal-portal#get-application-id-and-authentication-key
    var clientid = "**";           // The id of the application added to Azure AD
    var secretKey = "**";          // The key related to the client
    var datalakeUrl = "**.azuredatalakestore.net";

    var creds = new ClientCredential(clientid, secretKey);
    var clientCreds = await ApplicationTokenProvider.LoginSilentAsync(tenantId, creds);

    // Create ADLS client object
    var client = AdlsClient.CreateClient(datalakeUrl, clientCreds);
    var successCount = 0;
    var failedCount = 0;

    var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    foreach (var document in documents)
    {
        try
        {
            var docType = document.GetPropertyValue<string>("@type");

            if (string.IsNullOrEmpty(docType))
            {
                log.Error($"The type for document '{document.Id}' is unknown. Rejecting the document.");
                continue;
            }
            log.Verbose($"Uploading document '{document.Id}' of type '{docType}' to the data lake");

            // null out the Cosmos DB fields
            document.SetPropertyValue("_etag", null);
            document.SetPropertyValue("_lsn", null);
            document.SetPropertyValue("_rid", null);
            document.SetPropertyValue("_self", null);
            document.SetPropertyValue("_ts", null);

            //Create json bytes to save to the Data Lake
            var json = JsonConvert.SerializeObject(document);

            //Remove the Cosmos DB fields
            json = json.Replace(",\"_rid\":null,\"_self\":null,\"_ts\":0,\"_etag\":null", "");
            json = json.Replace(",\"_lsn\":0", "");
            byte[] toBytes = Encoding.ASCII.GetBytes(json);

            // All documents are stored in the folder by their type and using the id of the document.

            log.Verbose($"Document '{document.Id}' => document Type: '{docType}'");

            //Create a file - automatically creates any parent directories that don't exist
            var fileName = $"/Documents/{docType}/{document.Id}.json";
            using (var binaryWriter = new BinaryWriter(client.CreateFile(fileName, IfExists.Overwrite)))
            {
                binaryWriter.Write(toBytes);
            }

            successCount++;
        }
        catch(Exception ex)
        {
            failedCount++;
            log.Verbose("Catching");
            log.Error(ex.Message);
        }
    }

    var rejectedCount = documents.Count - successCount - successCount;
    log.Info($"Sync function complete. {successCount} successfully processed, {failedCount} failed and {rejectedCount} were rejected.");
}
