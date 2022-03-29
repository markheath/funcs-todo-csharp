using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using AzureFunctionsTodo.EntityFramework;
using AzureFunctionsTodo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureFunctionsTodo.TableStorage
{

    public static class TodoApiTableStorage
    {
        private const string Route = "tabletodo";
        private const string TableName = "todos";
        private const string PartitionKey = "TODO";

        [FunctionName("Table_CreateTodo")]
        public static async Task<IActionResult>CreateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = Route)]HttpRequest req, 
            [Table(TableName, Connection="AzureWebJobsStorage")] IAsyncCollector<TodoTableEntity> todoTable,
            ILogger log)
        {
            log.LogInformation("Creating a new todo list item");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<TodoCreateModel>(requestBody);

            var todo = new Todo() { TaskDescription = input.TaskDescription };
            await todoTable.AddAsync(todo.ToTableEntity());
            return new OkObjectResult(todo);
        }

        [FunctionName("Table_GetTodos")]
        public static async Task<IActionResult> GetTodos(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route)]HttpRequest req,
            // unfortunately IQueryable<TodoTableEntity> binding not supported in functions v2
            [Table(TableName, Connection = "AzureWebJobsStorage")] TableClient todoTable, 
            ILogger log)
        {
            // await todoTable.CreateIfNotExistsAsync();
            log.LogInformation("Getting todo list items");
            var page1 = await todoTable.QueryAsync<TodoTableEntity>().AsPages().FirstAsync();
            
            return new OkObjectResult(page1.Values.Select(Mappings.ToTodo));
        }

        [FunctionName("Table_GetTodoById")]
        public static IActionResult GetTodoById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route + "/{id}")]HttpRequest req,
            [Table(TableName, "TODO", "{id}", Connection = "AzureWebJobsStorage")] TodoTableEntity todo,
            ILogger log, string id)
        {
            log.LogInformation("Getting todo item by id");
            if (todo == null)
            {
                log.LogInformation($"Item {id} not found");
                return new NotFoundResult();
            }
            return new OkObjectResult(todo.ToTodo());
        }

        [FunctionName("Table_UpdateTodo")]
        public static async Task<IActionResult> UpdateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = Route + "/{id}")]HttpRequest req,
            [Table(TableName, Connection = "AzureWebJobsStorage")] TableClient todoTable,
            ILogger log, string id)
        {

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updated = JsonConvert.DeserializeObject<TodoUpdateModel>(requestBody);
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

        [FunctionName("Table_DeleteTodo")]
        public static async Task<IActionResult> DeleteTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = Route + "/{id}")]HttpRequest req,
            [Table(TableName, Connection = "AzureWebJobsStorage")] TableClient todoTable,
            ILogger log, string id)
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
}
