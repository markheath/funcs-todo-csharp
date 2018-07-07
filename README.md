### Azure Functions C# Bindings Sample

A simple CRUD (Create, Read, Update, Delete) API is created to manage TODO items, using the following options as backing store:

- in memory implementation
- blob storage
- table storage
- CosmosDb

It's implemented using Azure Functions V2 in C#, and can be run against the local storage emulator for table storage, and the CosmosDb emulator.


You'll need to set up a `local.settings.json` file containing connection strings for the Azure Storage and Azure CosmosDb emulators

```js
{
    "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "AzureWebJobsDashboard": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "CosmosDBConnection": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
  }
}
```