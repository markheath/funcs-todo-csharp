### Azure Functions C# Bindings Sample

A simple CRUD (Create, Read, Update, Delete) API is created to manage TODO items, using the following options as backing store:

- in memory implementation
- blob storage
- table storage
- CosmosDb

It's implemented using Azure Functions V2 in C#, and can be run against the local storage emulator for table storage, and the CosmosDb emulator.