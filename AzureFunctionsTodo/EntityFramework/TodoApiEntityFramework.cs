using AzureFunctionsTodo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureFunctionsTodo.EntityFramework;

public class TodoApiEntityFramework
{
    private const string Route = "eftodo";
    private readonly TodoContext todoContext;
    private readonly ILogger<TodoApiEntityFramework> logger;

    public TodoApiEntityFramework(TodoContext todoContext, ILogger<TodoApiEntityFramework> logger)
    {
        this.todoContext = todoContext;
        this.logger = logger;
    }

    [Function("EntityFramework_CreateTodo")]
    public async Task<IActionResult> CreateTodo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = Route)] HttpRequest req)
    {
        logger.LogInformation("Creating a new todo list item");
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonConvert.DeserializeObject<TodoCreateModel>(requestBody);
        if (input == null)
        {
            return new BadRequestObjectResult("Failed to deserialize request body");
        }
        var todo = new TodoEf { TaskDescription = input.TaskDescription };
        await todoContext.Todos.AddAsync(todo);
        await todoContext.SaveChangesAsync();
        return new OkObjectResult(todo);
    }

    [Function("EntityFramework_GetTodos")]
    public async Task<IActionResult> GetTodos(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route)] HttpRequest req)
    {
        logger.LogInformation("Getting todo list items");
        var todos = await todoContext.Todos.ToListAsync();
        return new OkObjectResult(todos);
    }

    [Function("EntityFramework_GetTodoById")]
    public async Task<IActionResult> GetTodoById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route + "/{id}")] HttpRequest req,
        string id)
    {
        logger.LogInformation("Getting todo item by id");
        var todo = await todoContext.Todos.FindAsync(Guid.Parse(id));
        if (todo == null)
        {
            logger.LogInformation($"Item {id} not found");
            return new NotFoundResult();
        }
        return new OkObjectResult(todo);
    }

    [Function("EntityFramework_UpdateTodo")]
    public async Task<IActionResult> UpdateTodo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = Route + "/{id}")] HttpRequest req,
        ILogger log, string id)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var updated = JsonConvert.DeserializeObject<TodoUpdateModel>(requestBody);
        if (updated == null)
        {
            return new BadRequestObjectResult("Failed to deserialize request body");
        }
        var todo = await todoContext.Todos.FindAsync(Guid.Parse(id));
        if (todo == null)
        {
            log.LogWarning($"Item {id} not found");
            return new NotFoundResult();
        }

        todo.IsCompleted = updated.IsCompleted;
        if (!string.IsNullOrEmpty(updated.TaskDescription))
        {
            todo.TaskDescription = updated.TaskDescription;
        }

        await todoContext.SaveChangesAsync();

        return new OkObjectResult(todo);
    }

    [Function("EntityFramework_DeleteTodo")]
    public async Task<IActionResult> DeleteTodo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = Route + "/{id}")] HttpRequest req,
        string id)
    {
        var todo = await todoContext.Todos.FindAsync(Guid.Parse(id));
        if (todo == null)
        {
            logger.LogWarning($"Item {id} not found");
            return new NotFoundResult();
        }

        todoContext.Todos.Remove(todo);
        await todoContext.SaveChangesAsync();
        return new OkResult();
    }
}
