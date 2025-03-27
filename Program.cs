using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Storage.Blobs;

var builder = FunctionsApplication.CreateBuilder(args);

// Get the storage connection string from the environment variables
string? storageConnectionString = builder.Configuration["AzureWebJobsStorage"];

if (string.IsNullOrEmpty(storageConnectionString))
{
    throw new InvalidOperationException("AzureWebJobsStorage is not set in environment variables.");
}

// Register the BlobServiceClient
builder.Services.AddSingleton(new BlobServiceClient(storageConnectionString));

builder.Services.AddHttpClient();

builder.ConfigureFunctionsWebApplication();

builder.Build().Run();
