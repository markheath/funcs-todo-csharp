### Azure Functions C# Bindings Sample

This project contains a simple CRUD (Create, Read, Update, Delete) REST API to manage TODO items, using the following options as backing store:

- In-memory implementation
- Blob storage
- Table storage
- Cosmos DB
- Entity Framework Core

It's implemented using Azure Functions V4 in C#, and can be run against the local storage emulator for Table or Blob storage, and the Cosmos DB emulator.

### Get set up

You'll need to set up a `local.settings.json` file containing connection strings for the Azure Storage and Azure CosmosDb emulators. There's a `local.settings.json.sample` file you can rename as a starting point.

```js
{
    "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "AzureWebJobsDashboard": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "SqlConnectionString": "Data Source=(LocalDB)\\MSSQLLocalDB;Integrated Security=true;Database=Todos",
    "CosmosDBConnection": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
  }
}
```

To test Cosmos, in the cosmos DB emulator, create a database called `tododb`, with a collection called `tasks` and a partition key of `/id`

To test the EF backing store, create a new `Todos` database and then run the `TodoTable.sql` SQL script against it to create the necessary table. (using SQL Server Object Explorer in Visual Studio is probably easiest)

### Testing Locally

You can run the `Test.ps1` PowerShell script to run a simple set of tests against the binding type of your choice, to create, read, update and delete todo items.

