using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using AzureFunctionsTodo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureFunctionsTodo.BlobStorage
{
    public static class TodoApiBlobStorage
    {
        private const string Route = "blobtodo";
        private const string BlobPath = "todos";

        [FunctionName("Blob_CreateTodo")]
        public static async Task<IActionResult>CreateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = Route)]HttpRequest req, 
            [Blob(BlobPath, Connection="AzureWebJobsStorage")] BlobContainerClient todoContainer,
            ILogger log)
        {
            log.LogInformation("Creating a new todo list item");
            await todoContainer.CreateIfNotExistsAsync();
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<TodoCreateModel>(requestBody);
            var todo = new Todo() { TaskDescription = input.TaskDescription };

            var blob = todoContainer.GetBlobClient($"{todo.Id}.json");
            await blob.UploadTextAsync(JsonConvert.SerializeObject(todo));

            return new OkObjectResult(todo);
        }

        [FunctionName("Blob_GetTodos")]
        public static async Task<IActionResult> GetTodos(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route)]HttpRequest req,
            [Blob(BlobPath, Connection = "AzureWebJobsStorage")] BlobContainerClient todoContainer, 
            ILogger log)
        {
            log.LogInformation("Getting todo list items");
            await todoContainer.CreateIfNotExistsAsync();

            var todos = new List<Todo>();
            await foreach(var result in todoContainer.GetBlobsAsync())
            {
                var blob = todoContainer.GetBlobClient(result.Name);
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
            [Blob(BlobPath + "/{id}.json", Connection = "AzureWebJobsStorage")] BlobClient blob,
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
            [Blob(BlobPath + "/{id}.json", Connection = "AzureWebJobsStorage")] BlobClient blob,
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
