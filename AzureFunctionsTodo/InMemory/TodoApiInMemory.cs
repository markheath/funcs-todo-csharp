using AzureFunctionsTodo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureFunctionsTodo.InMemory;

public class TodoApiInMemory
{
    private static readonly List<Todo> Items = new List<Todo>();
    private readonly ILogger<TodoApiInMemory> logger;
    private const string Route = "memorytodo";

    public TodoApiInMemory(ILogger<TodoApiInMemory> logger)
    {
        this.logger = logger;
    }

    [Function("InMemory_CreateTodo")]
    public async Task<IActionResult> CreateTodo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = Route)] HttpRequest req)
    {
        logger.LogInformation("Creating a new todo list item");
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonConvert.DeserializeObject<TodoCreateModel>(requestBody);
        if (input == null)
        {
            return new BadRequestObjectResult("Failed to deserialize request body");
        }
        var todo = new Todo() { TaskDescription = input.TaskDescription };
        Items.Add(todo);
        return new OkObjectResult(todo);
    }

    [Function("InMemory_GetTodos")]
    public IActionResult GetTodos(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route)] HttpRequest req)
    {
        logger.LogInformation("Getting todo list items");
        return new OkObjectResult(Items);
    }

    [Function("InMemory_GetTodoById")]
    public IActionResult GetTodoById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route + "/{id}")] HttpRequest req, string id)
    {
        var todo = Items.FirstOrDefault(t => t.Id == id);
        if (todo == null)
        {
            return new NotFoundResult();
        }
        return new OkObjectResult(todo);
    }

    [Function("InMemory_UpdateTodo")]
    public async Task<IActionResult> UpdateTodo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = Route + "/{id}")] HttpRequest req, string id)
    {
        var todo = Items.FirstOrDefault(t => t.Id == id);
        if (todo == null)
        {
            return new NotFoundResult();
        }

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var updated = JsonConvert.DeserializeObject<TodoUpdateModel>(requestBody);
        if (updated == null)
        {
            return new BadRequestObjectResult("Failed to deserialize request body");
        }
        todo.IsCompleted = updated.IsCompleted;
        if (!string.IsNullOrEmpty(updated.TaskDescription))
        {
            todo.TaskDescription = updated.TaskDescription;
        }

        return new OkObjectResult(todo);
    }

    [Function("InMemory_DeleteTodo")]
    public IActionResult DeleteTodo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = Route + "/{id}")] HttpRequest req, string id)
    {
        var todo = Items.FirstOrDefault(t => t.Id == id);
        if (todo == null)
        {
            return new NotFoundResult();
        }
        Items.Remove(todo);
        return new OkResult();
    }
}
