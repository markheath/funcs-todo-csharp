using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;

namespace AzureFunctionsTodo
{

    public static class TodoApiCosmosDb
    {
        private const string route = "todo4";

        [FunctionName("CosmosDb_CreateTodo")]
        public static async Task<IActionResult>CreateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = route)]HttpRequest req,
            [CosmosDB(
                databaseName: "tododb",
                collectionName: "tasks",
                ConnectionStringSetting = "CosmosDBConnection")]
            IAsyncCollector<Todo> todos, TraceWriter log)
        {
            log.Info("Creating a new todo list item");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<TodoCreateModel>(requestBody);

            var todo = new Todo() { TaskDescription = input.TaskDescription };
            await todos.AddAsync(todo);
            return new OkObjectResult(todo);
        }

        [FunctionName("CosmosDb_GetTodos")]
        public static async Task<IActionResult> GetTodos(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = route)]HttpRequest req,
            [CosmosDB(
                databaseName: "tododb",
                collectionName: "tasks",
                ConnectionStringSetting = "CosmosDBConnection",
                SqlQuery = "SELECT * FROM c order by c._ts desc")]
                IEnumerable<Todo> todos,
            TraceWriter log)
        {
            log.Info("Getting todo list items");
            return new OkObjectResult(todos);
        }

        [FunctionName("CosmosDb_GetTodoById")]
        public static IActionResult GetTodoById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = route + "/{id}")]HttpRequest req,
            [CosmosDB(databaseName: "tododb", collectionName: "tasks", ConnectionStringSetting = "CosmosDBConnection",
                Id = "{id}")] Todo todo,
            TraceWriter log, string id)
        {
            log.Info("Getting todo item by id");

            if (todo == null)
            {
                log.Info($"Item {id} not found");
                return new NotFoundResult();
            }
            return new OkObjectResult(todo);
        }

        [FunctionName("CosmosDb_UpdateTodo")]
        public static async Task<IActionResult> UpdateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = route + "/{id}")]HttpRequest req,
            [CosmosDB(databaseName: "tododb", collectionName: "tasks", ConnectionStringSetting = "CosmosDBConnection",
                Id = "{id}")] Todo todo,
            TraceWriter log, string id)
        {

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updated = JsonConvert.DeserializeObject<TodoUpdateModel>(requestBody);
            if (todo == null)
            {
                return new NotFoundResult();
            }
            
            todo.IsCompleted = updated.IsCompleted;
            if (!string.IsNullOrEmpty(updated.TaskDescription))
            {
                todo.TaskDescription = updated.TaskDescription;
            }

            // update not implemented yet

            return new OkObjectResult(todo);
        }

        [FunctionName("CosmosDb_DeleteTodo")]
        public static IActionResult DeleteTodo2(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = route + "/{id}")]HttpRequest req,
            [CosmosDB(databaseName: "tododb", collectionName: "tasks", ConnectionStringSetting = "CosmosDBConnection",
                Id = "{id}")] Todo todo,
            TraceWriter log, string id)
        {
            
            if (todo == null)
            {
                return new NotFoundResult();
            }
            // delete not implemented - may need to bind to DocumentClient
            return new OkResult();
        }
    }
}
