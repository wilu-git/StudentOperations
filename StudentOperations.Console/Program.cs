
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StudentOperations.Application;
using StudentOperations.ConsoleApp;
using StudentOperations.Infrastructure.Data;
using StudentOperations.Infrastructure.Services;



//var builder = Host.CreateApplicationBuilder(args);
//Problem Encountered Student Database connection string is missing. This error occurs when the application is unable to find the connection string for the Student Database in the configuration settings. To resolve this issue, ensure that the connection string is properly defined in the appsettings.json file or any other configuration source being used by the application.
//Problem 2: The migration did not create the Hangfire tables in the database. Manually created

//Attempt Fix: 
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});


var studentConnection =
    builder.Configuration.GetConnectionString("StudentDatabase")
    ?? throw new InvalidOperationException(
        "Student Database connection string is missing.");

var hangfireConnection =
    builder.Configuration.GetConnectionString("HangfireDatabase")
    ?? throw new InvalidOperationException(
        "Hangfire Database connection string is missing.");

builder.Services.AddDbContext<StudentOperationsDbContext>(
    options => options.UseSqlServer(studentConnection));

builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

builder.Services.AddTransient<EnrollmentJob>();

builder.Services.AddHangfire(configuration =>
    configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(
            hangfireConnection,
            new SqlServerStorageOptions()));

builder.Services.AddHangfireServer();

//Important: Register ConsoleMenu as Scoped to ensure it can resolve scoped services like IEnrollmentService
builder.Services.AddScoped<ConsoleMenu>();

using var host = builder.Build();

await host.StartAsync();

try
{
    RecurringJob.AddOrUpdate<EnrollmentJob>(
        "process-pending-enrollments",
        job => job.ProcessPendingAsync(),
        Cron.Minutely);

    using var scope = host.Services.CreateScope();

    var menu = scope.ServiceProvider
        .GetRequiredService<ConsoleMenu>();

    await menu.RunAsync();
}
finally
{
    await host.StopAsync();
}
