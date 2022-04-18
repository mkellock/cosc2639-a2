using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddQueryType<api.Query>()
    .SetRequestOptions(_ => new HotChocolate.Execution.Options.RequestExecutorOptions { ExecutionTimeout = TimeSpan.FromMinutes(2) });
//.AddMutationType<api.Mutation>();

var cors = System.Environment.GetEnvironmentVariable("CORS_URLS");
var origins = cors?.Split(',', System.StringSplitOptions.RemoveEmptyEntries);

if (origins == null || origins.Length == 0)
{
    origins = new string[] { "http://localhost:3000", "http://localhost:8080", "http://127.0.0.1:8080", "http://0.0.0.0:8080" };
}

builder.Services
    .AddCors(options =>
    {
        options.AddDefaultPolicy(builder =>
        {
            builder.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

var app = builder.Build();
app.UseFileServer(new FileServerOptions
{
    FileProvider = new PhysicalFileProvider(
                    System.IO.Path.Combine(Directory.GetCurrentDirectory(), "static")),
    EnableDefaultFiles = true
});
app.UseCors();
app.MapGraphQL();
app.Run();
