using Azure.Storage.Blobs;
using AzureFunctionsTodo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureFunctionsTodo.BlobStorage;

public class TodoApiBlobStorage
{
    private const string Route = "blobtodo";
    private const string BlobPath = "todos";
    private readonly ILogger<TodoApiBlobStorage> logger;

    public TodoApiBlobStorage(ILogger<TodoApiBlobStorage> logger)
    {
        this.logger = logger;
    }


    [Function("Blob_CreateTodo")]
    public async Task<IActionResult> CreateTodo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = Route)] HttpRequest req,
        [BlobInput(BlobPath, Connection = "AzureWebJobsStorage")] BlobContainerClient todoContainer)
    {
        logger.LogInformation("Creating a new todo list item");
        await todoContainer.CreateIfNotExistsAsync();
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonConvert.DeserializeObject<TodoCreateModel>(requestBody);
        if (input == null)
        {
            return new BadRequestObjectResult("Failed to deserialize request body");
        }
        var todo = new Todo() { TaskDescription = input.TaskDescription };

        var blob = todoContainer.GetBlobClient($"{todo.Id}.json");
        await blob.UploadTextAsync(JsonConvert.SerializeObject(todo));

        return new OkObjectResult(todo);
    }

    [Function("Blob_GetTodos")]
    public async Task<IActionResult> GetTodos(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route)] HttpRequest req,
        [BlobInput(BlobPath, Connection = "AzureWebJobsStorage")] BlobContainerClient todoContainer)
    {
        logger.LogInformation("Getting todo list items");
        await todoContainer.CreateIfNotExistsAsync();

        var todos = new List<Todo>();
        await foreach (var result in todoContainer.GetBlobsAsync())
        {
            var blob = todoContainer.GetBlobClient(result.Name);
            var json = await blob.DownloadTextAsync();
            var todoItem = JsonConvert.DeserializeObject<Todo>(json);
            if (todoItem == null)
            {
                logger.LogError($"failed to deserialize TODO from {result.Name}");
                return new StatusCodeResult(500);
            }
            todos.Add(todoItem);
        }
        return new OkObjectResult(todos);
    }

    [Function("Blob_GetTodoById")]
    public IActionResult GetTodoById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route + "/{id}")] HttpRequest req,
        [BlobInput(BlobPath + "/{id}.json", Connection = "AzureWebJobsStorage")] string json,
        string id)
    {
        logger.LogInformation("Getting todo item by id");
        if (json == null)
        {
            logger.LogInformation($"Item {id} not found");
            return new NotFoundResult();
        }
        return new OkObjectResult(JsonConvert.DeserializeObject<Todo>(json));
    }

    [Function("Blob_UpdateTodo")]
    public async Task<IActionResult> UpdateTodo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = Route + "/{id}")] HttpRequest req,
        [BlobInput(BlobPath + "/{id}.json", Connection = "AzureWebJobsStorage")] BlobClient blob,
        string id)
    {

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var updated = JsonConvert.DeserializeObject<TodoUpdateModel>(requestBody);
        if (updated == null)
        {
            return new BadRequestObjectResult("Failed to deserialize request body");
        }
        if (!await blob.ExistsAsync())
        {
            return new NotFoundResult();
        }
        var existingText = await blob.DownloadTextAsync();
        var existingTodo = JsonConvert.DeserializeObject<Todo>(existingText);
        if (existingTodo == null)
        {
            return new BadRequestObjectResult("Failed to deserialize request body");
        }
        existingTodo.IsCompleted = updated.IsCompleted;
        if (!string.IsNullOrEmpty(updated.TaskDescription))
        {
            existingTodo.TaskDescription = updated.TaskDescription;
        }

        await blob.UploadTextAsync(JsonConvert.SerializeObject(existingTodo));

        return new OkObjectResult(existingTodo);
    }

    [Function("Blob_DeleteTodo")]
    public async Task<IActionResult> DeleteTodo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = Route + "/{id}")] HttpRequest req,
        [BlobInput(BlobPath + "/{id}.json", Connection = "AzureWebJobsStorage")] BlobClient blob,
        string id)
    {
        if (!await blob.ExistsAsync())
        {
            return new NotFoundResult();
        }
        await blob.DeleteAsync();
        return new OkResult();
    }
}
