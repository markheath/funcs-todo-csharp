using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace AzureFunctionsTodo
{

    public static class TodoApiInMemory
    {
        private static readonly List<Todo> Items = new List<Todo>();
        private const string Route = "memorytodo";

        [FunctionName("InMemory_CreateTodo")]
        public static async Task<IActionResult>CreateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = Route)]HttpRequest req, ILogger log)
        {
            log.LogInformation("Creating a new todo list item");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<TodoCreateModel>(requestBody);

            var todo = new Todo() { TaskDescription = input.TaskDescription };
            Items.Add(todo);
            return new OkObjectResult(todo);
        }

        [FunctionName("InMemory_GetTodos")]
        public static IActionResult GetTodos(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route)]HttpRequest req, ILogger log)
        {
            log.LogInformation("Getting todo list items");
            return new OkObjectResult(Items);
        }

        [FunctionName("InMemory_GetTodoById")]
        public static IActionResult GetTodoById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route + "/{id}")]HttpRequest req, ILogger log, string id)
        {
            var todo = Items.FirstOrDefault(t => t.Id == id);
            if (todo == null)
            {
                return new NotFoundResult();
            }
            return new OkObjectResult(todo);
        }

        [FunctionName("InMemory_UpdateTodo")]
        public static async Task<IActionResult> UpdateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = Route + "/{id}")]HttpRequest req, ILogger log, string id)
        {
            var todo = Items.FirstOrDefault(t => t.Id == id);
            if (todo == null)
            {
                return new NotFoundResult();
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updated = JsonConvert.DeserializeObject<TodoUpdateModel>(requestBody);

            todo.IsCompleted = updated.IsCompleted;
            if (!string.IsNullOrEmpty(updated.TaskDescription))
            {
                todo.TaskDescription = updated.TaskDescription;
            }

            return new OkObjectResult(todo);
        }

        [FunctionName("InMemory_DeleteTodo")]
        public static IActionResult DeleteTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = Route + "/{id}")]HttpRequest req, ILogger log, string id)
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
}
