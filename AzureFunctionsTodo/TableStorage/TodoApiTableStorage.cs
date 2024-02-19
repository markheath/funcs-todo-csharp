using Azure;
using Azure.Data.Tables;
using AzureFunctionsTodo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureFunctionsTodo.TableStorage;

public class TodoApiTableStorage
{
    private const string Route = "tabletodo";
    private const string TableName = "todos";
    private const string PartitionKey = "TODO";
    private readonly ILogger<TodoApiTableStorage> logger;

    public TodoApiTableStorage(ILogger<TodoApiTableStorage> logger)
    {
        this.logger = logger;
    }


    public class CreateTodoResponse
    {
        public CreateTodoResponse(HttpResponse response, TodoTableEntity[] newEntity)
        {
            HttpResponse = response;
            NewEntity = newEntity;
        }

        public HttpResponse HttpResponse { get;  }
        
        [TableOutput(TableName, Connection = "AzureWebJobsStorage")]
        public TodoTableEntity[] NewEntity { get;  }
    }


    [Function("Table_CreateTodo")]
    public async Task<CreateTodoResponse> CreateTodo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = Route)] HttpRequest req)
    {
        logger.LogInformation("Creating a new todo list item");
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonConvert.DeserializeObject<TodoCreateModel>(requestBody);
        var response = req.HttpContext.Response;
        if (input == null)
        {
            response.StatusCode = 400;
            await response.WriteAsync("Failed to deserialize request body");
            return new CreateTodoResponse(
                response,
                Array.Empty<TodoTableEntity>());
        }

        var todo = new Todo() { TaskDescription = input.TaskDescription };
        response.StatusCode = 200;
        await response.WriteAsJsonAsync(todo);
        return new CreateTodoResponse(response, [todo.ToTableEntity()]);
    }

    [Function("Table_GetTodos")]
    public async Task<IActionResult> GetTodos(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route)] HttpRequest req,
        // unfortunately IQueryable<TodoTableEntity> binding not supported in functions v2
        [TableInput(TableName, Connection = "AzureWebJobsStorage")] TableClient todoTable)
    {
        // await todoTable.CreateIfNotExistsAsync();
        logger.LogInformation("Getting todo list items");
        var page1 = await todoTable.QueryAsync<TodoTableEntity>().AsPages().FirstAsync();

        return new OkObjectResult(page1.Values.Select(Mappings.ToTodo));
    }

    // note - seemingly not working at the moment due to bug https://github.com/Azure/azure-functions-dotnet-worker/issues/1233
    /*[Function("Table_GetTodoById")]
    public IActionResult GetTodoById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route + "/{id}")] HttpRequest req,
        [TableInput(TableName, PartitionKey, "{id}", Connection = "AzureWebJobsStorage")] TodoTableEntity todo,
        string id)
    {
        logger.LogInformation($"Getting todo item by id {id}");
        if (todo == null)
        {
            logger.LogInformation($"Item {id} not found");
            return new NotFoundResult();
        }
        return new OkObjectResult(todo.ToTodo());
    }*/

    // alternative implementation to work around the bug with TableInput binding for single entity
    [Function("Table_GetTodoById")]
    public async Task<IActionResult> GetTodoById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route + "/{id}")] HttpRequest req,
        [TableInput(TableName, Connection = "AzureWebJobsStorage")] TableClient todoTable,
        string id)
    {
        logger.LogInformation($"Getting todo item by id {id}");
        TodoTableEntity existingRow;
        try
        {
            var findResult = await todoTable.GetEntityAsync<TodoTableEntity>(PartitionKey, id);
            existingRow = findResult.Value;
        }
        catch (RequestFailedException e) when (e.Status == 404)
        {
            return new NotFoundResult();
        }
        return new OkObjectResult(existingRow.ToTodo());
    }

    [Function("Table_UpdateTodo")]
    public async Task<IActionResult> UpdateTodo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = Route + "/{id}")] HttpRequest req,
        [TableInput(TableName, Connection = "AzureWebJobsStorage")] TableClient todoTable,
        string id)
    {

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var updated = JsonConvert.DeserializeObject<TodoUpdateModel>(requestBody);
        if (updated == null)
        {
            return new BadRequestObjectResult("Please pass a valid TodoUpdateModel in the request body");
        }
        TodoTableEntity existingRow;
        try
        {
            var findResult = await todoTable.GetEntityAsync<TodoTableEntity>(PartitionKey, id);
            existingRow = findResult.Value;
        }
        catch (RequestFailedException e) when (e.Status == 404)
        {
            return new NotFoundResult();
        }

        existingRow.IsCompleted = updated.IsCompleted;
        if (!string.IsNullOrEmpty(updated.TaskDescription))
        {
            existingRow.TaskDescription = updated.TaskDescription;
        }

        await todoTable.UpdateEntityAsync(existingRow, existingRow.ETag, TableUpdateMode.Replace);

        return new OkObjectResult(existingRow.ToTodo());
    }

    [Function("Table_DeleteTodo")]
    public static async Task<IActionResult> DeleteTodo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = Route + "/{id}")] HttpRequest req,
        [TableInput(TableName, Connection = "AzureWebJobsStorage")] TableClient todoTable,
        string id)
    {
        try
        {
            await todoTable.DeleteEntityAsync(PartitionKey, id, ETag.All);
        }
        catch (RequestFailedException e) when (e.Status == 404)
        {
            return new NotFoundResult();
        }
        return new OkResult();
    }
}
