using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

[assembly:WebJobsStartup(typeof(AzureFunctionsTodo.Startup))]

namespace AzureFunctionsTodo
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            // TODO - find out how we can access IConfiguration to get the connection string
            builder.Services.AddDbContext<TodoDbContext>(options =>
                options.UseSqlServer(Environment.GetEnvironmentVariable("TodoContext")));
            builder.Services.AddSingleton<IMyService, MyService>();
        }
    }



    interface IMyService
    {

    }

    class MyService : IMyService
    {

    }
}
