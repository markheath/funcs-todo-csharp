using AzureFunctionsTodo.Models;
using Microsoft.EntityFrameworkCore;

namespace AzureFunctionsTodo.EntityFramework
{
    /// <summary>
    /// Context for entity framework
    /// </summary>
    public class TodoContext : DbContext
    {
        public TodoContext(DbContextOptions<TodoContext> options)
            : base(options)
        { }

        public DbSet<Todo> Todos { get; set; }
    }
}