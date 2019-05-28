using System;
using System.IO;
using System.Threading.Tasks;
using AzureFunctionsTodo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureFunctionsTodo.EntityFramework
{

    public class TodoApiEntityFramework
    {
        private const string Route = "eftodo";
        private readonly TodoContext todoContext;

        public TodoApiEntityFramework(TodoContext todoContext)
        {
            this.todoContext = todoContext;
        }

        [FunctionName("EntityFramework_CreateTodo")]
        public async Task<IActionResult>CreateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = Route)]HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Creating a new todo list item");
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<TodoCreateModel>(requestBody);
            var todo = new TodoEf { TaskDescription = input.TaskDescription };
            await todoContext.Todos.AddAsync(todo);
            await todoContext.SaveChangesAsync();
            return new OkObjectResult(todo);
        }

        [FunctionName("EntityFramework_GetTodos")]
        public async Task<IActionResult> GetTodos(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route)]HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Getting todo list items");
            var todos = await todoContext.Todos.ToListAsync();
            return new OkObjectResult(todos);
        }

        [FunctionName("EntityFramework_GetTodoById")]
        public async Task<IActionResult> GetTodoById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route + "/{id}")]HttpRequest req,
            ILogger log, string id)
        {
            log.LogInformation("Getting todo item by id");
            var todo = await todoContext.Todos.FindAsync(Guid.Parse(id));
            if (todo == null)
            {
                log.LogInformation($"Item {id} not found");
                return new NotFoundResult();
            }
            return new OkObjectResult(todo);
        }

        [FunctionName("EntityFramework_UpdateTodo")]
        public async Task<IActionResult> UpdateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = Route + "/{id}")]HttpRequest req,
            ILogger log, string id)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updated = JsonConvert.DeserializeObject<TodoUpdateModel>(requestBody);
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

        [FunctionName("EntityFramework_DeleteTodo")]
        public async Task<IActionResult> DeleteTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = Route + "/{id}")]HttpRequest req,
            ILogger log, string id)
        {
            var todo = await todoContext.Todos.FindAsync(Guid.Parse(id));
            if (todo == null)
            {
                log.LogWarning($"Item {id} not found");
                return new NotFoundResult();
            }

            todoContext.Todos.Remove(todo);
            await todoContext.SaveChangesAsync();
            return new OkResult();
        }
    }
}
