using Azure.Storage.Blobs;
using Microsoft.OpenApi.Models;
using SmartCache.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);


// Orleans builder
builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();

    var useInMemoryStorage = builder.Configuration.GetValue<bool>("UseInMemoryStorage");

    if (useInMemoryStorage)
    {
        // Use in-memory storage for local testing or if you don't want to use Azure Blob Storage
        siloBuilder.AddMemoryGrainStorage("AzureBlobStore");
    }
    else
    {
        // Use Azure Blob Storage for grain persistence
        siloBuilder.AddAzureBlobGrainStorage(
            name: "AzureBlobStore",
            configureOptions: options =>
            {
                options.BlobServiceClient = new BlobServiceClient(
                    builder.Configuration["AzureStorage:ConnectionString"]
                );
            });
    }
});

builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        Name = "X-API-Key",
        In = ParameterLocation.Header,
        Description = "API Key required to access the endpoints"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Bearer token required"
    });

    // Apply security globally
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            new string[] {}
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartCache API v1");
    });
}

app.UseMiddleware<ApiSecurityMiddleware>();

app.MapControllers();
app.Run();
