using AzureFunctionsTodo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Container = Microsoft.Azure.Cosmos.Container;

namespace AzureFunctionsTodo.CosmosDb;

public class TodoApiCosmosDb
{
    private const string Route = "cosmostodo";
    private const string DatabaseName = "tododb";
    private const string CollectionName = "tasks";
    private readonly ILogger<TodoApiCosmosDb> logger;

    public TodoApiCosmosDb(ILogger<TodoApiCosmosDb> logger)
    {

        this.logger = logger;
    }


    public class CreateTodoResponse
    {
        public CreateTodoResponse(HttpResponse httpResponse, object[] todo)
        {
            Todo = todo;
            HttpResponse = httpResponse;
        }
        [CosmosDBOutput(DatabaseName, CollectionName, Connection = "CosmosDBConnection")]
        public object[] Todo { get; }

        public HttpResponse HttpResponse { get; }
    }

    [Function("CosmosDb_CreateTodo")]
    public async Task<CreateTodoResponse> CreateTodo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = Route)] HttpRequest req)
    {
        logger.LogInformation("Creating a new todo list item");
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonConvert.DeserializeObject<TodoCreateModel>(requestBody);
        var response = req.HttpContext.Response;
        if (input == null)
        {
            response.StatusCode = 400;
            await response.WriteAsync("Failed to deserialize request body");
            return new CreateTodoResponse(response, Array.Empty<object>());
        }
        var todo = new Todo() { TaskDescription = input.TaskDescription };
        response.StatusCode = 200;
        await response.WriteAsJsonAsync(todo);
        // the object we need to add has to have a lower case id 
        return new CreateTodoResponse(response, [new { id = todo.Id, todo.CreatedTime, todo.IsCompleted, todo.TaskDescription }]);
    }

    [Function("CosmosDb_GetTodos")]
    public IActionResult GetTodos(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route)] HttpRequest req,
        [CosmosDBInput(
                DatabaseName,
                CollectionName,
                Connection = "CosmosDBConnection",
                SqlQuery = "SELECT * FROM c order by c._ts desc")]
                IEnumerable<Todo> todos)
    {
        logger.LogInformation("Getting todo list items");
        return new OkObjectResult(todos);
    }

    [Function("CosmosDb_GetTodoById")]
    public IActionResult GetTodoById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route + "/{id}")] HttpRequest req,
        [CosmosDBInput(DatabaseName, CollectionName, Connection = "CosmosDBConnection", PartitionKey = "{id}",
                Id = "{id}")] Todo todo,
        string id)
    {
        logger.LogInformation("Getting todo item by id");

        if (todo == null)
        {
            logger.LogInformation($"Item {id} not found");
            return new NotFoundResult();
        }
        return new OkObjectResult(todo);
    }

    [Function("CosmosDb_UpdateTodo")]
    public async Task<IActionResult> UpdateTodo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = Route + "/{id}")] HttpRequest req,
        [CosmosDBInput(DatabaseName, CollectionName, Connection = "CosmosDBConnection")]
                Container client,
        string id)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var updated = JsonConvert.DeserializeObject<TodoUpdateModel>(requestBody);
        if (updated == null)
        {
            return new BadRequestObjectResult("Failed to deserialize request body");
        }
        try
        {
            var response = await client.ReadItemAsync<Todo>(id, new PartitionKey(id));
            var todoDocument = response.Resource;
            todoDocument.IsCompleted = updated.IsCompleted;
            if (!string.IsNullOrEmpty(updated.TaskDescription))
            {
                todoDocument.TaskDescription = updated.TaskDescription;
            }

            var updateDoc = new { id = todoDocument.Id, todoDocument.CreatedTime, todoDocument.IsCompleted, todoDocument.TaskDescription };

            await client.ReplaceItemAsync(updateDoc, id, new PartitionKey(id));
            return new OkObjectResult(todoDocument);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new NotFoundResult();
        }
    }

    [Function("CosmosDb_DeleteTodo")]
    public async Task<IActionResult> DeleteTodo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = Route + "/{id}")] HttpRequest req,
        [CosmosDBInput(DatabaseName, CollectionName, Connection = "CosmosDBConnection")]
                Container client,
        string id)
    {
        var deleteResponse = await client.DeleteItemAsync<Todo>(id, new PartitionKey(id));
        // note: deleteResponse.StatusCode == HttpStatusCode.NoContent means the item existed and we deleted it OK
        return new OkResult();
    }
}
