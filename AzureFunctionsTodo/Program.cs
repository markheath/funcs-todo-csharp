using AzureFunctionsTodo.EntityFramework;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString") ?? throw new InvalidOperationException("SqlConnectionString environment variable not set");
        services.AddDbContext<TodoContext>(
            options => SqlServerDbContextOptionsExtensions.UseSqlServer(options, connectionString));
    })
    .Build();

host.Run();