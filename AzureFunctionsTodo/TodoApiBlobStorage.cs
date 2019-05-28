using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace AzureFunctionsTodo
{
    public static class TodoApiBlobStorage
    {
        private const string Route = "blobtodo";
        private const string BlobPath = "todos";

        [FunctionName("Blob_CreateTodo")]
        public static async Task<IActionResult>CreateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = Route)]HttpRequest req, 
            [Blob(BlobPath, Connection="AzureWebJobsStorage")] CloudBlobContainer todoContainer,
            ILogger log)
        {
            log.LogInformation("Creating a new todo list item");
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
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route)]HttpRequest req,
            [Blob(BlobPath, Connection = "AzureWebJobsStorage")] CloudBlobContainer todoContainer, 
            ILogger log)
        {
            log.LogInformation("Getting todo list items");
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
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route + "/{id}")]HttpRequest req,
            [Blob(BlobPath + "/{id}.json", Connection = "AzureWebJobsStorage")] string json,
            ILogger log, string id)
        {
            log.LogInformation("Getting todo item by id");
            if (json == null)
            {
                log.LogInformation($"Item {id} not found");
                return new NotFoundResult();
            }
            return new OkObjectResult(JsonConvert.DeserializeObject<Todo>(json));
        }

        [FunctionName("Blob_UpdateTodo")]
        public static async Task<IActionResult> UpdateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = Route + "/{id}")]HttpRequest req,
            [Blob(BlobPath + "/{id}.json", Connection = "AzureWebJobsStorage")] CloudBlockBlob blob,
            ILogger log, string id)
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

        [FunctionName("Blob_DeleteTodo")]
        public static async Task<IActionResult> DeleteTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = Route + "/{id}")]HttpRequest req,
            [Blob(BlobPath + "/{id}.json", Connection = "AzureWebJobsStorage")] CloudBlockBlob blob,
            ILogger log, string id)
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
