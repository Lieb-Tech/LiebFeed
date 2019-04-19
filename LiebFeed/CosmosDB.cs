using System;
using System.Collections.Generic;
using System.Text;

using System.Net;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;
using System.IO;

namespace LiebFeed
{
    public class CosmosSetting
    {
        public CosmosDB Cosmos { get; set; }
        public class CosmosDB
        {
            public string EndpointUrl { get; set; }
            public string PrimaryKey { get; set; }
        }
    }

    public class CosmosDB
    {
        private DocumentClient client;

        // ADD THIS PART TO YOUR CODE
        public CosmosDB()
        {
            var data = File.ReadAllText("cosmosDbKey.json");
            var config = JsonConvert.DeserializeObject<CosmosSetting>(data);

            this.client = new DocumentClient(new Uri(config.Cosmos.EndpointUrl), config.Cosmos.PrimaryKey, new ConnectionPolicy
            {
                ConnectionMode = ConnectionMode.Direct,
                ConnectionProtocol = Protocol.Tcp
            });
        }

        public void OpenConnection()
        {
            client.OpenAsync().Wait();
        }

        public IQueryable<dynamic> GetDocumentQuery(string collection, string query)
        {
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true };
            return client.CreateDocumentQuery<dynamic>(
                GetCollectionLink(collection),
                query,
                queryOptions);
        }

        public IQueryable<T> GetDocumentQuery<T>(string collection)
        {
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true };
            return client.CreateDocumentQuery<T>(this.GetCollectionLink(collection), queryOptions);
        }

        public Uri GetCollectionLink(string collectionName)
        {
            return UriFactory.CreateDocumentCollectionUri("liebfeeds", collectionName);
        }

        public async Task UpsertDocument(dynamic doc, string collection)
        {            
            try
            {                
                var res = await client.UpsertDocumentAsync(GetCollectionLink(collection), doc);
                var a = "";
            }
            catch (Exception ex)
            {
                System.Threading.Thread.Sleep(2000);                
                try
                {
                    await client.UpsertDocumentAsync(GetCollectionLink(collection), doc);
                }
                catch (Exception ex2)
                {
                    System.Threading.Thread.Sleep(2000);
                    try
                    {
                        await client.UpsertDocumentAsync(GetCollectionLink(collection), doc);
                    }
                    catch (Exception ex3)
                    {
                    }
                }
            }
        }
        public async Task ReplaceDocument(dynamic doc, string collection)
        {
            try
            {
                await client.ReplaceDocumentAsync(GetCollectionLink(collection), doc);
                var a = "";
            }
            catch (Exception ex)
            {
                System.Threading.Thread.Sleep(2000);
                try
                {
                    await client.UpsertDocumentAsync(GetCollectionLink(collection), doc);
                }
                catch (Exception ex2)
                {
                    System.Threading.Thread.Sleep(2000);
                    try
                    {
                        await client.UpsertDocumentAsync(GetCollectionLink(collection), doc);
                    }
                    catch (Exception ex3)
                    {
                    }
                }
            }
        }
    }
}
