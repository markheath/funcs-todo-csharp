using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace AzureFunctionsTodo
{
    public class TodoDbContext : DbContext
    {
        public TodoDbContext(DbContextOptions<TodoDbContext> options)
            : base(options)
        {
        }

        public DbSet<Todo> Todo { get; set; }
    }


    /// <summary>
    /// non-static - we're using dependency injection
    /// </summary>
    public class TodoApiEntityFrameworkCore
    {
        private readonly TodoDbContext dbContext;
        private const string route = "todo5";

        public TodoApiEntityFrameworkCore(TodoDbContext dbContext)
        {
            this.dbContext = dbContext;
        }


        [FunctionName("EfCore_EnsureCreated")]
        public async Task<IActionResult> EnsureCreated([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "createdb")]HttpRequest req,
            TraceWriter log)
        {
            var result = await dbContext.Database.EnsureCreatedAsync();
            log.Info($"Ensure Created {result}");
            return new OkResult();
        }

        [FunctionName("EfCore_CreateTodo")]
        public async Task<IActionResult>CreateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = route)]HttpRequest req,
            TraceWriter log)
        {
            log.Info("Creating a new todo list item");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<TodoCreateModel>(requestBody);

            var todo = new Todo { TaskDescription = input.TaskDescription };
            dbContext.Todo.Add(todo);
            await dbContext.SaveChangesAsync();
            return new OkObjectResult(todo);
        }

        [FunctionName("EfCore_GetTodos")]
        public async Task<IActionResult> GetTodos(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = route)]HttpRequest req,
            TraceWriter log)
        {
            log.Info("Getting todo list items");
            var maxPageSize = 20;
            var todos = await dbContext.Todo.Take(maxPageSize).ToListAsync();
            return new OkObjectResult(todos);
        }

        [FunctionName("EfCore_GetTodoById")]
        public async Task<IActionResult> GetTodoById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = route + "/{id}")]HttpRequest req,
            TraceWriter log, string id)
        {
            log.Info("Getting todo item by id");
            var todo = await dbContext.Todo.FindAsync(id);
            if (todo == null)
            {
                log.Info($"Item {id} not found");
                return new NotFoundResult();
            }
            return new OkObjectResult(todo);
        }

        [FunctionName("EfCore_UpdateTodo")]
        public async Task<IActionResult> UpdateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = route + "/{id}")]HttpRequest req,
            TraceWriter log, string id)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updated = JsonConvert.DeserializeObject<TodoUpdateModel>(requestBody);

            var todo = await dbContext.Todo.FindAsync(id);
            if (todo == null)
            {
                log.Info($"Item {id} not found");
                return new NotFoundResult();
            }

            todo.IsCompleted = updated.IsCompleted;

            if (!string.IsNullOrEmpty(updated.TaskDescription))
            {
                todo.TaskDescription = updated.TaskDescription;
            }

            await dbContext.SaveChangesAsync();

            return new OkObjectResult(todo);
        }

        [FunctionName("EfCore_DeleteTodo")]
        public async Task<IActionResult> DeleteTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = route + "/{id}")]HttpRequest req,
            TraceWriter log, string id)
        {
            var todo = await dbContext.Todo.FindAsync(id);
            if (todo == null)
            {
                log.Info($"Item {id} not found");
                return new NotFoundResult();
            }

            dbContext.Todo.Remove(todo);
            await dbContext.SaveChangesAsync();
            return new OkResult();
        }
    }
}
