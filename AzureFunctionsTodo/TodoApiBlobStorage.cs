using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;

namespace AzureFunctionsTodo
{

    public static class TodoApiBlobStorage
    {
        private const string route = "todo3";

        [FunctionName("Blob_CreateTodo")]
        public static async Task<IActionResult>CreateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = route)]HttpRequest req, 
            [Blob("todos", Connection="AzureWebJobsStorage")] CloudBlobContainer todoContainer,
            TraceWriter log)
        {
            log.Info("Creating a new todo list item");
            await todoContainer.CreateIfNotExistsAsync();
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<TodoCreateModel>(requestBody);
            var todo = new Todo() { TaskDescription = input.TaskDescription };

            var blob = todoContainer.GetBlockBlobReference($"{todo.Id}.json");
            await blob.UploadTextAsync(JsonConvert.SerializeObject(todo));

            return new OkObjectResult(todo);
        }

        [FunctionName("Blob_GetTodos")]
        public static async Task<IActionResult> GetTodos(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = route)]HttpRequest req,
            [Blob("todos", Connection = "AzureWebJobsStorage")] CloudBlobContainer todoContainer, 
            TraceWriter log)
        {
            log.Info("Getting todo list items");
            await todoContainer.CreateIfNotExistsAsync();
            var segment = await todoContainer.ListBlobsSegmentedAsync(null);

            var todos = new List<Todo>();
            foreach(var result in segment.Results)
            {
                var blob = todoContainer.GetBlockBlobReference(result.Uri.Segments.Last());
                var json  = await blob.DownloadTextAsync();
                todos.Add(JsonConvert.DeserializeObject<Todo>(json));
            }
            return new OkObjectResult(todos);
        }

        [FunctionName("Blob_GetTodoById")]
        public static IActionResult GetTodoById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = route + "/{id}")]HttpRequest req,
            [Blob("todos/{id}.json", Connection = "AzureWebJobsStorage")] string json,
            TraceWriter log, string id)
        {
            log.Info("Getting todo item by id");
            if (json == null)
            {
                log.Info($"Item {id} not found");
                return new NotFoundResult();
            }
            return new OkObjectResult(JsonConvert.DeserializeObject<Todo>(json));
        }

        [FunctionName("Blob_UpdateTodo")]
        public static async Task<IActionResult> UpdateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = route + "/{id}")]HttpRequest req,
            [Blob("todos/{id}.json", Connection = "AzureWebJobsStorage")] CloudBlockBlob blob,
            TraceWriter log, string id)
        {

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updated = JsonConvert.DeserializeObject<TodoUpdateModel>(requestBody);

            if (!await blob.ExistsAsync())
            {
                return new NotFoundResult();
            }
            var existingText = await blob.DownloadTextAsync();
            var existingTodo = JsonConvert.DeserializeObject<Todo>(existingText);

            existingTodo.IsCompleted = updated.IsCompleted;
            if (!string.IsNullOrEmpty(updated.TaskDescription))
            {
                existingTodo.TaskDescription = updated.TaskDescription;
            }

            await blob.UploadTextAsync(JsonConvert.SerializeObject(existingTodo));

            return new OkObjectResult(existingTodo);
        }

        [FunctionName("Blob_DeleteTodo")] // n.b. strange issue if this function is just called DeleteTodo - causes functions runtime to fall over
        public static async Task<IActionResult> DeleteTodo3(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = route + "/{id}")]HttpRequest req,
            [Blob("todos/{id}.json", Connection = "AzureWebJobsStorage")] CloudBlockBlob blob,
            TraceWriter log, string id)
        {
            if(!await blob.ExistsAsync())
            {
                return new NotFoundResult();
            }
            await blob.DeleteAsync();
            return new OkResult();
        }
    }
}
